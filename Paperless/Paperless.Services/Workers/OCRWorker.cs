using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;
using Tesseract;

namespace Paperless.Services.Workers
{
    //  OCR - Optional Character Recognition
    public class OCRWorker : BackgroundService
    {
        private readonly ILogger<OCRWorker> _logger;
        private readonly RabbitMqConfig _config;
        private readonly ConnectionFactory _connectionFactory;
        private IChannel _channel;
        private IConnection _connection;

        public OCRWorker(ILogger<OCRWorker> logger, IOptions<RabbitMqConfig> config)
        {
            _config = config.Value;
            _logger = logger;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.User,
                Password = _config.Password,
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
                queue: _config.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.BasicQosAsync(0, 1, false);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    string? message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("Received document {message} from Message Queue {QueueName}.", message, _config.QueueName);

                    // Simulate processing
                    await Task.Delay(500, stoppingToken);

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Processed document from Message Queue {QueueName} successfully.", _config.QueueName);
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

            await _channel.BasicConsumeAsync(queue: _config.QueueName, autoAck: false, consumer: consumer);
        }

        public string processImage(string body)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile("add image"); // TODO: fix
            using var page = engine.Process(img);

            return page.GetText();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.CloseAsync();
            _connection.CloseAsync();
            return base.StopAsync(cancellationToken);
        }
    }
}
