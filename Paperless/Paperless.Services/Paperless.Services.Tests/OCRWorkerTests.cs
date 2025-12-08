using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.MessageQueue;
using Paperless.Services.Services.OCR;
using Paperless.Services.Workers;
using System.Text;

namespace Paperless.Services.Tests
{
    public class OCRWorkerTests
    {
        private readonly Mock<ILogger<OcrWorker>> _loggerMock;
        private readonly Mock<MQListener> _mqListenerMock;
        private readonly Mock<MQPublisher> _mqPublisherMock;
        private readonly StorageService _storageService;
        private readonly OcrService _ocrService;

        public OCRWorkerTests()
        {
            _loggerMock = new Mock<ILogger<OcrWorker>>();
            
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
            _mqPublisherMock = new Mock<MQPublisher>(Mock.Of<ILogger<MQPublisher>>(), rabbitMqConfigMock.Object);

            // real instances with mocked dependencies
            Mock<IOptions<MinIoConfig>> minIoConfigMock = new Mock<IOptions<Configurations.MinIoConfig>>();
            minIoConfigMock.Setup(x => x.Value).Returns(new Configurations.MinIoConfig
            {
                Endpoint = "localhost:9000",
                Username = "minioadmin",
                Password = "minioadmin",
                BucketName = "test-bucket"
            });

            Mock<IOptions<OcrConfig>> ocrConfigMock = new Mock<IOptions<OcrConfig>>();
            ocrConfigMock.Setup(x => x.Value).Returns(new OcrConfig());

            _storageService = new StorageService(
                minIoConfigMock.Object,
                Mock.Of<ILogger<StorageService>>()
            );

            _ocrService = new OcrService(
                ocrConfigMock.Object,
                Mock.Of<ILogger<OcrService>>()
            );
        }

        [Fact]
        public void can_create_worker()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _mqPublisherMock.Object,
                _storageService,
                _ocrService
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void worker_gets_created_successfully()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _mqPublisherMock.Object,
                _storageService,
                _ocrService
            );
            // shouldn't crash
            Assert.NotNull(worker);
        }

        [Fact]
        public void is_a_background_service()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _mqPublisherMock.Object,
                _storageService,
                _ocrService
            );

            Assert.NotNull(worker);
            Assert.IsAssignableFrom<BackgroundService>(worker);
        }
    }
}
