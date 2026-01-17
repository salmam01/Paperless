using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paperless.BL.Configurations;
using Paperless.BL.Exceptions;
using Paperless.BL.Models.Domain;
using Paperless.BL.Models.Dtos;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Paperless.BL.Services.Messaging
{
    public class DocumentPublisher : IDocumentPublisher
    {
        private readonly ConnectionFactory _factory;
        private readonly MQPublisherConfig _config;
        private readonly ILogger<DocumentPublisher> _logger;
        private readonly string _exchangeName;
        
        public DocumentPublisher(
            MQConnectionFactory factory,
            IOptions<MQPublisherConfig> config, 
            ILogger<DocumentPublisher> logger
        ) {
            _config = config.Value;
            _logger = logger;
            _factory = factory.ConnectionFactory;
            _exchangeName = factory.ExchangeName;
        }

        public async Task PublishDocumentAsync(Guid id, string fileType, List<Category> categories)
        {
            try
            {
                await using IConnection connection = await _factory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                DocumentUploadPayload payload = new DocumentUploadPayload
                {
                    DocumentId = id,
                    FileType = fileType,
                    Categories = categories
                };

                string jsonString = JsonSerializer.Serialize(
                    payload,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null
                    }
                );
                byte[] body = Encoding.UTF8.GetBytes(jsonString);

                BasicProperties properties = new BasicProperties 
                { 
                    Persistent = true,
                    Headers = new Dictionary<string, object?>
                    {
                        { "x-retry-count", 0 }
                    }
                };

                if (payload.FileType != "PDF")
                {
                    await channel.BasicPublishAsync(
                        exchange: _exchangeName,
                        routingKey: _config.RoutingKeys[0],
                        mandatory: true,
                        basicProperties: properties,
                        body: body
                    );

                    _logger.LogInformation(
                        "Document with ID {Id} published to Exchange {ExchangeName} with Routing Key {RoutingKey} for Summary Generation.",
                        id,
                        _exchangeName,
                        _config.RoutingKeys[0]
                    );
                } 
                else
                {
                    await channel.BasicPublishAsync(
                        exchange: _exchangeName,
                        routingKey: _config.RoutingKeys[1],
                        mandatory: true,
                        basicProperties: properties,
                        body: body
                    );

                    _logger.LogInformation(
                        "Document with ID {Id} published to Exchange {ExchangeName} with Routing Key {RoutingKey} for OCR and Summary Generation.",
                        id,
                        _exchangeName,
                        _config.RoutingKeys[1]
                    );
                }

            } catch (Exception ex) {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "publishing to RabbitMQ failing."
                );
                throw new RabbitMQException($"Failed to publish document {id} to Message Queue.", ex);
            }
        }

        public async Task UpdateDocumentCategoryAsync(Guid documentId, string category)
        {
            try
            {
                await using IConnection connection = await _factory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                UpdateDocumentCategoryPayload payload = new UpdateDocumentCategoryPayload
                {
                    DocumentId = documentId,
                    Category = category
                };


                string jsonString = JsonSerializer.Serialize(
                    payload,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null
                    }
                );

                byte[] body = Encoding.UTF8.GetBytes(jsonString);
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
                    routingKey: _config.RoutingKeys[2],
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document with ID {Id} published to Exchange {ExchangeName} with Routing Key {RoutingKey} for OCR.",
                    documentId,
                    _exchangeName,
                    _config.RoutingKeys[2]
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "DELETE", "Business", "publishing to RabbitMQ failing."
                );
                throw new RabbitMQException($"Failed to publish document {documentId} to Message Queue.", ex);
            }
        }

        public async Task DeleteDocumentAsync(Guid id)
        {
            try
            {
                await using IConnection connection = await _factory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

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
                    exchange: _exchangeName,
                    routingKey: _config.RoutingKeys[3],
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document with ID {Id} published to Exchange {ExchangeName} with Routing Key {RoutingKey} for OCR.",
                    id,
                    _exchangeName,
                    _config.RoutingKeys[1]
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "DELETE", "Business", "publishing to RabbitMQ failing."
                );
                throw new RabbitMQException($"Failed to publish document {id} to Message Queue.", ex);
            }
        }

        public async Task DeleteDocumentsAsync()
        {
            try
            {
                await using IConnection connection = await _factory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                byte[] body = Encoding.UTF8.GetBytes("");
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
                    routingKey: _config.RoutingKeys[4],
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document with published to Exchange {ExchangeName} with Routing Key {RoutingKey} for Summary Generation.",
                    _exchangeName,
                    _config.RoutingKeys[2]
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "DELETE ALL", "Business", "publishing to RabbitMQ failing."
                );
                throw new RabbitMQException($"Failed to publish delete documents to Message Queue.", ex);
            }
        }

    }
}
