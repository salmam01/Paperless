using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Services.Services
{
    public class MessageQueueService {
        private readonly ILogger<MessageQueueService> _logger;
        private readonly RabbitMqConfig _config;
        private readonly ConnectionFactory _connectionFactory;
        private IChannel? _channel;
        private IConnection? _connection;

        public MessageQueueService(
            ILogger<MessageQueueService> logger,
            IOptions<RabbitMqConfig> config)
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

        public async Task StartListeningAsync(
            Func<string, BasicDeliverEventArgs, Task> onMessageReceived,
            CancellationToken stoppingToken
        ) {
            _connection = await _connectionFactory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _config.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.BasicQosAsync(0, 1, false);
            _logger.LogInformation("OCR Worker is running and listening for messages in {QueueName}.", _config.QueueName);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag, 
                        multiple: false, 
                        requeue: true
                    );
                    await StopListeningAsync();
                }

                string? id = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation(
                    "Received document ID {message} from Message Queue {QueueName}.",
                    id,
                    _config.QueueName
                );

                int retryCount = 0;
                if (ea.BasicProperties.Headers != null &&
                    ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryCountObj)
                ) {
                    try
                    {
                        retryCount = Convert.ToInt32(retryCountObj);
                    }
                    catch
                    {
                        retryCount = 0;
                    }
                }

                try
                {
                    await onMessageReceived(id, ea);
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount <= _config.MaxRetries)
                    {
                        _logger.LogWarning(
                            ex,
                            "Retry attempt {RetryCount}/{MaxRetries} for document ID {DocumentId} from Message Queue {QueueName}. Error: {ErrorMessage}",
                            retryCount,
                            _config.MaxRetries,
                            id,
                            _config.QueueName,
                            ex.Message
                        );

                        await Retry(ea, retryCount);
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Max retries ({MaxRetries}) exceeded for document ID {DocumentId} from Message Queue {QueueName}. Message will be permanently rejected. Error: {ErrorMessage}",
                            _config.MaxRetries,
                            id,
                            _config.QueueName,
                            ex.Message
                        );

                        // Maximaler Retry-Count erreicht --> Nachricht endgültig ablehnen
                        await _channel.BasicNackAsync(
                            deliveryTag: ea.DeliveryTag, 
                            multiple: false, 
                            requeue: false
                        );
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _config.QueueName, 
                autoAck: false, 
                consumer: consumer
            );
        }

        public async Task Retry(BasicDeliverEventArgs ea, int retryCount)
        {
            BasicProperties newProperties = new BasicProperties
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
                routingKey: _config.QueueName,
                mandatory: true,
                basicProperties: newProperties,
                body: ea.Body.ToArray()
            );

            // Original-Nachricht bestätigen (-->damit sie nicht mehrfach verarbeitet wird)
            await _channel.BasicAckAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false
            );
        }

        public async Task StopListeningAsync()
        {
            if (_channel != null)
                await _channel.CloseAsync();
            if (_connection != null)
                await _connection.CloseAsync();
        }
    }
}
