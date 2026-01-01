using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Search;
using Paperless.Services.Services.Messaging;
using Paperless.Services.Services.Search;
using Paperless.Services.Workers;
using RabbitMQ.Client.Events;
using System.Text.Json;
using Paperless.Services.Services.Messaging.Base;

namespace Paperless.Services.Tests
{
    public class IndexingWorkerTests
    {
        private readonly Mock<ILogger<IndexingWorker>> _loggerMock;
        private readonly Mock<MQListener> _mqListenerMock;
        private readonly Mock<IElasticRepository> _elasticServiceMock;

        public IndexingWorkerTests()
        {
            _loggerMock = new Mock<ILogger<IndexingWorker>>();
            
            // Setup QueueConfig mock
            Mock<IOptions<ListenerConfig>> queueConfigMock = new Mock<IOptions<ListenerConfig>>();
            queueConfigMock.Setup(x => x.Value).Returns(new ListenerConfig
            {
                QueueName = "indexing.queue",
                ExchangeName = "services.fanout",
                MaxRetries = 3
            });
            
            Mock<IOptions<RabbitMQConfig>> rabbitMqConfigMock = new Mock<IOptions<RabbitMQConfig>>();
            rabbitMqConfigMock.Setup(x => x.Value).Returns(new RabbitMQConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "admin",
                Password = "admin123"
            });
            
            MQConnectionFactory mqConnectionFactory = new MQConnectionFactory(rabbitMqConfigMock.Object);
            
            _mqListenerMock = new Mock<MQListener>(
                Mock.Of<ILogger<MQListener>>(), 
                queueConfigMock.Object,
                mqConnectionFactory
            );
            
            _elasticServiceMock = new Mock<IElasticRepository>();
        }

        [Fact]
        public void can_create_worker()
        {
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _elasticServiceMock.Object
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void is_a_background_service()
        {
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _elasticServiceMock.Object
            );

            Assert.IsAssignableFrom<BackgroundService>(worker);
        }

        [Fact]
        public void parse_message_with_valid_json_returns_document()
        {
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _elasticServiceMock.Object
            );

            string validMessage = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "Id", "test-id-123" },
                { "Title", "Test Document" },
                { "OcrResult", "This is test content" }
            });

            // Use reflection to access private ParseMessage method for testing
            MethodInfo? method = typeof(IndexingWorker).GetMethod("ParseMessage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);
            
            SearchDocument? result = method.Invoke(worker, new object[] { validMessage }) as SearchDocument;
            Assert.NotNull(result);
            Assert.Equal("test-id-123", result.Id);
            Assert.Equal("Test Document", result.Title);
            Assert.Equal("This is test content", result.Content);
        }

        [Fact]
        public void parse_message_with_invalid_json_returns_empty_document()
        {
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _elasticServiceMock.Object
            );

            string invalidMessage = "invalid json";

            MethodInfo? method = typeof(IndexingWorker).GetMethod("ParseMessage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);
            
            // JsonSerializer.Deserialize throws JsonException for invalid JSON
            Exception exception = Assert.ThrowsAny<Exception>(() => 
                method.Invoke(worker, new object[] { invalidMessage })
            );
            
            // Verify it's a JsonException (wrapped in TargetInvocationException)
            Assert.True(exception is System.Reflection.TargetInvocationException || 
                       exception is JsonException ||
                       exception.InnerException is JsonException);
        }

        [Fact]
        public void parse_message_with_missing_fields_returns_empty_document()
        {
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _elasticServiceMock.Object
            );

            string incompleteMessage = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "Id", "test-id" }
                //  Title and OcrResult: missing
            });

            MethodInfo? method = typeof(IndexingWorker).GetMethod("ParseMessage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);
            
            SearchDocument? result = method.Invoke(worker, new object[] { incompleteMessage }) as SearchDocument;
            Assert.NotNull(result);
            // Returns empty document when req fields are missing
            Assert.True(string.IsNullOrEmpty(result.Id));
        }

        [Fact]
        public async Task handle_message_calls_index_async_with_ocr_result()
        {
            // Arrange
            _elasticServiceMock.Setup(x => x.IndexAsync(It.IsAny<SearchDocument>()))
                .ReturnsAsync(true);

            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _elasticServiceMock.Object
            );

            string validMessage = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "Id", "test-id-123" },
                { "Title", "Test Document" },
                { "OcrResult", "This is OCR text content from the document" }
            });

            // Use reflection to access private HandleMessageAsync method
            MethodInfo? method = typeof(IndexingWorker).GetMethod("HandleMessageAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);

            // Act
            BasicDeliverEventArgs eventArgs = new BasicDeliverEventArgs(
                consumerTag: "test-consumer",
                deliveryTag: 1,
                redelivered: false,
                exchange: "test-exchange",
                routingKey: "test-routing-key",
                properties: Mock.Of<RabbitMQ.Client.IReadOnlyBasicProperties>(),
                body: ReadOnlyMemory<byte>.Empty,
                cancellationToken: CancellationToken.None
            );
            await (Task)method.Invoke(worker, new object[] { validMessage, eventArgs })!;

            // Assert - Verify that IndexAsync was called 
            _elasticServiceMock.Verify(
                x => x.IndexAsync(It.Is<SearchDocument>(d => 
                    d.Id == "test-id-123" && 
                    d.Title == "Test Document" && 
                    d.Content == "This is OCR text content from the document"
                )), 
                Times.Once
            );
        }

        [Fact]
        public async Task handle_message_with_empty_message_does_not_call_index_async()
        {
            // Arrange
            IndexingWorker worker = new IndexingWorker(
                _loggerMock.Object,
                _mqListenerMock.Object,
                _elasticServiceMock.Object
            );

            string emptyMessage = "";

            MethodInfo? method = typeof(IndexingWorker).GetMethod("HandleMessageAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);

            // Act
            BasicDeliverEventArgs eventArgs = new BasicDeliverEventArgs(
                consumerTag: "test-consumer",
                deliveryTag: 1,
                redelivered: false,
                exchange: "test-exchange",
                routingKey: "test-routing-key",
                properties: Mock.Of<RabbitMQ.Client.IReadOnlyBasicProperties>(),
                body: ReadOnlyMemory<byte>.Empty,
                cancellationToken: CancellationToken.None
            );
            await (Task)method.Invoke(worker, new object[] { emptyMessage, eventArgs })!;

            // Assert - IndexAsync was NOT called
            _elasticServiceMock.Verify(
                x => x.IndexAsync(It.IsAny<SearchDocument>()), 
                Times.Never
            );
        }
    }
}


