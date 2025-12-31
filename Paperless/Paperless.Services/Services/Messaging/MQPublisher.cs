using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs;
using Paperless.Services.Services.Messaging.Base;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Messaging
{
    public class MQPublisher
    {
        private readonly ILogger<MQPublisher> _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly QueueConfig _config;

        public MQPublisher(
            ILogger<MQPublisher> logger,
            IOptions<QueueConfig> config,
            MQConnectionFactory mqConnectionFactory
        )
        {
            _logger = logger;
            _config = config.Value;
            _connectionFactory = mqConnectionFactory.ConnectionFactory;
        }

        public async Task PublishOcrResult(DocumentDTO document)
        {
            try
            {
                await using IConnection connection = await _connectionFactory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: _config.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                string resultToJson = JsonSerializer.Serialize(document);
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
                    exchange: _config.ExchangeName,
                    routingKey: "",
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document {DocumentId} to GenAI queue {QueueName} for summary generation published.",
                    document.Id,
                    _config.QueueName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send document {DocumentId} to {queueName}. Error: {ErrorMessage}",
                    document.Id,
                    _config.QueueName,
                    ex.Message
                );
            }
        }
    }
}