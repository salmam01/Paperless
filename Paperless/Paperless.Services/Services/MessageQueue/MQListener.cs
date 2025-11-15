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
        private readonly string _queueName;

        public MQListener(
            ILogger<MQListener> logger,
            IOptions<RabbitMqConfig> config,
            string queueName
        ) {
            _logger = logger;
            _config = config.Value;
            _queueName = GetQueueName();

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
            _connection = await GetConnectionAsync();
            _channel = await GetChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _config.OcrQueue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.BasicQosAsync(0, 1, false);
            _logger.LogInformation("OCR Worker is running and listening for messages.");

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

                //  Get Retry-Counter
                int retryCount = GetRetryCount(ea);

                try
                {
                    if (_queueName == "ocr.worker")
                    {
                        string? id = Encoding.UTF8.GetString(ea.Body.ToArray());

                        _logger.LogInformation(
                            "Received document ID {message} from Message Queue {QueueName}.",
                            id,
                            _config.OcrQueue
                        );

                        await onMessageReceived(id, ea);
                    }
                    else
                    {
                        _logger.LogInformation(
                            //  TODO
                            "Received  from Message Queue {QueueName}.",
                            _config.SummaryQueue
                        );
                    }

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
                            queueName,
                            ex.Message
                        );

                        await Retry(channel, ea, retryCount);
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Max retries ({MaxRetries}) exceeded for document ID {DocumentId} from Message Queue {QueueName}. Message will be permanently rejected. Error: {ErrorMessage}",
                            _config.MaxRetries,
                            id,
                            _config.,
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
                queue: queueName, 
                autoAck: false, 
                consumer: consumer
            );
        }

        private async Task<IConnection> GetConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
                _connection = await _connectionFactory.CreateConnectionAsync();
            return _connection;
        }

        private async Task<IChannel> GetChannelAsync()
        {
            if (_channel == null || !_channel.IsOpen)
                _channel = await _connection.CreateChannelAsync();
            return _channel;
        }

        private string GetQueueName()
        {
            if (_queueType == QueueType.Ocr) return _config.OcrQueue;
            return _config.SummaryQueue;
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

        public async Task Retry(IChannel channel, BasicDeliverEventArgs ea, int retryCount)
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
                routingKey: queueName,
                mandatory: true,
                basicProperties: newProperties,
                body: ea.Body.ToArray()
            );

            // Original-Nachricht bestätigen (-->damit sie nicht mehrfach verarbeitet wird)
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
