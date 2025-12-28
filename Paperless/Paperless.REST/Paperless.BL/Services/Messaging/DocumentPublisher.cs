using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paperless.BL.Configurations;
using Paperless.BL.Exceptions;
using RabbitMQ.Client;
using System.Text;

namespace Paperless.BL.Services.Messaging
{
    public class DocumentPublisher : IDocumentPublisher
    {
        private readonly ConnectionFactory _connectionFactory;
        private readonly RabbitMQConfig _config;
        private readonly ILogger<DocumentPublisher> _logger;
        
        public DocumentPublisher(IOptions<RabbitMQConfig> config, ILogger<DocumentPublisher> logger)
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

        public async Task PublishDocumentAsync(Guid id)
        {
            _logger.LogInformation(
                "Publishing document to Message Queue. Document ID: {DocumentId}, Queue: {QueueName}.",
                id,
                _config.QueueName
            );

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
                
                byte[] body = Encoding.UTF8.GetBytes(id.ToString());
                BasicProperties properties = new BasicProperties 
                { 
                    Persistent = true,
                    Headers = new Dictionary<string, object?>
                    {
                        { "x-retry-count", 0 }
                    }
                };
                
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _config.QueueName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Published document with ID {DocumentId} to Message Queue {QueueName} successfully.", 
                    id, 
                    _config.QueueName
                );

            } catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "publishing to RabbitMQ failing."
                );
                throw new RabbitMQException($"Failed to publish document {id} to Message Queue.", ex);
            }
        }
    }
}
