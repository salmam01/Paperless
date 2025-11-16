using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using System.Net.Http.Json;
using System.Text.Json;

namespace Paperless.Services.Services
{
    public class GenAIService
    {
        private readonly GenAIConfig _config;
        private readonly ILogger<GenAIService> _logger;
        private readonly HttpClient _httpClient;

        public GenAIService(
            IOptions<GenAIConfig> config,
            ILogger<GenAIService> logger,
            HttpClient httpClient
        ) {
            _config = config.Value;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        }

        public async Task<string> GenerateSummaryAsync(string documentContent)
        {
            if (string.IsNullOrWhiteSpace(documentContent))
            {
                throw new ArgumentException("Document content cann't be empty.", nameof(documentContent));
            }

            string url = string.Format(_config.ApiUrl, _config.ModelName);
            url += $"?key={_config.ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {  
                                text = $"Erstelle eine Zusammenfassung des folgenden Dokuments auf deutsch :\n\n{documentContent}"
                            }
                        }
                    }
                },
                generationConfig = new //das nochmal recherchieren
                {
                    temperature = 0.7,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 1024
                }
            };

            try
            {
                _logger.LogInformation("Sending request to Google Gemini API for summary generation.");

                using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, requestBody);
                response.EnsureSuccessStatusCode();

                JsonElement responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                
                string summary = responseContent
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                _logger.LogInformation(
                    "Summary successfully generated. Summary length: {SummaryLength}",
                    summary.Length
                );

                return summary;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error: while calling Google Gemini API. Status: {StatusCode}",
                    ex.Data.Contains("StatusCode") ? ex.Data["StatusCode"] : "Unknown"
                );
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Timeout: :while calling Google Gemini API after {TimeoutSeconds} seconds.",
                    _config.TimeoutSeconds
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error: while calling Google Gemini API: {ErrorMessage}",
                    ex.Message
                );
                throw;
            }
        }
    }
}

