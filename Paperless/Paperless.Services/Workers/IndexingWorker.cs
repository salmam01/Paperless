using Paperless.Services.Models.Search;
using Paperless.Services.Services.MessageQueues;
using Paperless.Services.Services.Search;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Paperless.Services.Workers
{
    public class IndexingWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly MQListener _mqListener;
        private readonly IElasticRepository _elasticService;

        public IndexingWorker(
            ILogger<IndexingWorker> logger,
            [FromKeyedServices("IndexingListener")] MQListener mqListener,
            IElasticRepository searchService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _elasticService = searchService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Indexing Worker is starting...");
            await _elasticService.CreateIndexIfNotExistsAsync();
            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(string message, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation(
                "Processing message from queue inside Indexing Worker." +
                "\nMessage length: {MessageLength} characters.",
                message?.Length ?? 0
            );

            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Received empty message from queue inside Indexing Worker. Skipping processing.");
                    return;
                }

                SearchDocument document = ParseMessage(message);
                if (document == null || string.IsNullOrEmpty(document.Id))
                {
                    _logger.LogWarning("Received invalid message from queue inside Indexing Worker. Skipping processing.");
                    return;
                }

                _logger.LogInformation(
                    "Document {DocumentId} content retrieved inside Indexing Worker.",
                    document.Id
                );

                await _elasticService.IndexAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process document inside Indexing Worker." +
                    "\nError: {ErrorMessage}",
                    ex.Message
                );
                throw;
            }
        }

        private SearchDocument ParseMessage(string message)
        {
            Dictionary<string, string> jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(message)
                ?? new Dictionary<string, string>();

            SearchDocument document = new();

            if (jsonObject == null || !jsonObject.ContainsKey("Id") || !jsonObject.ContainsKey("Title") || !jsonObject.ContainsKey("OcrResult"))
            {
                _logger.LogWarning(
                    "Document is NULL. Skipping indexing for ElasticSearch." +
                    "\nMessage Received: {Message}",
                    message
                );
                return document;
            }

            document.Id = jsonObject["Id"];
            document.Title = jsonObject["Title"];
            document.Content = jsonObject["OcrResult"];

            return document;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Indexing Worker is stopping...");
            await _mqListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
