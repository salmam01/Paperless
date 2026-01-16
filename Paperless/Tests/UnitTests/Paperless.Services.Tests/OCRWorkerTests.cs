using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.Messaging.Base;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Services.Messaging.Publishers;
using Paperless.Services.Services.OCR;
using Paperless.Services.Workers;

namespace Paperless.Services.Tests
{
    public class OCRWorkerTests
    {
        private readonly Mock<ILogger<OCRWorker>> _loggerMock;
        private readonly Mock<OCRListener> _ocrListenerMock;
        private readonly Mock<MQPublisher> _mqPublisherMock;
        private readonly Mock<StorageService> _storageServiceMock;
        private readonly Mock<OCRService> _ocrServiceMock;

        public OCRWorkerTests()
        {
            _loggerMock = new Mock<ILogger<OCRWorker>>();
            
            // ListenerConfig 
            Mock<IOptionsMonitor<ListenerConfig>> listenerConfigMock = new Mock<IOptionsMonitor<ListenerConfig>>();
            listenerConfigMock.Setup(x => x.Get("OCRListener")).Returns(new ListenerConfig
            {
                QueueName = "ocr.queue",
                MaxRetries = 3,
                RoutingKeys = new List<string> { "ocr.completed" }
            });
            
            //RabbitMQConfig 
            Mock<IOptions<RabbitMQConfig>> rabbitMqConfigMock = new Mock<IOptions<RabbitMQConfig>>();
            rabbitMqConfigMock.Setup(x => x.Value).Returns(new RabbitMQConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest",
                ExchangeName = "services.fanout"
            });
            
            MQConnectionFactory mqConnectionFactory = new MQConnectionFactory(rabbitMqConfigMock.Object);
            
            _ocrListenerMock = new Mock<OCRListener>(
                Mock.Of<ILogger<OCRListener>>(), 
                listenerConfigMock.Object,
                mqConnectionFactory
            );
            
            // Setup MQPublisher 
            Mock<IOptions<MQPublisherConfig>> publisherConfigMock = new Mock<IOptions<MQPublisherConfig>>();
            publisherConfigMock.Setup(x => x.Value).Returns(new MQPublisherConfig
            {
                RoutingKeys = new List<string> { "ocr.completed", "summary.completed" }
            });
            
            _mqPublisherMock = new Mock<MQPublisher>(
                Mock.Of<ILogger<MQPublisher>>(),
                publisherConfigMock.Object,
                mqConnectionFactory
            );

            // MinIOConfig setup
            Mock<IOptions<MinIOConfig>> minIoConfigMock = new Mock<IOptions<MinIOConfig>>();
            minIoConfigMock.Setup(x => x.Value).Returns(new MinIOConfig
            {
                Endpoint = "localhost:9000",
                Username = "minioadmin",
                Password = "minioadmin",
                BucketName = "test-bucket"
            });

            _storageServiceMock = new Mock<StorageService>(
                minIoConfigMock.Object,
                Mock.Of<ILogger<StorageService>>()
            );

            // OCRConfig 
            Mock<IOptions<OCRConfig>> ocrConfigMock = new Mock<IOptions<OCRConfig>>();
            ocrConfigMock.Setup(x => x.Value).Returns(new OCRConfig());

            _ocrServiceMock = new Mock<OCRService>(
                ocrConfigMock.Object,
                Mock.Of<ILogger<OCRService>>()
            );
        }

        [Fact]
        public void can_create_worker()
        {
            OCRWorker worker = new OCRWorker(
                _loggerMock.Object,
                _ocrListenerMock.Object,
                _mqPublisherMock.Object,
                _storageServiceMock.Object,
                _ocrServiceMock.Object
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void worker_gets_created_successfully()
        {
            OCRWorker worker = new OCRWorker(
                _loggerMock.Object,
                _ocrListenerMock.Object,
                _mqPublisherMock.Object,
                _storageServiceMock.Object,
                _ocrServiceMock.Object
            );
            // shouldn't crash
            Assert.NotNull(worker);
        }

        [Fact]
        public void is_a_background_service()
        {
            OCRWorker worker = new OCRWorker(
                _loggerMock.Object,
                _ocrListenerMock.Object,
                _mqPublisherMock.Object,
                _storageServiceMock.Object,
                _ocrServiceMock.Object
            );

            Assert.NotNull(worker);
            Assert.IsAssignableFrom<BackgroundService>(worker);
        }
    }
}
