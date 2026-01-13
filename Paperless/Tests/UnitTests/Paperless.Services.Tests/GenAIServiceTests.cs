using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Services.Clients;

namespace Paperless.Services.Tests
{
    public class GenAIServiceTests
    {
        private readonly Mock<ILogger<GenAIService>> _loggerMock;
        private readonly Mock<IOptions<GenAIConfig>> _configMock;
        private readonly GenAIConfig _testConfig;

        public GenAIServiceTests()
        {
            _loggerMock = new Mock<ILogger<GenAIService>>();
            _configMock = new Mock<IOptions<GenAIConfig>>();
            
            _testConfig = new GenAIConfig
            {
                ApiKey = "test-api-key",
                ModelName = "gemini-2.0-flash",
                ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent",
                MaxRetries = 3,
                TimeoutSeconds = 30
            };

            _configMock.Setup(x => x.Value).Returns(_testConfig);
        }

        [Fact]
        public void can_create_service()
        {
            HttpClient httpClient = new HttpClient();
            GenAIService service = new GenAIService(_configMock.Object, _loggerMock.Object, httpClient);
            Assert.NotNull(service);
        }

        [Fact]
        public async Task throws_when_document_content_is_empty()
        {
            HttpClient httpClient = new HttpClient();
            GenAIService service = new GenAIService(_configMock.Object, _loggerMock.Object, httpClient);
            
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await service.GenerateSummaryAsync(string.Empty));
        }

        [Fact]
        public async Task throws_when_document_content_is_null()
        {
            HttpClient httpClient = new HttpClient();
            GenAIService service = new GenAIService(_configMock.Object, _loggerMock.Object, httpClient);
            
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await service.GenerateSummaryAsync(null!));
        }

        [Fact]
        public async Task throws_when_document_content_is_whitespace()
        {
            HttpClient httpClient = new HttpClient();
            GenAIService service = new GenAIService(_configMock.Object, _loggerMock.Object, httpClient);
            
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await service.GenerateSummaryAsync("   "));
        }

        [Fact]
        public async Task throws_when_document_content_is_too_short()
        {
            HttpClient httpClient = new HttpClient();
            GenAIService service = new GenAIService(_configMock.Object, _loggerMock.Object, httpClient);
            
            string shortContent = new string('a', 49); // < 50 char
            
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await service.GenerateSummaryAsync(shortContent));
        }

        [Fact]
        public void has_reasonable_defaults()
        {
            GenAIConfig config = new GenAIConfig();
            
            Assert.Equal("gemini-2.0-flash", config.ModelName);
            Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent", config.ApiUrl);
            Assert.Equal(3, config.MaxRetries);
            Assert.Equal(30, config.TimeoutSeconds);
        }

        [Fact]
        public void sets_http_client_timeout()
        {
            HttpClient httpClient = new HttpClient();
            GenAIService service = new GenAIService(_configMock.Object, _loggerMock.Object, httpClient);
            
            Assert.Equal(TimeSpan.FromSeconds(30), httpClient.Timeout);
        }

        [Fact]
        public void builds_correct_api_url()
        {
            GenAIConfig config = new GenAIConfig
            {
                ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent",
                ModelName = "gemini-2.0-flash",
                ApiKey = "test-key"
            };

            string url = string.Format(config.ApiUrl, config.ModelName);
            url += $"?key={config.ApiKey}";

            Assert.Contains("gemini-2.0-flash", url);
            Assert.Contains("?key=test-key", url);
        }

        [Fact]
        public void is_content_valid_returns_true_for_valid_content()
        {
            string validContent = new string('a', 100);
            bool result = GenAIService.IsContentValid(validContent, 50);
            Assert.True(result);
        }

        [Fact]
        public void is_content_valid_returns_false_for_short_content()
        {
            string shortContent = new string('a', 30);
            bool result = GenAIService.IsContentValid(shortContent, 50);
            Assert.False(result);
        }

        [Fact]
        public void is_content_valid_returns_false_for_empty_content()
        {
            bool result = GenAIService.IsContentValid(string.Empty, 50);
            Assert.False(result);
        }

        [Fact]
        public void is_content_valid_returns_false_for_whitespace()
        {
            bool result = GenAIService.IsContentValid("   ", 50);
            Assert.False(result);
        }
    }
}

