using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Messaging.Listeners
{
    public enum IndexingEventType
    {
        OcrCompleted,
        DocumentDeleted,
        DocumentsDeleted
    }

    public class IndexingListener : MQBaseListener
    {
        private const string _configName = "IndexingListener";

        public IndexingListener(
            ILogger<IndexingListener> logger,
            IOptionsMonitor<ListenerConfig> config,
            MQConnectionFactory factory
        ) : base(logger, config, factory, _configName) { }

        protected override async Task DeclareTopologyAsync()
        {
            await _channel.QueueDeclareAsync(
                queue: _config.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.QueueBindAsync(_config.QueueName, _exchangeName, _config.RoutingKeys[0]);
            await _channel.QueueBindAsync(_config.QueueName, _exchangeName, _config.RoutingKeys[1]);
            await _channel.QueueBindAsync(_config.QueueName, _exchangeName, _config.RoutingKeys[2]);

            _logger.LogInformation(
                "Declared topology for queue {QueueName}.",
                _config.QueueName
            );
        }

        public IndexingEventType HandleEventType(BasicDeliverEventArgs ea)
        {
            if (ea.RoutingKey == _config.RoutingKeys[0])
                return IndexingEventType.OcrCompleted;
            if (ea.RoutingKey == _config.RoutingKeys[1])
                return IndexingEventType.DocumentDeleted;
            if (ea.RoutingKey == _config.RoutingKeys[2])
                return IndexingEventType.DocumentsDeleted;

            throw new InvalidOperationException($"Unknown routing key: {ea.RoutingKey}");
        }

        public SummaryCompletedPayload ProcessSummaryCompletedPayload(BasicDeliverEventArgs ea)
        {
            try
            {
                string body = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogInformation(
                    "Received message from Message Queue {QueueName}.\nMessage:\n{message}",
                    _config.QueueName,
                    body
                );

                SummaryCompletedPayload? payload = JsonSerializer.Deserialize<SummaryCompletedPayload>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (payload == null)
                    throw new InvalidOperationException("Message could not be deserialized");

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse message from queue {QueueName}", _config.QueueName);
                throw;
            }
        }

        public string ProcessDeleteDocumentPayload(BasicDeliverEventArgs ea)
        {
            try
            {
                string id = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogInformation(
                    "Received message on {QueueName}.\nMessage:\n{message}",
                    _config.QueueName,
                    id
                );

                if (id == null || string.IsNullOrEmpty(id))
                    throw new InvalidOperationException("Message could not be deserialized");

                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse message from queue {QueueName}", _config.QueueName);
                throw;
            }
        }
    }
}
