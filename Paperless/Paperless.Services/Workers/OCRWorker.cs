using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using Paperless.Services.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Services.Workers
{
    //  OCR - Optional Character Recognition :)
    public class OcrWorker : BackgroundService
    {
        private readonly ILogger<OcrWorker> _logger;
        private readonly RabbitMqConfig _rabbitMqConfig;
        private readonly ConnectionFactory _connectionFactory;
        private readonly StorageService _storageService;
        private readonly OcrService _ocrService;
        private readonly IServiceProvider _serviceProvider;
        private IChannel? _channel;
        private IConnection? _connection;

        public OcrWorker(
            ILogger<OcrWorker> logger, 
            IOptions<RabbitMqConfig> rabbitMqConfig, 
            StorageService storageService,
            OcrService ocrService,
            IServiceProvider serviceProvider
        ) {
            _rabbitMqConfig = rabbitMqConfig.Value;
            _storageService = storageService;
            _ocrService = ocrService;
            _serviceProvider = serviceProvider;
            _logger = logger;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _rabbitMqConfig.Host,
                Port = _rabbitMqConfig.Port,
                UserName = _rabbitMqConfig.User,
                Password = _rabbitMqConfig.Password,
            };
        }

        //  TODO: Refactor method, split it into multiple parts (MessageQueue, Storage: done, OCR: done)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _rabbitMqConfig.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.BasicQosAsync(0, 1, false);
            _logger.LogInformation("OCR Worker is running and listening for messages in {QueueName}.", _rabbitMqConfig.QueueName);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    //  Stream -> Temp file -> Ghostscript -> Upload -> Delete

                    string? id = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation(
                        "Received document ID {message} from Message Queue {QueueName}.", 
                        id, 
                        _rabbitMqConfig.QueueName
                    );

                    //  Download file (stream) from minIO
                    MemoryStream documentContent = await _storageService.DownloadDocumentFromStorageAsync(id);
                    if (documentContent.Length <= 0)
                        throw new Exception("Document stream is empty.");
                    
                    OcrResult result = _ocrService.ProcessPdf(documentContent);

                    // Document in database with OCR content updtae
                    if (Guid.TryParse(id, out Guid documentId)) {
                        using IServiceScope scope = _serviceProvider.CreateScope();
                        try
                        {
                            DocumentUpdateService documentUpdateService = scope.ServiceProvider.GetRequiredService<DocumentUpdateService>();
                            await documentUpdateService.UpdateDocumentContentAsync(documentId, result.PdfContent);

                            // Send message to GenAI queue for summary generation
                            await SendToGenAIQueueAsync(documentId);
                        }
                        catch (KeyNotFoundException)
                        {
                            _logger.LogWarning("Document {DocumentId} not found in database.", documentId);
                            // Continue anyway -> OCR was successful
                        }
                        catch (Exception dbEx)
                        {
                            _logger.LogError(
                                dbEx,
                                "Failed to update document {DocumentId} in database with OCR content.",
                                documentId
                            );
                            // Continue anyway
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid document ID format: {DocumentId}", id);
                    }

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation(
                        "Processed document from Message Queue {QueueName} successfully.\n *** Result ***\n{content}", 
                        _rabbitMqConfig.QueueName,
                        result.PdfContent
                    );
                }
                catch (Exception ex)
                {
                    string? id = Encoding.UTF8.GetString(ea.Body.ToArray());
                    
                    // Retry-Logik: Retry-Counter aus Headers lesen
                    int retryCount = 0;
                    if (ea.BasicProperties.Headers != null && 
                        ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryCountObj))
                    {
                        try
                        {
                            retryCount = Convert.ToInt32(retryCountObj);
                        }
                        catch
                        {
                            retryCount = 0;
                        }
                    }

                    retryCount++;
                    
                    if (retryCount <= _rabbitMqConfig.MaxRetries)
                    {
                        _logger.LogWarning(
                            ex,
                            "Retry attempt {RetryCount}/{MaxRetries} for document ID {DocumentId} from Message Queue {QueueName}. Error: {ErrorMessage}",
                            retryCount,
                            _rabbitMqConfig.MaxRetries,
                            id,
                            _rabbitMqConfig.QueueName,
                            ex.Message
                        );
                        
                        var newProperties = new BasicProperties
                        {
                            Persistent = true,
                            Headers = new Dictionary<string, object?>()
                        };
                        
                        if (ea.BasicProperties.Headers != null)
                        {
                            foreach (var header in ea.BasicProperties.Headers)
                            {
                                if (header.Key != "x-retry-count")
                                {
                                    newProperties.Headers[header.Key] = header.Value;
                                }
                            }
                        }
                        
                        newProperties.Headers["x-retry-count"] = retryCount;
                        await _channel.BasicPublishAsync(
                            exchange: "",
                            routingKey: _rabbitMqConfig.QueueName,
                            mandatory: true,
                            basicProperties: newProperties,
                            body: ea.Body.ToArray()
                        );
                        
                        // Original-Nachricht bestätigen (-->damit sie nicht mehrfach verarbeitet wird)
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Max retries ({MaxRetries}) exceeded for document ID {DocumentId} from Message Queue {QueueName}. Message will be permanently rejected. Error: {ErrorMessage}",
                            _rabbitMqConfig.MaxRetries,
                            id,
                            _rabbitMqConfig.QueueName,
                            ex.Message
                        );
                        
                        // Maximaler Retry-Count erreicht --> Nachricht endgültig ablehnen
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
            };

            await _channel.BasicConsumeAsync(queue: _rabbitMqConfig.QueueName, autoAck: false, consumer: consumer);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null)
                await _channel.CloseAsync();
            if (_connection != null) 
                _connection?.CloseAsync();

            await base.StopAsync(cancellationToken);
        }

        private async Task SendToGenAIQueueAsync(Guid documentId)
        {
            try
            {
                await using IConnection connection = await _connectionFactory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                string genAIQueueName = "document.ocr.completed";
                await channel.QueueDeclareAsync(
                    queue: genAIQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                byte[] body = Encoding.UTF8.GetBytes(documentId.ToString());
                BasicProperties properties = new BasicProperties
                {
                    Persistent = true,
                    Headers = new Dictionary<string, object?>
                    {
                        { "x-retry-count", 0 }
                    }
                };

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: genAIQueueName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document {DocumentId} to GenAI queue {QueueName} for summary generation published.",
                    documentId,
                    genAIQueueName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Upps! Failed to send document {DocumentId} to GenAI queue. Error: {ErrorMessage}",
                    documentId,
                    ex.Message
                );
                // Don't throw - OCR was successful, GenAI can be retried later
            }
        }
    }
}
