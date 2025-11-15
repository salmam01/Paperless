using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using Paperless.Services.Services;
using Paperless.Services.Services.MessageQueue;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Paperless.Services.Workers
{
    //  OCR - Optional Character Recognition :)
    public class OcrWorker : BackgroundService
    {
        private readonly ILogger<OcrWorker> _logger;
        private readonly MQListener _messageQueueService;
        private readonly StorageService _storageService;
        private readonly OcrService _ocrService;

        public OcrWorker(
            ILogger<OcrWorker> logger, 
            MQListener messageQueueService,
            StorageService storageService,
            OcrService ocrService
        ) {
            _messageQueueService = messageQueueService;
            _storageService = storageService;
            _ocrService = ocrService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //  Stream -> Temp file -> Ghostscript -> Upload -> Delete
            await _messageQueueService.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(string id, BasicDeliverEventArgs ea)
        {
            //  Download file (stream) from minIO
            MemoryStream documentContent = await _storageService.DownloadDocumentFromStorageAsync(id);
            if (documentContent.Length <= 0)
                throw new Exception("Document stream is empty.");

            //  Process file to text
            OcrResult result = _ocrService.ProcessPdf(documentContent);

            _logger.LogInformation(
                "Processed document from Message Queue successfully.\n*** Result ***\n{content}",
                result.PdfContent
            );

            //await _messageQueueService.PublishToResultQueue(result.PdfContent);

        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _messageQueueService.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }

        /*
        private async Task SendToGenAIQueueAsync(Guid documentId)
        {
            try
            {
                await using IConnection connection = await _connectionFactory.CreateConnectionAsync();
                await using IChannel channel = await connection.CreateChannelAsync();

                string genAIQueueName = "document.ocr.completed";
                await channel.QueueDeclareAsync(
                    queue: genAIQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );

                byte[] body = Encoding.UTF8.GetBytes(documentId.ToString());
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
                    routingKey: genAIQueueName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation(
                    "Document {DocumentId} to GenAI queue {QueueName} for summary generation published.",
                    documentId,
                    genAIQueueName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Upps! Failed to send document {DocumentId} to GenAI queue. Error: {ErrorMessage}",
                    documentId,
                    ex.Message
                );
                // Don't throw - OCR was successful, GenAI can be retried later
            }
        }*/
    }
}
