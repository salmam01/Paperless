using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace Paperless.Services.Services.Clients
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

        public async Task<string> GenerateSummaryAsync(string content)
        {
            _logger.LogInformation(
                "Generating summary using Cerebras API. Document content length: {ContentLength} characters.",
                content?.Length ?? 0
            );

            // Validate content: must not be null, empty, or whitespace
            // Also check for minimum meaningful length (at least 50 characters after trimming)
            const int MIN_CONTENT_LENGTH = 50;
            string trimmedContent = content?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(trimmedContent))
            {
                throw new ArgumentException("Document content cannot be empty.", nameof(content));
            }
            
            if (trimmedContent.Length < MIN_CONTENT_LENGTH)
            {
                throw new ArgumentException(
                    $"Document content is too short for summary generation. Minimum length: {MIN_CONTENT_LENGTH} characters, actual length: {trimmedContent.Length} characters.",
                    nameof(content)
                );
            }

            string url = _config.ApiUrl.Contains("{0}") 
                ? string.Format(_config.ApiUrl, _config.ModelName) 
                : _config.ApiUrl;

            // Cerebras API request format 
            var requestBody = new
            {
                model = _config.ModelName,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Analyze the following document and create a short and concise summary. " +
                                 $"Only reply with the summary. " +
                                 $"The document is:\n\n{content}"
                    }
                }
            };            
            try
            {
                _logger.LogInformation(
                    "Sending request to Cerebras API for summary generation. Model: {ModelName}, URL: {ApiUrl}",
                    _config.ModelName,
                    url
                );

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, requestBody);                
                response.EnsureSuccessStatusCode();

                JsonElement responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();

                // Cerebras API response format : choices[0].message.content
                string summary = responseContent
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
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
                    "HTTP error: while calling Cerebras API. Status: {StatusCode}",
                    ex.Data.Contains("StatusCode") ? ex.Data["StatusCode"] : "Unknown"
                );
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Timeout: while calling Cerebras API after {TimeoutSeconds} seconds.",
                    _config.TimeoutSeconds
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error: while calling Cerebras API: {ErrorMessage}",
                    ex.Message
                );
                throw;
            }
        }

        public async Task<Guid> SelectCategoryAsync(string summary, List<Category> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                throw new ArgumentException("Category list cannot be empty.", nameof(categories));
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new ArgumentException("Summary cannot be empty.", nameof(summary));
            }

            _logger.LogInformation(
                "Selecting category using Cerebras API. Summary length: {SummaryLength} characters. Available categories: {CategoryCount}",
                summary.Length,
                categories.Count
            );

            // Build category list string for the prompt
            string categoryList = string.Join("\n", categories.Select((c, index) => $"{index + 1}. {c.Name} (ID: {c.Id})"));

            string url = _config.ApiUrl.Contains("{0}") 
                ? string.Format(_config.ApiUrl, _config.ModelName) 
                : _config.ApiUrl;

            // Cerebras API request format
            var requestBody = new
            {
                model = _config.ModelName,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Based on the following document summary, select the most appropriate category from the provided list. " +
                                 $"Only reply with the category ID (the GUID) that best matches the summary. " +
                                 $"Do not include any other text, only the GUID.\n\n" +
                                 $"Available categories:\n{categoryList}\n\n" +
                                 $"Document summary:\n{summary}"
                    }
                }
            };

            try
            {
                _logger.LogInformation(
                    "Sending request to Cerebras API for category selection. Model: {ModelName}, URL: {ApiUrl}",
                    _config.ModelName,
                    url
                );

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, requestBody);
                response.EnsureSuccessStatusCode();

                JsonElement responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();

                // Cerebras API response format: choices[0].message.content
                string categoryIdString = responseContent
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                // Response clean up  
                categoryIdString = categoryIdString.Trim();
                
                if (Guid.TryParse(categoryIdString, out Guid selectedCategoryId))
                {
                    //  selected category ID exists in the provided list?
                    if (categories.Any(c => c.Id == selectedCategoryId))
                    {
                        _logger.LogInformation(
                            "Category successfully selected. Category ID: {CategoryId}",
                            selectedCategoryId
                        );
                        return selectedCategoryId;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "AI selected category ID {CategoryId} which is not in the provided list. Using first category as fallback.",
                            selectedCategoryId
                        );
                        return categories[0].Id;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to parse category ID from AI response: {Response}. Using first category as fallback.",
                        categoryIdString
                    );
                    return categories[0].Id;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error while calling Cerebras API for category selection. Status: {StatusCode}. Using first category as fallback.",
                    ex.Data.Contains("StatusCode") ? ex.Data["StatusCode"] : "Unknown"
                );
                return categories[0].Id;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Timeout while calling Cerebras API for category selection after {TimeoutSeconds} seconds. Using first category as fallback.",
                    _config.TimeoutSeconds
                );
                return categories[0].Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while calling Cerebras API for category selection: {ErrorMessage}. Using first category as fallback.",
                    ex.Message
                );
                return categories[0].Id;
            }
        }

        public static bool IsContentValid(string content, int minLength = 50)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            string trimmedContent = content.Trim();
            return trimmedContent.Length >= minLength;
        }
    }
}

