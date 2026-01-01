using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.Messaging;
using Paperless.Services.Services.Messaging.Base;
using Paperless.Services.Services.Messaging.Publishers;
using Paperless.Services.Services.OCR;
using Paperless.Services.Workers;

namespace Paperless.Services.Tests
{
    public class OCRWorkerTests
    {
        private readonly Mock<ILogger<OCRWorker>> _loggerMock;
        private readonly Mock<MQListener> _mqListenerMock;
        private readonly Mock<MQPublisher> _mqPublisherMock;
        private readonly StorageService _storageService;
        private readonly OCRService _ocrService;

        public OCRWorkerTests()
        {
            _loggerMock = new Mock<ILogger<OCRWorker>>();
            
            // Setup QueueConfig mock
            Mock<IOptions<QueueConfig>> queueConfigMock = new Mock<IOptions<QueueConfig>>();
            queueConfigMock.Setup(x => x.Value).Returns(new QueueConfig
            {
                QueueName = "ocr.queue",
                ExchangeName = "services.fanout",
                MaxRetries = 3
            });
            
            // Setup RabbitMQConfig mock
            Mock<IOptions<RabbitMQConfig>> rabbitMqConfigMock = new Mock<IOptions<RabbitMQConfig>>();
            rabbitMqConfigMock.Setup(x => x.Value).Returns(new RabbitMQConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest"
            });
            
            MQConnectionFactory mqConnectionFactory = new MQConnectionFactory(rabbitMqConfigMock.Object);
            
            _mqListenerMock = new Mock<MQListener>(
                Mock.Of<ILogger<MQListener>>(), 
                queueConfigMock.Object,
                mqConnectionFactory
            );
            
            _mqPublisherMock = new Mock<MQPublisher>(
                Mock.Of<ILogger<MQPublisher>>(), 
                queueConfigMock.Object,
                mqConnectionFactory
            );

            // real instances with mocked dependencies
            Mock<IOptions<MinIOConfig>> minIoConfigMock = new Mock<IOptions<Configurations.MinIOConfig>>();
            minIoConfigMock.Setup(x => x.Value).Returns(new Configurations.MinIOConfig
            {
                Endpoint = "localhost:9000",
                Username = "minioadmin",
                Password = "minioadmin",
                BucketName = "test-bucket"
            });

            Mock<IOptions<OCRConfig>> ocrConfigMock = new Mock<IOptions<OCRConfig>>();
            ocrConfigMock.Setup(x => x.Value).Returns(new OCRConfig());

            _storageService = new StorageService(
                minIoConfigMock.Object,
                Mock.Of<ILogger<StorageService>>()
            );

            _ocrService = new OCRService(
                ocrConfigMock.Object,
                Mock.Of<ILogger<OCRService>>()
            );
        }

        [Fact]
        public void can_create_worker()
        {
            OCRWorker worker = new OCRWorker(
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
            OCRWorker worker = new OCRWorker(
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
            OCRWorker worker = new OCRWorker(
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
