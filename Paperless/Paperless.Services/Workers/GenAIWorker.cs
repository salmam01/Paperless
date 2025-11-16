using Paperless.Services.Services;
using Paperless.Services.Services.MessageQueue;
using RabbitMQ.Client.Events;

namespace Paperless.Services.Workers
{
    public class GenAIWorker : BackgroundService
    {
        private readonly ILogger<GenAIWorker> _logger;
        private readonly MQListener _mqListener;
        private readonly GenAIService _genAIService;

        public GenAIWorker(
            ILogger<GenAIWorker> logger,
            [FromKeyedServices("SummaryListener")] MQListener mqListener,
            GenAIService genAIService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _genAIService = genAIService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GenAI Worker is starting...");
            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(string documentIdString, BasicDeliverEventArgs ea)
        {
            if (!Guid.TryParse(documentIdString, out Guid documentId))
            {
                _logger.LogError(
                    "Invalid document ID format received from queue: {DocumentIdString}",
                    documentIdString
                );
                throw new ArgumentException($"Invalid document ID format: {documentIdString}");
            }

            _logger.LogInformation(
                "Document {DocumentId} for summary generation processing.",
                documentId
            );

            //  no...don't do this
            using IServiceScope scope = _serviceProvider.CreateScope();
            var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

            try
            {
                //  or this...
                var document = await documentService.GetDocumentAsync(documentId);
                
                if (string.IsNullOrWhiteSpace(document.Content))
                {
                    _logger.LogWarning(
                        "Document {DocumentId} has no content. Skipping summary generation.",
                        documentId
                    );
                    return;
                }

                _logger.LogInformation(
                    "Document {DocumentId} content retrieved. Content length: {ContentLength}",
                    documentId,
                    document.Content.Length
                );
                
                string summary = await _genAIService.GenerateSummaryAsync(document.Content);

                // Update document with summary via BL Layer
                await documentService.UpdateDocumentSummaryAsync(documentId, summary);

                _logger.LogInformation(
                    "Successfully generated & saved summary for document {DocumentId}. Summary length: {SummaryLength}",
                    documentId,
                    summary.Length
                );
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(
                    ex,
                    "Document {DocumentId} not found in database.",
                    documentId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to generate summary for document {DocumentId}. Error: {ErrorMessage}",
                    documentId,
                    ex.Message
                );
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GenAI Worker is stopping...");
            await _mqListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
