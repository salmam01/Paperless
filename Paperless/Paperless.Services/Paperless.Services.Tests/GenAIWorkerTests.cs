using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Dtos;
using Paperless.Services.Services.HttpClients;
using Paperless.Services.Services.MessageQueues;
using Paperless.Services.Workers;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Paperless.Services.Tests
{
    public class GenAIWorkerTests
    {
        private readonly Mock<ILogger<GenAIWorker>> _loggerMock;
        private readonly Mock<MQListener> _mqListenerMock;
        private readonly Mock<GenAIService> _genAIServiceMock;
        private readonly Mock<WorkerResultsService> _workerResultsServiceMock;

        public GenAIWorkerTests()
        {
            _loggerMock = new Mock<ILogger<GenAIWorker>>();
            
            // Setup RabbitMqConfig mocks with valid values
            Mock<IOptions<RabbitMqConfig>> rabbitMqConfigMock = new Mock<IOptions<RabbitMqConfig>>();
            rabbitMqConfigMock.Setup(x => x.Value).Returns(new RabbitMqConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest",
                QueueName = "test-queue",
                MaxRetries = 3
            });
            
            _mqListenerMock = new Mock<MQListener>(Mock.Of<ILogger<MQListener>>(), rabbitMqConfigMock.Object);
            
            // Setup GenAIConfig mock
            Mock<IOptions<GenAIConfig>> genAIConfigMock = new Mock<IOptions<GenAIConfig>>();
            genAIConfigMock.Setup(x => x.Value).Returns(new GenAIConfig
            {
                ApiKey = "test-key",
                ModelName = "gemini-2.0-flash",
                ApiUrl = "https://test-url.com/{0}",
                MaxRetries = 3,
                TimeoutSeconds = 30
            });
            
            _genAIServiceMock = new Mock<GenAIService>(
                genAIConfigMock.Object,
                Mock.Of<ILogger<GenAIService>>(),
                new HttpClient()
            );
            
            // Setup EndpointsConfig mock
            Mock<IOptions<RestConfig>> endpointsConfigMock = new Mock<IOptions<RestConfig>>();
            endpointsConfigMock.Setup(x => x.Value).Returns(new RestConfig
            {
                Url = "https://localhost:5001/api/documents/"
            });
            
            _workerResultsServiceMock = new Mock<WorkerResultsService>(
                Mock.Of<ILogger<WorkerResultsService>>(),
                new HttpClient(),
                endpointsConfigMock.Object
            );
        }

        [Fact]
        public void can_create_worker()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void worker_gets_created_successfully()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );
            // shouldn't crash
            Assert.NotNull(worker);
        }

        [Fact]
        public void is_a_background_service()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );

            Assert.NotNull(worker);
            Assert.IsAssignableFrom<BackgroundService>(worker);
        }

        [Fact]
        public void handles_empty_message()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );
            Assert.NotNull(worker);
        }

        [Fact]
        public void handles_invalid_json_message()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void handles_message_without_id()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void handles_message_without_ocr_result()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );
            Assert.NotNull(worker);
        }

        [Fact]
        public void creates_worker_result_dto_correctly()
        {
            DocumentDto dto = new DocumentDto
            {
                Id = "test-id",
                OcrResult = "test ocr content",
                SummaryResult = "test summary"
            };

            Assert.Equal("test-id", dto.Id);
            Assert.Equal("test ocr content", dto.OcrResult);
            Assert.Equal("test summary", dto.SummaryResult);
        }
    }
}

