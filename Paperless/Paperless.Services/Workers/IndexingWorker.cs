using Paperless.Services.Services;
using Paperless.Services.Services.MessageQueues;
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
        private readonly SearchService _searchService;

        public IndexingWorker(
            ILogger<IndexingWorker> logger,
            [FromKeyedServices("IndexingListener")] MQListener mqListener,
            SearchService searchService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _searchService = searchService;
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
            string id = jsonObject["Id"];
            string ocrResult = jsonObject["OcrResult"];

            _logger.LogInformation(
                "Document {DocumentId} content retrieved.",
                id
            );

            //_searchService.Store(id, ocrResult);
        }

        /*
         * What to store in ElasticSearch (NoSQL DB)
         * Document ID, OCR Result, Search Related stuff?
        */
    }
}
