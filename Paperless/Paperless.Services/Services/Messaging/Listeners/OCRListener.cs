using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Messaging.Listeners
{
    public class OCRListener : MQBaseListener
    {
        private const string _configName = "OCRListener";

        public OCRListener(
            ILogger<OCRListener> logger,
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

            _logger.LogInformation(
                "Declared topology for {QueueName}.",
                _config.QueueName
            );
        }

        //  TODO: add a Parser helper class!
        public OCRPayload ProcessPayload(BasicDeliverEventArgs ea)
        {
            try
            {
                string body = Encoding.UTF8.GetString(ea.Body.ToArray());
                OCRPayload? payload = new();

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

                payload = JsonSerializer.Deserialize<OCRPayload>(body);

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse message on {QueueName}", _config.QueueName);
                throw;
            }
        }
    }
}
