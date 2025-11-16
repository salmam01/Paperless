using Paperless.Services.Models.Dtos;
using Paperless.Services.Services.HttpClients;
using Paperless.Services.Services.MessageQueue;
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
            try
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
                
                string summary = await _genAIService.GenerateSummaryAsync(ocrResult);

                WorkerResultDto workerResultDto = new WorkerResultDto
                {
                    Id = id,
                    OcrResult = ocrResult,
                    SummaryResult = summary
                };

                _logger.LogInformation(
                    "Successfully generated & saved summary for document {DocumentId}. Summary length: {SummaryLength}\n*** Summary ***\n{Summary}",
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
                    "Failed to generate summary for document. Error: {ErrorMessage}",
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
