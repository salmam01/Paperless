using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Messaging.Publishers
{
    //  TODO: can be cleaner
    public class MQPublisher
    {
        private readonly ILogger<MQPublisher> _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly MQPublisherConfig _config;
        private readonly string _exchangeName;

        public MQPublisher(
            ILogger<MQPublisher> logger,
            IOptions<MQPublisherConfig> config,
            MQConnectionFactory mqConnectionFactory
        ) {
            _logger = logger;
            _config = config.Value;
            _connectionFactory = mqConnectionFactory.ConnectionFactory;
            _exchangeName = mqConnectionFactory.ExchangeName;
        }

        public async Task PublishOcrResult(OCRCompletedPayload payload)
        {
            try
            {
                await using IConnection connection = await _connectionFactory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                string resultToJson = JsonSerializer.Serialize(payload);
                byte[] body = Encoding.UTF8.GetBytes(resultToJson);

                BasicProperties properties = new BasicProperties
                {
                    Persistent = true,
                    Headers = new Dictionary<string, object?>
                    {
                        { "x-retry-count", 0 }
                    }
                };

                await channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: _config.RoutingKeys[0],
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document {Id} published to Exchange {ExchangeName} with Routing Key {RoutingKey} for {Reason}.",
                    payload.DocumentId,
                    _exchangeName,
                    _config.RoutingKeys[0],
                    "Summary Generation"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish Document {Id} to Exchange {ExchangeName} with Routing Key {RoutingKey}. " +
                    "Error:\n{ErrorMessage}",
                    payload.DocumentId,
                    _exchangeName,
                    _config.RoutingKeys[0],
                    ex.Message
                );
            }
        }

        public async Task PublishSummaryResult(SummaryCompletedPayload payload)
        {
            try
            {
                await using IConnection connection = await _connectionFactory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                string resultToJson = JsonSerializer.Serialize(payload);
                byte[] body = Encoding.UTF8.GetBytes(resultToJson);

                BasicProperties properties = new BasicProperties
                {
                    Persistent = true,
                    Headers = new Dictionary<string, object?>
                    {
                        { "x-retry-count", 0 }
                    }
                };

                await channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: _config.RoutingKeys[1],
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document {Id} published to Exchange {ExchangeName} with Routing Key {RoutingKey} for {Reason}.",
                    payload.DocumentId,
                    _exchangeName,
                    _config.RoutingKeys[1],
                    "Document Indexing"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish Document {Id} to Exchange {ExchangeName} with Routing Key {RoutingKey}. " +
                    "Error:\n{ErrorMessage}",
                    payload.DocumentId,
                    _exchangeName,
                    _config.RoutingKeys[1],
                    ex.Message
                );
            }
        }
    }
}