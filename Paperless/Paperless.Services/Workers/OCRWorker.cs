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
                    _logger.LogError(
                        ex,
                        "{method} /document failed in {layer} Layer due to {reason}.",
                        "POST", "Services", "an error processing the message"
                    );
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false); //  TODO: add requeue logic !!
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
