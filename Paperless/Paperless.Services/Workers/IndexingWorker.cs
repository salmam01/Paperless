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
                //  Get the incoming event type
                IndexingEventType eventType = _mqListener.HandleEventType(ea);
                
                switch(eventType)
                {
                    //  Store the OCR, summary and category results in ElasticSearch
                    case IndexingEventType.SummaryCompleted:

                        SummaryCompletedPayload payload = _mqListener.ProcessSummaryCompletedPayload(ea);
                        if (payload == null || payload.DocumentId == Guid.Empty)
                        {
                            _logger.LogWarning(
                                "Received invalid message from queue inside {WorkerType} Worker. Skipping processing.",
                                "Indexing"
                            );
                            return;
                        }

                        _logger.LogInformation(
                            "Processing {RequestType} request for Document with ID {Id}.",
                            "Document Indexing",
                            payload.DocumentId
                        );

                        SearchDocument document = new SearchDocument
                        {
                            Id = payload.DocumentId.ToString(),
                            Title = payload.Title,
                            Content = payload.OCRResult,
                            Category = payload.CategoryId.ToString()
                        };

                        await _elasticService.IndexAsync(document);

                        break;

                    //  Delete the document by ID
                    case IndexingEventType.DocumentDeleted:

                        string id = _mqListener.ProcessDeleteDocumentPayload(ea);
                        if (id == null || string.IsNullOrEmpty(id))
                        {
                            _logger.LogWarning(
                                "Received invalid message from queue inside {WorkerType} Worker. Skipping processing.",
                                "Indexing"
                            );
                            return;
                        }

                        _logger.LogInformation(
                            "Processing {RequestType} request for Document with ID {Id}.",
                            "Remove Document",
                            id
                        );

                        await _elasticService.RemoveAsync(id);

                        break;

                    //  Delete all documents inside the index
                    case IndexingEventType.DocumentsDeleted:

                        _logger.LogInformation(
                            "Processing {RequestType} request.",
                            "Remove All Documents"
                        );

                        await _elasticService.RemoveAllAsync();

                        break;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process document inside {WorkerType} Worker." +
                    "\nError: {ErrorMessage}",
                    "Indexing",
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
