using Microsoft.Extensions.Options;
using Minio;
using Paperless.Services.Configurations;
using Paperless.Services.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Tesseract;

namespace Paperless.Services.Workers
{
    //  OCR - Optional Character Recognition
    public class OCRWorker : BackgroundService
    {
        private readonly ILogger<OCRWorker> _logger;
        private readonly RabbitMqConfig _rabbitMqConfig;
        private readonly ConnectionFactory _connectionFactory;
        private IChannel? _channel;
        private IConnection? _connection;
        private readonly IMinioClient _minIO;

        public OCRWorker(ILogger<OCRWorker> logger, IOptions<RabbitMqConfig> rabbitMqConfig, MinIOService minIOService)
        {
            _rabbitMqConfig = rabbitMqConfig.Value;
            _minIO = minIOService.Client;
            _logger = logger;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _rabbitMqConfig.Host,
                Port = _rabbitMqConfig.Port,
                UserName = _rabbitMqConfig.User,
                Password = _rabbitMqConfig.Password,
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            /*
            Tesseract can only convert text from images (turns them into bitmaps) --> ghostscript converts pdfs to images
                1) worker receives pdf 
                2) checks file format
                3) if it's a pdf, call ghostscript
                4) ghostscript converts pdf to image
                5) tesseract converts image to text
            */
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
                    string? message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("Received document ID {message} from Message Queue {QueueName}.", message, _rabbitMqConfig.QueueName);

                    // Simulate processing
                    await Task.Delay(500, stoppingToken);

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Processed document from Message Queue {QueueName} successfully.", _rabbitMqConfig.QueueName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "{method} /document failed in {layer} Layer due to {reason}.",
                        "POST", "Services", "an error processing the message"
                    );
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(queue: _rabbitMqConfig.QueueName, autoAck: false, consumer: consumer);
        }

        public string processImage(string body)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile("add image"); // TODO: fix
            using var page = engine.Process(img);

            return page.GetText();
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
