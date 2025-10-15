using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paperless.BL.Configurations;
using Paperless.BL.Exceptions;
using Paperless.BL.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Paperless.BL.Services
{
    public class DocumentPublisher
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly RabbitMqConfig _config;
        private readonly ILogger<DocumentPublisher> _logger;
        
        public DocumentPublisher(IOptions<RabbitMqConfig> config, ILogger<DocumentPublisher> logger)
        {
            _config = config.Value;
            _logger = logger;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.User,
                Password = _config.Password,
            };
        }

        public async Task PublishDocumentAsync(Document document)
        {
            try
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: _config.QueueName, 
                    durable: true, 
                    exclusive: false, 
                    autoDelete: false
                );

                var messageToJson = JsonSerializer.Serialize(document);
                var body = Encoding.UTF8.GetBytes(messageToJson);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _config.QueueName,
                    mandatory: true,
                    basicProperties: new BasicProperties { Persistent = true },
                    body: body
                );

                _logger.LogInformation("Published document with ID {DocumentId} to Message Queue {QueueName} successfully.", document.Id, _config.QueueName);

            } catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "publishing to RabbitMQ failing."
                );
                throw new RabbitMQException($"Failed to publish document {document.Id} to Message Queue.", ex);
            }
        }
    }
}
