using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Messaging.Listeners
{
    public class OCRListener : MQBaseListener
    {
        public OCRListener(
            ILogger<OCRListener> logger,
            IOptions<QueueConfig> config,
            MQConnectionFactory mqConnectionFactory
        ) : base (logger, config, mqConnectionFactory) { }

        protected override async Task DeclareTopologyAsync()
        {
            await _channel.QueueDeclareAsync(
                queue: _config.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.QueueBindAsync(_config.QueueName, _exchangeName, _config.RoutingKeys[0]);

            _logger.LogInformation(
                "Declared topology for queue {QueueName}.",
                _config.QueueName
            );
        }

        public OCRPayload ProcessPayload(BasicDeliverEventArgs ea)
        {
            try
            {
                string body = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogInformation(
                    "Received message from Message Queue {QueueName}.\nMessage:\n{message}",
                    _config.QueueName,
                    body
                );

                OCRPayload? payload = JsonSerializer.Deserialize<OCRPayload>(
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
