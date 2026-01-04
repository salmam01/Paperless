using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Services.Clients
{
    public class ResultClient
    {
        private readonly ILogger<ResultClient> _logger;
        private readonly HttpClient _client;
        private readonly Uri _baseUrl;

        public ResultClient(
            ILogger<ResultClient> logger,
            HttpClient client, 
            IOptions<RESTConfig> config
        ) {
            _logger = logger;
            _client = client;
            _baseUrl = new Uri(config.Value.Url);
        }

        public async Task PostWorkerResultsAsync(SummaryCompletedPayload payload)
        {
            _logger.LogInformation(
                "Sending received Worker Results for Document with ID {Id} to REST server.",
                payload.DocumentId
            );

            using StringContent content = new(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            Uri endpoint = new Uri(_baseUrl, $"{payload.DocumentId}");

            using HttpResponseMessage response = await _client.PostAsync(
                endpoint,
                content
            );

            string jsonResponse = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Sent results for Document with ID {Id} to REST server successfully. OCR result length: {OcrLength}, Summary length: {SummaryLength}",
                    payload.DocumentId,
                    payload.OCRResult?.Length ?? 0,
                    payload.Summary?.Length ?? 0
                );
            } else {
                _logger.LogWarning(
                    "REST server returned non-success status {Status} for ID {Id}. Body: {Body}",
                    response.StatusCode,
                    payload.DocumentId,
                    jsonResponse
                );
            }
        }
    }
}
