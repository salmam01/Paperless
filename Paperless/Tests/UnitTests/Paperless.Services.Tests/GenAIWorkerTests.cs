using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Services.Clients;
using Paperless.Services.Services.Messaging.Base;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Services.Messaging.Publishers;
using Paperless.Services.Workers;

namespace Paperless.Services.Tests
{
    public class GenAIWorkerTests
    {
        private readonly Mock<ILogger<GenAIWorker>> _loggerMock;
        private readonly Mock<GenAIListener> _mqListenerMock;
        private readonly Mock<MQPublisher> _mqPublisherMock;
        private readonly Mock<GenAIService> _genAIServiceMock;
        private readonly Mock<ResultClient> _workerResultsServiceMock;

        public GenAIWorkerTests()
        {
            _loggerMock = new Mock<ILogger<GenAIWorker>>();
            
            // Setup ListenerConfig mock
            Mock<IOptionsMonitor<ListenerConfig>> listenerConfigMock = new Mock<IOptionsMonitor<ListenerConfig>>();
            listenerConfigMock.Setup(x => x.Get("SummaryListener")).Returns(new ListenerConfig
            {
                QueueName = "summary.queue",
                MaxRetries = 3,
                RoutingKeys = new List<string> { "summary.completed" }
            });
            
            // RabbitMQConfig 
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
            
            _mqListenerMock = new Mock<GenAIListener>(
                Mock.Of<ILogger<GenAIListener>>(), 
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
            
            // GenAIConfig 
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
            
            // Setup RESTConfig 
            Mock<IOptions<RESTConfig>> restConfigMock = new Mock<IOptions<RESTConfig>>();
            restConfigMock.Setup(x => x.Value).Returns(new RESTConfig
            {
                Url = "https://localhost:5001/api/documents/"
            });
            
            _workerResultsServiceMock = new Mock<ResultClient>(
                Mock.Of<ILogger<ResultClient>>(),
                new HttpClient(),
                restConfigMock.Object
            );
        }

        [Fact]
        public void can_create_worker()
        {
            GenAIWorker worker = new GenAIWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _mqPublisherMock.Object,
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
                _mqPublisherMock.Object,
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
                _mqPublisherMock.Object,
                _genAIServiceMock.Object,
                _workerResultsServiceMock.Object
            );

            Assert.NotNull(worker);
            Assert.IsAssignableFrom<BackgroundService>(worker);
        }
    }
}

