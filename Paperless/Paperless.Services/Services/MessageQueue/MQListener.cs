using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Data.Common;
using System.Text;

namespace Paperless.Services.Services.MessageQueue
{
    public class MQListener {
        private readonly ILogger<MQListener> _logger;
        private readonly RabbitMqConfig _config;
        private readonly ConnectionFactory _connectionFactory;
        private IChannel? _channel;
        private IConnection? _connection;

        public MQListener(
            ILogger<MQListener> logger,
            IOptions<RabbitMqConfig> config
        ) {
            _logger = logger;
            _config = config.Value;

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
            _logger.LogInformation(
                "Starting Message Queue listener. Queue: {QueueName}, Host: {Host}, Port: {Port}",
                _config.QueueName,
                _config.Host,
                _config.Port
            );

            // Retry logic for initial connection with exponential backoff
            int maxConnectionRetries = 10;
            int retryDelaySeconds = 5;
            
            for (int attempt = 1; attempt <= maxConnectionRetries; attempt++)
            {
                try
                {
                    if (_connection == null || !_connection.IsOpen)
                        _connection = await _connectionFactory.CreateConnectionAsync();
                    if (_channel == null || !_channel.IsOpen)
                        _channel = await _connection.CreateChannelAsync();
                    
                    // Connection successful, break out of retry loop
                    break;
                }
                catch (Exception ex) when (attempt < maxConnectionRetries)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries}). Retrying in {DelaySeconds} seconds...",
                        attempt,
                        maxConnectionRetries,
                        retryDelaySeconds
                    );
                    
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);
                    retryDelaySeconds = Math.Min(retryDelaySeconds * 2, 30); // Exponential backoff, max 30 seconds
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to connect to RabbitMQ after {MaxRetries} attempts. Giving up.",
                        maxConnectionRetries
                    );
                    throw;
                }
            }

            if (_channel == null || !_channel.IsOpen)
            {
                throw new InvalidOperationException("Failed to establish RabbitMQ channel connection.");
            }

            await _channel.QueueDeclareAsync(
                queue: _config.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.BasicQosAsync(0, 1, false);
            _logger.LogInformation(
                "RabbitMQ Queue {QueueName} is running and listening for messages.",
                _config.QueueName
            );

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

                int retryCount = GetRetryCount(ea);
                string? body;
                
                try
                {

                    body = Encoding.UTF8.GetString(ea.Body.ToArray());

                    _logger.LogInformation(
                        "Received message from Message Queue {QueueName}. Message length: {MessageLength} characters, Delivery tag: {DeliveryTag}",
                        _config.QueueName,
                        body?.Length ?? 0,
                        ea.DeliveryTag
                    );

                    await onMessageReceived(body, ea);

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount <= _config.MaxRetries)
                    {
                        _logger.LogWarning(
                            ex,
                            "Retry attempt {RetryCount}/{MaxRetries} from Message Queue {QueueName}. Error: {ErrorMessage}",
                            retryCount,
                            _config.MaxRetries,
                            _config.QueueName,
                            ex.Message
                        );

                        await RetryTask(_channel, ea, retryCount);
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Max retries ({MaxRetries}) exceeded from Message Queue {QueueName}. Message will be permanently rejected. Error: {ErrorMessage}",
                            _config.MaxRetries,
                            _config.QueueName,
                            ex.Message
                        );

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

        private int GetRetryCount(BasicDeliverEventArgs ea)
        {
            if (ea.BasicProperties.Headers != null &&
                ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryCountObj)
            ) {
                try
                {
                    return Convert.ToInt32(retryCountObj);
                }
                catch
                {
                    return 0;
                }
            }

            return 0;
        }

        private async Task RetryTask(IChannel channel, BasicDeliverEventArgs ea, int retryCount)
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
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: _config.QueueName,
                mandatory: true,
                basicProperties: newProperties,
                body: ea.Body.ToArray()
            );

            await channel.BasicAckAsync(
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
