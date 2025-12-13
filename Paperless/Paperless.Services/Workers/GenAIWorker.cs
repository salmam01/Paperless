using Paperless.Services.Models.DTOs;
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
                "Processing message from queue inside GenAI Worker.\nMessage length: {MessageLength} characters.",
                message?.Length ?? 0
            );

            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("Received empty message from queue inside GenAI Worker. Skipping processing.");
                    return;
                }

                DocumentDTO document = ParseMessage(message);
                if (document == null || string.IsNullOrEmpty(document.Id))
                {
                    _logger.LogWarning("Received invalid message from queue inside GenAI Worker. Skipping processing.");
                    return;
                }

                _logger.LogInformation(
                    "Document {DocumentId} content retrieved.\nOCR result length: {OcrLength} characters.",
                    document.Id,
                    document.OcrResult?.Length ?? 0
                );
                
                // Check if OCR result has meaningful content (minimum 50 characters after trimming)
                const int MIN_CONTENT_LENGTH = 50;
                string trimmedContent = document.OcrResult?.Trim() ?? string.Empty;
                
                string summary;
                if (string.IsNullOrWhiteSpace(trimmedContent) || trimmedContent.Length < MIN_CONTENT_LENGTH)
                {
                    _logger.LogWarning(
                        "Document {DocumentId} has insufficient content for summary generation." +
                        "\nContent length: {ContentLength} characters (minimum: {MinLength})." +
                        "\nSetting default summary message.",
                        document.Id,
                        trimmedContent.Length,
                        MIN_CONTENT_LENGTH
                    );
                    summary = "No summary available: Document doesn't contain enough readable text.";
                }
                else
                {
                    try
                    {
                        summary = await _genAIService.GenerateSummaryAsync(document.OcrResult);
                    }
                    catch (ArgumentException argEx)
                    {
                        // Content validation failed - set default summary
                        _logger.LogWarning(
                            argEx,
                            "Document {DocumentId} failed content validation for summary generation. Setting default summary.\nError: {ErrorMessage}",
                            document.Id,
                            argEx.Message
                        );
                        summary = "No summary available - Document doesn't contain enough readable text..";                    }
                    catch (Exception apiEx)
                    {
                        // API call failed - set default summary to prevent message rejection
                        _logger.LogError(
                            apiEx,
                            "Failed to generate summary via API for document {DocumentId}. Setting default summary.\nError: {ErrorMessage}",
                            document.Id,
                            apiEx.Message
                        );
                        summary = "No summary available: Error generating summary.";
                    }
                }

                DocumentDTO workerResultDto = new DocumentDTO
                {
                    Id = document.Id,
                    OcrResult = document.OcrResult,
                    SummaryResult = summary
                };

                _logger.LogInformation(
                    "Successfully processed summary for document {DocumentId}." +
                    "\nSummary length: {SummaryLength}" +
                    "\n*** Summary ***\n{Summary}",
                    document.Id,
                    summary.Length,
                    summary
                );

                await _workerResultsService.PostWorkerResultsAsync(workerResultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process document inside GenAI Worker." +
                    "\nError: {ErrorMessage}",
                    ex.Message
                );
                throw;
            }
        }

        //  Add a helper class
        private DocumentDTO ParseMessage(string message)
        {
            Dictionary<string, string> jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(message)
                ?? new Dictionary<string, string>();

            DocumentDTO document = new();

            if (jsonObject == null || !jsonObject.ContainsKey("Id") || !jsonObject.ContainsKey("Title") || !jsonObject.ContainsKey("OcrResult"))
            {
                _logger.LogWarning(
                    "Document is NULL. Skipping summary generation." +
                    "\nMessage: {Message}",
                    message
                );
                return document;
            } 

            document.Id = jsonObject["Id"];
            document.Title = jsonObject["Title"];
            document.OcrResult = jsonObject["OcrResult"];

            return document;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GenAI Worker is stopping...");
            await _mqListener.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}