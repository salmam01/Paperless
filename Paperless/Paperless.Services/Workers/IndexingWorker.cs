using Paperless.Services.Models.Dtos;
using Paperless.Services.Models.Search;
using Paperless.Services.Services.MessageQueues;
using Paperless.Services.Services.SearchService;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Paperless.Services.Workers
{
    public class IndexingWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly MQListener _mqListener;
        private readonly IElasticService _elasticService;

        public IndexingWorker(
            ILogger<IndexingWorker> logger,
            [FromKeyedServices("IndexingListener")] MQListener mqListener,
            IElasticService searchService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _elasticService = searchService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Indexing Worker is starting...");
            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(string message, BasicDeliverEventArgs ea)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Dictionary<string, string> jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(message)
                ?? new Dictionary<string, string>();

            if (jsonObject == null || !jsonObject.ContainsKey("Id") || !jsonObject.ContainsKey("OcrResult"))
            {
                _logger.LogWarning(
                    "Document has no content. Skipping summary generation."
                );
                return;
            }
            SearchDocument document = ParseMessage(message);

            _logger.LogInformation(
                "Document {DocumentId} content retrieved.",
                document.Id
            );

            await _elasticService.CreateIndexIfNotExistsAsync();
            await _elasticService.AddOrUpdate(document);
        }

        private SearchDocument ParseMessage(string message)
        {
            Dictionary<string, string> jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(message)
                ?? new Dictionary<string, string>();

            SearchDocument document = new();

            if (jsonObject == null || !jsonObject.ContainsKey("Id") || !jsonObject.ContainsKey("Title") || !jsonObject.ContainsKey("OcrResult"))
            {
                _logger.LogWarning(
                    "Document is NULL. Skipping summary generation. Message: {Message}",
                    message
                );
                return document;
            }

            document.Id = jsonObject["Id"];
            document.Title = jsonObject["Title"];
            document.Content = jsonObject["OcrResult"];

            return document;
        }
    }
}
