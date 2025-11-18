using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Dtos;
using Paperless.Services.Services.HttpClients;
using System.Text;

namespace Paperless.Services.Tests
{
    public class WorkerResultsServiceTests
    {
        private readonly Mock<ILogger<WorkerResultsService>> _loggerMock;
        private readonly Mock<IOptions<EndpointsConfig>> _configMock;
        private readonly EndpointsConfig _testConfig;

        public WorkerResultsServiceTests()
        {
            _loggerMock = new Mock<ILogger<WorkerResultsService>>();
            _configMock = new Mock<IOptions<EndpointsConfig>>();
            
            _testConfig = new EndpointsConfig
            {
                Rest = "https://localhost:5001/api/documents/"
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
            EndpointsConfig config = new EndpointsConfig();
            Assert.Equal(string.Empty, config.Rest);
        }
        

        [Fact]
        public void creates_worker_result_dto()
        {
            WorkerResultDto dto = new WorkerResultDto
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
            WorkerResultDto dto = new WorkerResultDto();

            Assert.Equal(string.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.OcrResult);
            Assert.Equal(string.Empty, dto.SummaryResult);
        }
    }
}

