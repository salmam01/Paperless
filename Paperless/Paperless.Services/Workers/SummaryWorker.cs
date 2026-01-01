using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.HttpClients;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Services.Messaging.Publishers;
using RabbitMQ.Client.Events;

namespace Paperless.Services.Workers
{
    public class SummaryWorker : BackgroundService
    {
        private readonly ILogger<SummaryWorker> _logger;
        private readonly SummaryListener _mqListener;
        private readonly MQPublisher _mqPublisher;
        private readonly SummaryService _genAIService;
        private readonly WorkerResultsService _workerResultsService;

        public SummaryWorker(
            ILogger<SummaryWorker> logger,
            SummaryListener mqListener,
            MQPublisher mqPublisher,
            SummaryService genAIService,
            WorkerResultsService workerResultsService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _mqPublisher = mqPublisher;
            _genAIService = genAIService;
            _workerResultsService = workerResultsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Summary Worker is starting...");
            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            /*
            _logger.LogInformation(
                "Processing message from queue inside Summary Worker.\nMessage length: {MessageLength} characters.",
                message?.Length ?? 0
            );*/

            try
            {
                //  TODO: summary payload contains category list => use that for the summary
                OCRCompletedPayload payload = _mqListener.ProcessPayload(ea);

                if (payload == null || string.IsNullOrEmpty(payload.Id) || string.IsNullOrEmpty(payload.Title) || string.IsNullOrEmpty(payload.OCRResult))
                {
                    _logger.LogWarning("Received invalid message from queue inside Summary Worker. Skipping processing.");
                    return;
                }

                _logger.LogInformation(
                    "Document {DocumentId} content retrieved.\nOCR result length: {OcrLength} characters.",
                    payload.Id,
                    payload.OCRResult?.Length ?? 0
                );
                
                // Check if OCR result has meaningful content (minimum 50 characters after trimming)
                const int MIN_CONTENT_LENGTH = 50;
                string trimmedContent = payload.OCRResult?.Trim() ?? string.Empty;
                
                string summary;
                //  TODO: AI selects a category from the list based on the summary
                string category;

                if (string.IsNullOrWhiteSpace(trimmedContent) || trimmedContent.Length < MIN_CONTENT_LENGTH)
                {
                    _logger.LogWarning(
                        "Document {DocumentId} has insufficient content for summary generation." +
                        "\nContent length: {ContentLength} characters (minimum: {MinLength})." +
                        "\nSetting default summary message.",
                        payload.Id,
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
                            payload.Id,
                            argEx.Message
                        );
                        summary = "No summary available - Document doesn't contain enough readable text..";                    }
                    catch (Exception apiEx)
                    {
                        // API call failed - set default summary to prevent message rejection
                        _logger.LogError(
                            apiEx,
                            "Failed to generate summary via API for document {DocumentId}. Setting default summary.\nError: {ErrorMessage}",
                            payload.Id,
                            apiEx.Message
                        );
                        summary = "No summary available: Error generating summary.";
                    }
                }

                SummaryCompletedPayload summaryPayload = new SummaryCompletedPayload
                {
                    Id = payload.Id,
                    Title = payload.Title,
                    Category = string.Empty, // TODO: update this!
                    OCRResult = payload.OCRResult,
                    Summary = summary
                };

                _logger.LogInformation(
                    "Successfully processed summary for document {DocumentId}." +
                    "\nSummary length: {SummaryLength}" +
                    "\n*** Summary ***\n{Summary}",
                    payload.Id,
                    summary.Length,
                    summary
                );

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
            _logger.LogInformation("GenAI Worker is stopping...");
            await _mqListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}