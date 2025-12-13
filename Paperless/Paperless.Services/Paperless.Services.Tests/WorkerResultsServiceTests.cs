using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.DTOs;
using Paperless.Services.Services.HttpClients;

namespace Paperless.Services.Tests
{
    public class WorkerResultsServiceTests
    {
        private readonly Mock<ILogger<WorkerResultsService>> _loggerMock;
        private readonly Mock<IOptions<RESTConfig>> _configMock;
        private readonly RESTConfig _testConfig;

        public WorkerResultsServiceTests()
        {
            _loggerMock = new Mock<ILogger<WorkerResultsService>>();
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
            WorkerResultsService service = new WorkerResultsService(
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
        public void creates_worker_result_dto()
        {
            DocumentDTO dto = new DocumentDTO
            {
                Id = "doc-123",
                OcrResult = "Extracted text from document",
                SummaryResult = "This is a summary of the document"
            };

            Assert.Equal("doc-123", dto.Id);
            Assert.Equal("Extracted text from document", dto.OcrResult);
            Assert.Equal("This is a summary of the document", dto.SummaryResult);
        }

        [Fact]
        public void worker_result_dto_has_default_values()
        {
            DocumentDTO dto = new DocumentDTO();

            Assert.Equal(string.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.OcrResult);
            Assert.Equal(string.Empty, dto.SummaryResult);
        }
    }
}

