using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs.Payloads;
using Paperless.Services.Services.Clients;

namespace Paperless.Services.Tests
{
    public class WorkerResultsServiceTests
    {
        private readonly Mock<ILogger<ResultClient>> _loggerMock;
        private readonly Mock<IOptions<RESTConfig>> _configMock;
        private readonly RESTConfig _testConfig;

        public WorkerResultsServiceTests()
        {
            _loggerMock = new Mock<ILogger<ResultClient>>();
            _configMock = new Mock<IOptions<RESTConfig>>();
            _testConfig = new RESTConfig
            {
                Url = "https://localhost:5001/api/documents/"
            };

            _configMock.Setup(x => x.Value).Returns(_testConfig);
        }

        [Fact]
        public void can_create_service()
        {
            HttpClient httpClient = new HttpClient();
            ResultClient service = new ResultClient(
                _loggerMock.Object, httpClient, _configMock.Object
            );
            Assert.NotNull(service);
        }

        [Fact]
        public void has_reasonable_defaults()
        {
            RESTConfig config = new RESTConfig();
            Assert.Equal(string.Empty, config.Url);
        }
        

        [Fact]
        public void creates_summary_completed_payload()
        {
            Guid docId = Guid.NewGuid();
            Guid catId = Guid.NewGuid();
            SummaryCompletedPayload payload = new SummaryCompletedPayload
            {
                DocumentId = docId,
                Title = "Test Document",
                CategoryId = catId,
                CategoryName = "Test Category",
                OCRResult = "Extracted text from document",
                Summary = "This is a summary of the document"
            };

            Assert.Equal(docId, payload.DocumentId);
            Assert.Equal("Test Document", payload.Title);
            Assert.Equal(catId, payload.CategoryId);
            Assert.Equal("Test Category", payload.CategoryName);
            Assert.Equal("Extracted text from document", payload.OCRResult);
            Assert.Equal("This is a summary of the document", payload.Summary);
        }

        [Fact]
        public void summary_completed_payload_has_default_values()
        {
            SummaryCompletedPayload payload = new SummaryCompletedPayload();

            Assert.Equal(Guid.Empty, payload.DocumentId);
            Assert.Equal(string.Empty, payload.Title);
            Assert.Equal(Guid.Empty, payload.CategoryId);
            Assert.Equal(string.Empty, payload.CategoryName);
            Assert.Equal(string.Empty, payload.OCRResult);
            Assert.Equal(string.Empty, payload.Summary);
        }
    }
}

