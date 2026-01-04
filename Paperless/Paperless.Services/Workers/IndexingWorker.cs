using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Models.Search;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Services.Search;
using RabbitMQ.Client.Events;

namespace Paperless.Services.Workers
{
    public class IndexingWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IndexingListener _mqListener;
        private readonly IElasticRepository _elasticService;

        public IndexingWorker(
            ILogger<IndexingWorker> logger,
            IndexingListener mqListener,
            IElasticRepository searchService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _elasticService = searchService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "{WorkerType} Worker is starting ...",
                "Indexing"
            );

            await _elasticService.CreateIndexIfNotExistsAsync();
            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            try
            {
                IndexingEventType eventType = _mqListener.HandleEventType(ea);
                switch(eventType)
                {
                    case IndexingEventType.OcrCompleted:
                        SummaryCompletedPayload payload = _mqListener.ProcessSummaryCompletedPayload(ea);
                        if (payload == null || string.IsNullOrEmpty(payload.Id))
                        {
                            _logger.LogWarning("Received invalid message from queue inside Indexing Worker. Skipping processing.");
                            return;
                        }

                        _logger.LogInformation(
                            "Processing {RequestType} request for Document with ID {Id}.",
                            "OCR",
                            payload.Id
                        );

                        SearchDocument document = new SearchDocument
                        {
                            Id = payload.Id,
                            Title = payload.Title,
                            Content = payload.OCRResult,
                            Category = payload.Category
                        };

                        await _elasticService.IndexAsync(document);

                        break;

                    case IndexingEventType.DocumentDeleted:
                        string id = _mqListener.ProcessDeleteDocumentPayload(ea);
                        if (id == null || string.IsNullOrEmpty(id))
                        {
                            _logger.LogWarning("Received invalid message from queue inside Indexing Worker. Skipping processing.");
                            return;
                        }

                        await _elasticService.RemoveAsync(id);

                        break;

                    case IndexingEventType.DocumentsDeleted:

                        await _elasticService.RemoveAllAsync();

                        break;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process document inside Indexing Worker." +
                    "\nError: {ErrorMessage}",
                    ex.Message
                );
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "{WorkerType} Worker is stopping...",
                "Indexing"
            );
            
            await _mqListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
