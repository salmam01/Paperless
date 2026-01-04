using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client.Events;
using System.Text;

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
                "Declared topology for queue {QueueName}.",
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
                        "Received empty message from Message Queue {QueueName}.",
                        _config.QueueName
                    );
                    return payload;
                }

                _logger.LogInformation(
                    "Received message from Message Queue {QueueName}.\nMessage:\n{message}",
                    _config.QueueName,
                    body
                );

                JObject jsonObject = JObject.Parse(body);
                
                if (jsonObject.TryGetValue("Id", out JToken? idToken))
                    payload.Id = idToken?.ToString() ?? string.Empty;

                List<string> categories = [];

                if (jsonObject.TryGetValue("Categories", out JToken? categoriesToken) &&
                    categoriesToken is JArray categoriesArray)
                {
                    foreach (var category in categoriesArray)
                    {
                        var name = category["Name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                            categories.Add(name);
                    }
                }

                payload.CategoryList.Categories = categories;

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
