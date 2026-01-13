using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Services.Messaging.Base;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Services.Search;
using Paperless.Services.Workers;

namespace Paperless.Services.Tests
{
    public class IndexingWorkerTests
    {
        private readonly Mock<ILogger<IndexingWorker>> _loggerMock;
        private readonly Mock<IndexingListener> _indexingListenerMock;
        private readonly Mock<IElasticRepository> _elasticServiceMock;

        public IndexingWorkerTests()
        {
            _loggerMock = new Mock<ILogger<IndexingWorker>>();
            
            // Setup ListenerConfig 
            Mock<IOptionsMonitor<ListenerConfig>> listenerConfigMock = new Mock<IOptionsMonitor<ListenerConfig>>();
            listenerConfigMock.Setup(x => x.Get("IndexingListener")).Returns(new ListenerConfig
            {
                QueueName = "indexing.queue",
                MaxRetries = 3,
                RoutingKeys = new List<string> { "summary.completed", "document.deleted", "documents.deleted" }
            });
            
            Mock<IOptions<RabbitMQConfig>> rabbitMqConfigMock = new Mock<IOptions<RabbitMQConfig>>();
            rabbitMqConfigMock.Setup(x => x.Value).Returns(new RabbitMQConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "admin",
                Password = "admin123",
                ExchangeName = "services.fanout"
            });
            
            MQConnectionFactory mqConnectionFactory = new MQConnectionFactory(rabbitMqConfigMock.Object);
            
            _indexingListenerMock = new Mock<IndexingListener>(
                Mock.Of<ILogger<IndexingListener>>(), 
                listenerConfigMock.Object,
                mqConnectionFactory
            );
            
            _elasticServiceMock = new Mock<IElasticRepository>();
        }

        [Fact]
        public void can_create_worker()
        {
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _indexingListenerMock.Object,
                _elasticServiceMock.Object
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void is_a_background_service()
        {
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _indexingListenerMock.Object,
                _elasticServiceMock.Object
            );

            Assert.IsAssignableFrom<BackgroundService>(worker);
        }
    }
}


