using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Paperless.Services.Services.HttpClients
{
    public class WorkerResultsService
    {
        private readonly ILogger<WorkerResultsService> _logger;
        private readonly HttpClient _client;
        private readonly Uri _baseUrl;

        public WorkerResultsService(
            ILogger<WorkerResultsService> logger,
            HttpClient client, 
            IOptions<RestConfig> config
        ) {
            _logger = logger;
            _client = client;
            _baseUrl = new Uri(config.Value.Url);
        }

        public async Task PostWorkerResultsAsync(DocumentDto workerResult)
        {
            _logger.LogInformation(
                "Sending received Worker Results for Document with ID {Id} to REST server.",
                workerResult.Id
            );

            using StringContent content = new(
                JsonSerializer.Serialize(workerResult),
                Encoding.UTF8,
                "application/json"
            );

            Uri endpoint = new Uri(_baseUrl, $"{workerResult.Id}");

            using HttpResponseMessage response = await _client.PostAsync(
                endpoint,
                content
            );

            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Sent results for Document with ID {Id} to REST server successfully. OCR result length: {OcrLength}, Summary length: {SummaryLength}",
                    workerResult.Id,
                    workerResult.OcrResult?.Length ?? 0,
                workerResult.SummaryResult?.Length ?? 0
                );
            } else {
                _logger.LogWarning(
                    "REST server returned non-success status {Status} for ID {Id}. Body: {Body}",
                    response.StatusCode,
                    workerResult.Id,
                    jsonResponse
                );
            }
        }
    }
}
