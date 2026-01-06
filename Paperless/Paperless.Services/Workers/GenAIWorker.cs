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
                    "OCR result length: {OcrLength} characters. " +
                    "Available categories: {CategoryCount}",
                    "Summary",
                    payload.DocumentId,
                    payload.OCRResult?.Length ?? 0,
                    payload.Categories.Count
                );
                
                //  Check if OCR result has meaningful content (minimum 50 characters after trimming)
                const int MIN_CONTENT_LENGTH = 50;
                string trimmedContent = payload.OCRResult?.Trim() ?? string.Empty;
                
                string summary;
                Guid selectedCategoryId;

                if (!SummaryService.IsContentValid(trimmedContent, MIN_CONTENT_LENGTH))
                {
                    _logger.LogWarning(
                        "Document {DocumentId} has insufficient content for summary generation." +
                        "\nContent length: {ContentLength} characters (minimum: {MinLength})." +
                        "\nSetting default summary message and using first category.",
                        payload.DocumentId,
                        trimmedContent.Length,
                        MIN_CONTENT_LENGTH
                    );
                    summary = "No summary available: Document doesn't contain enough readable text.";
                    selectedCategoryId = payload.Categories[0].Id;
                }
                else
                {
                    try
                    {
                        // Generate summary first
                        summary = await _genAIService.GenerateSummaryAsync(payload.OCRResult);
                        
                        // Then select category based on the generated summary
                        try
                        {
                            selectedCategoryId = await _genAIService.SelectCategoryAsync(summary, payload.Categories);
                        }
                        catch (Exception categoryEx)
                        {
                            _logger.LogWarning(
                                categoryEx,
                                "Failed to select category via AI for document {DocumentId}. Using first category as fallback.\nError: {ErrorMessage}",
                                payload.DocumentId,
                                categoryEx.Message
                            );
                            selectedCategoryId = payload.Categories[0].Id;
                        }
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
                        summary = "No summary available - Document doesn't contain enough readable text..";
                        selectedCategoryId = payload.Categories[0].Id;
                    }
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
                        selectedCategoryId = payload.Categories[0].Id;
                    }
                }

                SummaryCompletedPayload summaryPayload = new SummaryCompletedPayload
                {
                    DocumentId = payload.DocumentId,
                    Title = payload.Title,
                    CategoryId = selectedCategoryId,
                    OCRResult = payload.OCRResult,
                    Summary = summary
                };

                _logger.LogInformation(
                    "Successfully processed summary for document {DocumentId}." +
                    "\nSummary length: {SummaryLength}" +
                    "\nSelected Category ID: {CategoryId}" +
                    "\n*** Summary ***\n{Summary}",
                    payload.DocumentId,
                    summary.Length,
                    selectedCategoryId,
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