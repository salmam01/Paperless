using Paperless.API.Configurations;
using Paperless.API.Dtos;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Paperless.API.Messaging
{
    public class DocumentPublisher
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly RabbitMqConfig _config;
        private readonly ILogger<DocumentPublisher> _logger;

        public DocumentPublisher(RabbitMqConfig config, ILogger<DocumentPublisher> logger)
        {
            _config = config;
            _logger = logger;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = config.Host,
                Port = config.Port,
                UserName = config.User,
                Password = config.Password,
            };
        }

        public async Task PublishDocumentAsync(DocumentDto documentDto)
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

                var messageToJson = JsonSerializer.Serialize(documentDto);
                var body = Encoding.UTF8.GetBytes(messageToJson);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _config.QueueName,
                    mandatory: true,
                    basicProperties: new BasicProperties { Persistent = true },
                    body: body
                );

                _logger.LogInformation("Published document to RabbitMQ {DocumentId}", documentDto.Id);

            } catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "publishing to RabbitMQ failing."
                );
                throw;
            }
        }
    }
}
