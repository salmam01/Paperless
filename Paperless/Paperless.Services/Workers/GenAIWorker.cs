using Paperless.Services.Models.Dtos;
using Paperless.Services.Services.HttpClients;
using Paperless.Services.Services.MessageQueues;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Paperless.Services.Workers
{
    public class GenAIWorker : BackgroundService
    {
        private readonly ILogger<GenAIWorker> _logger;
        private readonly MQListener _mqListener;
        private readonly GenAIService _genAIService;
        private readonly WorkerResultsService _workerResultsService;

        public GenAIWorker(
            ILogger<GenAIWorker> logger,
            [FromKeyedServices("SummaryListener")] MQListener mqListener,
            GenAIService genAIService,
            WorkerResultsService workerResultsService
        ) {
            _logger = logger;
            _mqListener = mqListener;
            _genAIService = genAIService;
            _workerResultsService = workerResultsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GenAI Worker is starting...");
            await _mqListener.StartListeningAsync(HandleMessageAsync, stoppingToken);
        }

        private async Task HandleMessageAsync(string message, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation(
                "Processing message from queue. Message length: {MessageLength} characters.",
                message?.Length ?? 0
            );

            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Received empty message from queue. Skipping processing.");
                    return;
                }

                Dictionary<string, string> jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(message)
                    ?? new Dictionary<string, string>();

                if (jsonObject == null || !jsonObject.ContainsKey("Id") || !jsonObject.ContainsKey("OcrResult"))
                {
                    _logger.LogWarning(
                        "Document has no content. Skipping summary generation. Message: {Message}",
                        message
                    );
                    return;
                }
                string id = jsonObject["Id"];
                string ocrResult = jsonObject["OcrResult"];

                _logger.LogInformation(
                    "Document {DocumentId} content retrieved. OCR result length: {OcrLength} characters.",
                    id,
                    ocrResult?.Length ?? 0
                );
                
                // Check if OCR result has meaningful content (minimum 50 characters after trimming)
                const int MIN_CONTENT_LENGTH = 50;
                string trimmedContent = ocrResult?.Trim() ?? string.Empty;
                
                string summary;
                if (string.IsNullOrWhiteSpace(trimmedContent) || trimmedContent.Length < MIN_CONTENT_LENGTH)
                {
                    _logger.LogWarning(
                        "Document {DocumentId} has insufficient content for summary generation. Content length: {ContentLength} characters (minimum: {MinLength}). Setting default summary.",
                        id,
                        trimmedContent.Length,
                        MIN_CONTENT_LENGTH
                    );
                    summary = "No summary available - Document doesn't contain enough readable text.";
                }
                else
                {
                    try
                    {
                        summary = await _genAIService.GenerateSummaryAsync(ocrResult);
                    }
                    catch (ArgumentException argEx)
                    {
                        // Content validation failed - set default summary
                        _logger.LogWarning(
                            argEx,
                            "Document {DocumentId} failed content validation for summary generation. Setting default summary. Error: {ErrorMessage}",
                            id,
                            argEx.Message
                        );
                        summary = "No summary available - Document doesn't contain enough readable text..";                    }
                    catch (Exception apiEx)
                    {
                        // API call failed - set default summary to prevent message rejection
                        _logger.LogError(
                            apiEx,
                            "Failed to generate summary via API for document {DocumentId}. Setting default summary. Error: {ErrorMessage}",
                            id,
                            apiEx.Message
                        );
                        summary = "No summary available - Error generating summary.";
                    }
                }

                WorkerResultDto workerResultDto = new WorkerResultDto
                {
                    Id = id,
                    OcrResult = ocrResult,
                    SummaryResult = summary
                };

                _logger.LogInformation(
                    "Successfully processed summary for document {DocumentId}. Summary length: {SummaryLength}\n*** Summary ***\n{Summary}",
                    id,
                    summary.Length,
                    summary
                );

                await _workerResultsService.PostWorkerResultsAsync(workerResultDto);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(
                    ex,
                    "Document not found in database."
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process document. Error: {ErrorMessage}",
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