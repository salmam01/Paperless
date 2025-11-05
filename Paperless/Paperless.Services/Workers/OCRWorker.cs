using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using Paperless.Services.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Services.Workers
{
    //  OCR - Optional Character Recognition
    public class OcrWorker : BackgroundService
    {
        private readonly ILogger<OcrWorker> _logger;
        private readonly RabbitMqConfig _rabbitMqConfig;
        private readonly ConnectionFactory _connectionFactory;
        private readonly StorageService _storageService;
        private readonly OcrService _ocrService;
        private IChannel? _channel;
        private IConnection? _connection;

        public OcrWorker(
            ILogger<OcrWorker> logger, 
            IOptions<RabbitMqConfig> rabbitMqConfig, 
            StorageService storageService,
            OcrService ocrService
        ) {
            _rabbitMqConfig = rabbitMqConfig.Value;
            _storageService = storageService;
            _ocrService = ocrService;
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
                        
                        // Neue Properties mit aktualisiertem Retry-Counter erstellen
                        var newProperties = new BasicProperties
                        {
                            Persistent = true,
                            Headers = new Dictionary<string, object>()
                        };
                        
                        // Bestehende Headers kopieren (falls vorhanden)
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
                        
                        // Retry-Counter setzen
                        newProperties.Headers["x-retry-count"] = retryCount;
                        
                        // Nachricht mit aktualisiertem Counter zurück in Queue schicken
                        await _channel.BasicPublishAsync(
                            exchange: "",
                            routingKey: _rabbitMqConfig.QueueName,
                            mandatory: true,
                            basicProperties: newProperties,
                            body: ea.Body.ToArray()
                        );
                        
                        // Original-Nachricht bestätigen (damit sie nicht mehrfach verarbeitet wird)
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
                        
                        // Maximaler Retry-Count erreicht - Nachricht endgültig ablehnen
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
    }
}
