using System.Text;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Paperless.Services.Workers
{
    public class OCRWorker : BackgroundService
    {
        private readonly ILogger<OCRWorker> _logger;
        private readonly RabbitMqConfig _config;
        private IConnection? _connection;
        private IModel? _channel;

        public OCRWorker(ILogger<OCRWorker> logger, IOptions<RabbitMqConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_connection == null || _channel == null)
            {
                ConnectionFactory factory = new ConnectionFactory
                {
                    HostName = _config.Host,
                    Port = _config.Port,
                    UserName = _config.User,
                    Password = _config.Password
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: _config.QueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.BasicQos(0, 1, false);
            }

            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (_, ea) =>
            {
                try
                {
                    string? message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("Received document {message} from Message Queue {QueueName}.", message, _config.QueueName);

                    // Simulate processing
                    Task.Delay(500, stoppingToken).Wait(stoppingToken);

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Processed document from Message Queue {QueueName} successfully.", _config.QueueName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "{method} /document failed in {layer} Layer due to {reason}.",
                        "POST", "Services", "an error processing the message"
                    );
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };
            _channel.BasicConsume(queue: _config.QueueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.Close();
            _connection?.Close();
            return base.StopAsync(cancellationToken);
        }
    }
}
