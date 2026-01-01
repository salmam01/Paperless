using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Messaging.Publishers
{
    public class MQPublisher
    {
        private readonly ILogger<MQPublisher> _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly QueueConfig _config;
        private readonly string _exchangeName;

        public MQPublisher(
            ILogger<MQPublisher> logger,
            IOptions<QueueConfig> config,
            MQConnectionFactory mqConnectionFactory
        )
        {
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
                    "Document {DocumentId} to Summary queue {QueueName} for summary generation published.",
                    payload.Id,
                    _config.QueueName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send document {DocumentId} to {QueueName}. Error: {ErrorMessage}",
                    payload.Id,
                    _config.QueueName,
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
                    "Document {DocumentId} to Indexing queue {QueueName} for indexing published.",
                    payload.Id,
                    _config.QueueName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send document {DocumentId} to {queueName}. Error: {ErrorMessage}",
                    payload.Id,
                    _config.QueueName,
                    ex.Message
                );
            }
        }
    }
}