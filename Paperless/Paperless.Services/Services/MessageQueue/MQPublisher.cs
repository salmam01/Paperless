using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Paperless.Services.Services.MessageQueue
{
    public class MQPublisher
    {
        private readonly ILogger<MQPublisher> _logger;
        private readonly ConnectionFactory _connectionFactory;
        private readonly RabbitMqConfig _config;

        public MQPublisher(
            ILogger<MQPublisher> logger,
            IOptions<RabbitMqConfig> config
        ) {
            _logger = logger;
            _config = config.Value;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.User,
                Password = _config.Password,
            };
        }

        public async Task PublishOcrResult(string documentId, string ocrResult)
        {
            _logger.LogInformation(
                "Publishing OCR result to queue. Document ID: {DocumentId}, Queue: {QueueName}, OCR result length: {OcrLength} characters.",
                documentId,
                _config.QueueName,
                ocrResult?.Length ?? 0
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

                string resultToJson = JsonSerializer.Serialize(
                    new
                    {
                        Id = documentId,
                        OcrResult = ocrResult
                    }
                );
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
                    exchange: "",
                    routingKey: _config.QueueName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document {DocumentId} to GenAI queue {QueueName} for summary generation published.",
                    documentId,
                    _config.QueueName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Upps! Failed to send document {DocumentId} to {queueName}. Error: {ErrorMessage}",
                    documentId,
                    _config.QueueName,
                    ex.Message
                );
            }
        }
    }
}
