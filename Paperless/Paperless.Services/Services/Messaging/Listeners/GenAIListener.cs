using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Messaging.Listeners
{
    public enum GenAIEventType
    {
        DocumentUploaded,
        DocumentOCRCompleted
    }
    public class GenAIListener : MQBaseListener
    {
        private const string _configName = "GenAIListener";

        public GenAIListener(
            ILogger<GenAIListener> logger,
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

            _logger.LogInformation(
                "Declared topology for {QueueName}.",
                _config.QueueName
            );
        }
        public GenAIEventType HandleEventType(BasicDeliverEventArgs ea)
        {
            if (ea.RoutingKey == _config.RoutingKeys[0])
                return GenAIEventType.DocumentUploaded;
            if (ea.RoutingKey == _config.RoutingKeys[1])
                return GenAIEventType.DocumentOCRCompleted;

            throw new InvalidOperationException($"Unknown Routing Key: {ea.RoutingKey}");
        }

        public DocumentUploadedPayload ProcessDocumentUploadedPayload(BasicDeliverEventArgs ea)
        {
            try
            {
                string body = Encoding.UTF8.GetString(ea.Body.ToArray());
                DocumentUploadedPayload? payload = new();

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning(
                        "Received empty message on {QueueName}.",
                        _config.QueueName
                    );
                    return payload;
                }

                _logger.LogInformation(
                    "Deserializing message on {QueueName}. Message Length: {Length}",
                    _config.QueueName,
                    body.Length
                );

                payload = JsonSerializer.Deserialize<DocumentUploadedPayload>(body);

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse message on {QueueName}", _config.QueueName);
                throw;
            }
        }

        public OCRCompletedPayload ProcessOCRCompletedPayload(BasicDeliverEventArgs ea)
        {
            try
            {
                string body = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogInformation(
                    "Deserializing message on {QueueName}. Message Length: {Length}",
                    _config.QueueName,
                    body.Length
                );

                OCRCompletedPayload? payload = JsonSerializer.Deserialize<OCRCompletedPayload>(
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
    }
}
