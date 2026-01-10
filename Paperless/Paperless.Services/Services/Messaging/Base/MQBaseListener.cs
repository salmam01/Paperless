using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Paperless.Services.Services.Messaging.Base
{
    public abstract class MQBaseListener
    {
        protected readonly ILogger _logger;
        protected readonly ListenerConfig _config;
        protected readonly ConnectionFactory _factory;
        protected IChannel? _channel;
        protected IConnection? _connection;
        protected readonly string _exchangeName;

        protected MQBaseListener(
            ILogger logger,
            IOptionsMonitor<ListenerConfig> config,
            MQConnectionFactory factory,
            string configName
        ) {
            _logger = logger;
            _config = config.Get(configName);
            _factory = factory.ConnectionFactory;
            _exchangeName = factory.ExchangeName;
        }

        public async Task StartListeningAsync(
            Func<BasicDeliverEventArgs, Task> onMessageReceived,
            CancellationToken cancellationToken
        ) {
            const int maxRetries = 10;
            const int delayMs = 3000; // 3 seconds

            int attempt = 0;
            while (_connection == null && attempt < maxRetries)
            {
                try
                {
                    _connection ??= await _factory.CreateConnectionAsync();
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
                {
                    attempt++;
                    _logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} - RabbitMQ not ready, retrying in {Delay}ms", attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            if (_connection == null)
            {
                _logger.LogCritical("Failed to connect to RabbitMQ after {MaxRetries} attempts", maxRetries);
                throw new InvalidOperationException("Could not connect to RabbitMQ");
            }

            _channel ??= await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: true);
            await DeclareTopologyAsync();

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await _channel.BasicNackAsync(
                        ea.DeliveryTag,
                        multiple: false,
                        requeue: true
                    );
                    //  TODO: Worker needs to stop the Listener
                    return;
                }

                try
                {
                    _logger.LogInformation(
                        "Received message on {QueueName}",
                        _config.QueueName
                    );

                    await onMessageReceived(ea);
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                } 
                catch (Exception ex) 
                {
                    int retryCount = GetRetryCount(ea);
                    await IncreaseRetryCount(retryCount, ea, ex);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _config.QueueName,
                autoAck: false,
                consumer: consumer
            );
        }

        protected abstract Task DeclareTopologyAsync();

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


        private async Task<int> IncreaseRetryCount(int retryCount, BasicDeliverEventArgs ea, Exception ex)
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

            return retryCount;
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
