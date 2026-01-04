using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Clients;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Services.Messaging.Publishers;
using RabbitMQ.Client.Events;

namespace Paperless.Services.Workers
{
    public class GenAIWorker : BackgroundService
    {
        private readonly ILogger<GenAIWorker> _logger;
        private readonly GenAIListener _mqListener;
        private readonly MQPublisher _mqPublisher;
        private readonly SummaryService _genAIService;
        private readonly ResultClient _workerResultsService;

        public GenAIWorker(
            ILogger<GenAIWorker> logger,
            GenAIListener mqListener,
            MQPublisher mqPublisher,
            SummaryService genAIService,
            ResultClient workerResultsService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _mqPublisher = mqPublisher;
            _genAIService = genAIService;
            _workerResultsService = workerResultsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "{WorkerType} Worker is starting ...",
                "Summary"
            );

            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            try
            {
                //  Deserialize incoming payload
                //  TODO: OCRCompletedPayload contains category list => use that for summary worker to get the category
                OCRCompletedPayload payload = _mqListener.ProcessPayload(ea);

                if (payload == null ||
                    payload.DocumentId == Guid.Empty || 
                    string.IsNullOrEmpty(payload.Title) || 
                    string.IsNullOrEmpty(payload.OCRResult) ||
                    payload.Categories.Count == 0
                ) {
                    _logger.LogWarning(
                        "Received invalid message from queue inside {WorkerType} Worker. Skipping processing.",
                        "Summary"
                    );
                    return;
                }

                _logger.LogInformation(
                    "Processing {RequestType} request for Document with ID {Id}. " +
                    "OCR result length: {OcrLength} characters.",
                    "Summary",
                    payload.DocumentId,
                    payload.OCRResult?.Length ?? 0
                );
                
                //  TODO: this step should be in a helper method or class
                //  Check if OCR result has meaningful content (minimum 50 characters after trimming)
                const int MIN_CONTENT_LENGTH = 50;
                string trimmedContent = payload.OCRResult?.Trim() ?? string.Empty;
                
                string summary;
                //  TODO: AI selects a category from the list based on the generated summary
                string category;

                if (string.IsNullOrWhiteSpace(trimmedContent) || trimmedContent.Length < MIN_CONTENT_LENGTH)
                {
                    _logger.LogWarning(
                        "Document {DocumentId} has insufficient content for summary generation." +
                        "\nContent length: {ContentLength} characters (minimum: {MinLength})." +
                        "\nSetting default summary message.",
                        payload.DocumentId,
                        trimmedContent.Length,
                        MIN_CONTENT_LENGTH
                    );
                    summary = "No summary available: Document doesn't contain enough readable text.";
                }
                else
                {
                    try
                    {
                        summary = await _genAIService.GenerateSummaryAsync(payload.OCRResult);
                    }
                    catch (ArgumentException argEx)
                    {
                        // Content validation failed - set default summary
                        _logger.LogWarning(
                            argEx,
                            "Document {DocumentId} failed content validation for summary generation. Setting default summary.\nError: {ErrorMessage}",
                            payload.DocumentId,
                            argEx.Message
                        );
                        summary = "No summary available - Document doesn't contain enough readable text..";                    }
                    catch (Exception apiEx)
                    {
                        // API call failed - set default summary to prevent message rejection
                        _logger.LogError(
                            apiEx,
                            "Failed to generate summary via API for document {DocumentId}. Setting default summary.\nError: {ErrorMessage}",
                            payload.DocumentId,
                            apiEx.Message
                        );
                        summary = "No summary available: Error generating summary.";
                    }
                }

                SummaryCompletedPayload summaryPayload = new SummaryCompletedPayload
                {
                    DocumentId = payload.DocumentId,
                    Title = payload.Title,
                    CategoryId = payload.Categories[0].Id, // TODO: update this! (PLACEHOLDER)
                    OCRResult = payload.OCRResult,
                    Summary = summary
                };

                _logger.LogInformation(
                    "Successfully processed summary for document {DocumentId}." +
                    "\nSummary length: {SummaryLength}" +
                    "\n*** Summary ***\n{Summary}",
                    payload.DocumentId,
                    summary.Length,
                    summary
                );

                //  Send OCR, Summary and Category results to REST server and IndexingWorker
                await _workerResultsService.PostWorkerResultsAsync(summaryPayload);
                await _mqPublisher.PublishSummaryResult(summaryPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process document inside Summary Worker." +
                    "\nError: {ErrorMessage}",
                    ex.Message
                );
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "{WorkerType} Worker is stopping...",
                "Summary"
            );

            await _mqListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}