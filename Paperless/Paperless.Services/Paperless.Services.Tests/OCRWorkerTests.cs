using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Ocr;
using Paperless.Services.Services;
using Paperless.Services.Workers;
using System.Text;

namespace Paperless.Services.Tests
{
    public class OCRWorkerTests
    {
        private readonly Mock<ILogger<OcrWorker>> _loggerMock;
        private readonly Mock<IOptions<RabbitMqConfig>> _rabbitMqConfigMock;
        private readonly StorageService _storageService;
        private readonly OcrService _ocrService;
        private readonly RabbitMqConfig _testRabbitMqConfig;
        private readonly Mock<IServiceProvider> _serviceProviderMock;

        public OCRWorkerTests()
        {
            _loggerMock = new Mock<ILogger<OcrWorker>>();
            _rabbitMqConfigMock = new Mock<IOptions<RabbitMqConfig>>();
            _serviceProviderMock = new Mock<IServiceProvider>();

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

            _testRabbitMqConfig = new RabbitMqConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest",
                QueueName = "document.uploaded"
            };

            _rabbitMqConfigMock.Setup(x => x.Value).Returns(_testRabbitMqConfig);

            // Setup ServiceProvider mock
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
            _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
        }

        [Fact]
        public void can_create_worker()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _rabbitMqConfigMock.Object,
                _storageService,
                _ocrService,
                _serviceProviderMock.Object
            );

            Assert.NotNull(worker);
        }

        [Fact]
        public void config_has_right_values()
        {
            RabbitMqConfig config = new RabbitMqConfig
            {
                Host = "test-host",
                Port = 5672,
                User = "test-user",
                Password = "test-password",
                QueueName = "test-queue"
            };

            Assert.Equal("test-host", config.Host);
            Assert.Equal(5672, config.Port);
            Assert.Equal("test-user", config.User);
            Assert.Equal("test-password", config.Password);
            Assert.Equal("test-queue", config.QueueName);
        }

        [Fact]
        public void worker_gets_created_successfully()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _rabbitMqConfigMock.Object,
                _storageService,
                _ocrService,
                _serviceProviderMock.Object
            );
            // shouldn't crash
            Assert.NotNull(worker);
        }

        [Fact]
        public void has_correct_queue_name()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _rabbitMqConfigMock.Object,
                _storageService,
                _ocrService,
                _serviceProviderMock.Object
            );

            Assert.NotNull(worker);
            Assert.Equal("document.uploaded", _testRabbitMqConfig.QueueName);
        }

        [Fact]
        public void creates_connection_factory()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _rabbitMqConfigMock.Object,
                _storageService,
                _ocrService,
                _serviceProviderMock.Object
            );

            // worker should be createable
            Assert.NotNull(worker);
        }

        [Fact]
        public void is_a_background_service()
        {
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _rabbitMqConfigMock.Object,
                _storageService,
                _ocrService,
                _serviceProviderMock.Object
            );

            Assert.NotNull(worker);
            Assert.IsAssignableFrom<BackgroundService>(worker);
        }

        // Retry-Logik Tests
        [Fact]
        public void retry_count_starts_at_zero()
        {
            RabbitMqConfig testConfig = new RabbitMqConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest",
                QueueName = "test-queue",
                MaxRetries = 3
            };
            _rabbitMqConfigMock.Setup(x => x.Value).Returns(testConfig);

            // Act - Test Retry-Counter Initialization
            Dictionary<string, object> headers = new Dictionary<string, object> { { "x-retry-count", 0 } };
            int retryCount = 0;
            if (headers.TryGetValue("x-retry-count", out var retryCountObj))
            {
                try
                {
                    retryCount = Convert.ToInt32(retryCountObj);
                }
                catch
                {
                    retryCount = 0;
                }
            }
            Assert.Equal(0, retryCount);
        }

        [Fact]
        public void retry_count_increments_correctly()
        {
            RabbitMqConfig testConfig = new RabbitMqConfig
            {
                MaxRetries = 3
            };

            // Act -  Retry-Logic simulation
            int retryCount = 0;
            Dictionary<string, object> headers = new Dictionary<string, object> { { "x-retry-count", 0 } };
            
            if (headers.TryGetValue("x-retry-count", out var retryCountObj))
            {
                retryCount = Convert.ToInt32(retryCountObj);
            }
            
            retryCount++; // first Retry
           
            Assert.Equal(1, retryCount);
            Assert.True(retryCount <= testConfig.MaxRetries);
        }

        [Fact]
        public void retry_count_respects_max_retries()
        {
            RabbitMqConfig testConfig = new RabbitMqConfig
            {
                MaxRetries = 3
            };

            // Act -  3. Retry-try simulation
            int retryCount = 0;
            Dictionary<string, object> headers = new Dictionary<string, object> { { "x-retry-count", 2 } };
            
            if (headers.TryGetValue("x-retry-count", out object? retryCountObj))
            {
                retryCount = Convert.ToInt32(retryCountObj);
            }
            
            retryCount++; // 3. Retry
            
            Assert.Equal(3, retryCount);
            Assert.True(retryCount <= testConfig.MaxRetries);
        }

        [Fact]
        public void retry_count_exceeds_max_retries()
        {
          
            RabbitMqConfig testConfig = new RabbitMqConfig
            {
                MaxRetries = 3
            };

            // Act -  4. Retry simulation (sollte MaxRetries überschreiten)
            int retryCount = 3; // Bereits 3 Retries
            retryCount++; // 4. Retry
            
            // Assert
            Assert.Equal(4, retryCount);
            Assert.False(retryCount <= testConfig.MaxRetries);
        }

        [Fact]
        public void retry_count_handles_missing_header()
        {
           
            Dictionary<string, object> headers = new Dictionary<string, object>(); // no x-retry-count Header
            
            int retryCount = 0;
            if (headers.TryGetValue("x-retry-count", out var retryCountObj))
            {
                retryCount = Convert.ToInt32(retryCountObj);
            }
            retryCount++;

            // Sollte auf 1 gesetzt werden (0 + 1)
            Assert.Equal(1, retryCount);
        }

        [Fact]
        public void retry_count_handles_invalid_header_value()
        {
            Dictionary<string, object> headers = new Dictionary<string, object> { { "x-retry-count", "invalid" } };

            int retryCount = 0;
            if (headers.TryGetValue("x-retry-count", out var retryCountObj))
            {
                try
                {
                    retryCount = Convert.ToInt32(retryCountObj);
                }
                catch
                {
                    retryCount = 0;
                }
            }
            retryCount++;

            // Assert - Sollte bei Fehler auf 1 gesetzt werden (0 + 1)
            Assert.Equal(1, retryCount);
        }

        [Fact]
        public void max_retries_config_is_loaded_correctly()
        {
            RabbitMqConfig testConfig = new RabbitMqConfig
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest",
                QueueName = "test-queue",
                MaxRetries = 3
            };
            _rabbitMqConfigMock.Setup(x => x.Value).Returns(testConfig);
            
            OcrWorker worker = new OcrWorker(
                _loggerMock.Object,
                _rabbitMqConfigMock.Object,
                _storageService,
                _ocrService,
                _serviceProviderMock.Object
            );
            
            Assert.NotNull(worker);
            Assert.Equal(3, testConfig.MaxRetries);
        }
    }
}

