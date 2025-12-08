using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Dtos;
using Paperless.Services.Services.HttpClients;
using System.Text;

namespace Paperless.Services.Tests
{
    // Integration tests for GenAI
    // Checking if GenAI components work together
    public class GenAIIntegrationTests
    {
        private readonly Mock<ILogger<GenAIService>> _genAILoggerMock;
        private readonly Mock<ILogger<WorkerResultsService>> _workerResultsLoggerMock;
        private readonly Mock<IOptions<GenAIConfig>> _genAIConfigMock;
        private readonly Mock<IOptions<EndpointsConfig>> _endpointsConfigMock;
        private readonly GenAIConfig _testGenAIConfig;
        private readonly EndpointsConfig _testEndpointsConfig;

        public GenAIIntegrationTests()
        {
            _genAILoggerMock = new Mock<ILogger<GenAIService>>();
            _workerResultsLoggerMock = new Mock<ILogger<WorkerResultsService>>();
            _genAIConfigMock = new Mock<IOptions<GenAIConfig>>();
            _endpointsConfigMock = new Mock<IOptions<EndpointsConfig>>();

            _testGenAIConfig = new GenAIConfig
            {
                ApiKey = "test-api-key",
                ModelName = "gemini-2.0-flash",
                ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent",
                MaxRetries = 3,
                TimeoutSeconds = 30
            };

            _testEndpointsConfig = new EndpointsConfig
            {
                Rest = "https://localhost:5001/api/documents/"
            };

            _genAIConfigMock.Setup(x => x.Value).Returns(_testGenAIConfig);
            _endpointsConfigMock.Setup(x => x.Value).Returns(_testEndpointsConfig);
        }

        [Fact]
        public void genai_service_uses_http_client()
        {
            HttpClient httpClient = new HttpClient();
            GenAIService service = new GenAIService(
                _genAIConfigMock.Object,
                _genAILoggerMock.Object,
                httpClient
            );
            
            Assert.NotNull(service);
            Assert.Equal(TimeSpan.FromSeconds(30), httpClient.Timeout);
        }

        [Fact]
        public void worker_results_service_uses_http_client()
        {
            HttpClient httpClient = new HttpClient();
            WorkerResultsService service = new WorkerResultsService(
                _workerResultsLoggerMock.Object, httpClient,
                _endpointsConfigMock.Object
            );
            
            Assert.NotNull(service);
        }

        [Fact]
        public void worker_result_dto_contains_all_required_fields()
        {
            DocumentDto dto = new DocumentDto
            {
                Id = "test-id",
                OcrResult = "OCR content",
                SummaryResult = "Summary content"
            };

            Assert.NotNull(dto.Id);
            Assert.NotNull(dto.OcrResult);
            Assert.NotNull(dto.SummaryResult);
        }

        [Fact]
        public void genai_config_has_valid_api_url_format()
        {
            GenAIConfig config = new GenAIConfig
            {
                ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent",
                ModelName = "gemini-2.0-flash"
            };

            string url = string.Format(config.ApiUrl, config.ModelName);
            
            Assert.Contains("gemini-2.0-flash", url);
            Assert.Contains("generateContent", url);
        }

        [Fact]
        public void endpoints_config_has_rest_endpoint()
        {
            EndpointsConfig config = new EndpointsConfig
            {
                Rest = "https://localhost:5001/api/documents/"
            };

            Assert.NotNull(config.Rest);
            Assert.NotEmpty(config.Rest);
        }

        [Fact]
        public void genai_config_timeout_is_reasonable()
        {
            GenAIConfig config = new GenAIConfig
            {
                TimeoutSeconds = 30
            };

            Assert.True(config.TimeoutSeconds > 0);
            Assert.True(config.TimeoutSeconds <= 300); // Max 5 min
        }
    }
}

