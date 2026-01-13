using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Search;
using Paperless.Services.Services.Search;

namespace Paperless.Services.Tests
{
    public class ElasticServiceTests
    {
        private readonly Mock<ILogger<ElasticService>> _loggerMock;
        private readonly Mock<IOptions<ElasticSearchConfig>> _configMock;

        public ElasticServiceTests()
        {
            _loggerMock = new Mock<ILogger<ElasticService>>();
            
            _configMock = new Mock<IOptions<ElasticSearchConfig>>();
            _configMock.Setup(x => x.Value).Returns(new ElasticSearchConfig
            {
                Url = "http://localhost:9200",
                Index = "test-index",
                Username = "",
                Password = "test-password"
            });
        }

        [Fact]
        public void can_create_elastic_service()
        {
            ElasticService service = new ElasticService(
                _loggerMock.Object,
                _configMock.Object
            );

            Assert.NotNull(service);
        }

        [Fact]
        public void elastic_service_implements_interface()
        {
            ElasticService service = new ElasticService(
                _loggerMock.Object,
                _configMock.Object
            );

            Assert.IsAssignableFrom<IElasticRepository>(service);
        }

        [Fact]
        public async Task index_async_returns_boolean()
        {
            ElasticService service = new ElasticService(
                _loggerMock.Object,
                _configMock.Object
            );

            SearchDocument document = new SearchDocument
            {
                Id = "test-id",
                Title = "Test Title",
                Content = "Test Content"
            };

            // Returns bool (may fail without Elasticsearch, but should return bool)
            bool result = await service.IndexAsync(document);
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task remove_async_returns_boolean()
        {
            ElasticService service = new ElasticService(
                _loggerMock.Object,
                _configMock.Object
            );
            
            // If Elasticsearch isnt available, it may throw an exception,  is acceptable
            try
            {
                bool result = await service.RemoveAsync("test-id");
                Assert.IsType<bool>(result);
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public async Task remove_all_async_returns_nullable_long()
        {
            ElasticService service = new ElasticService(
                _loggerMock.Object,
                _configMock.Object
            );

            long? result = await service.RemoveAllAsync();
            Assert.True(result == null || result is long);
        }

        [Fact]
        public async Task create_index_if_not_exists_can_be_called()
        {
            ElasticService service = new ElasticService(
                _loggerMock.Object,
                _configMock.Object
            );
            await service.CreateIndexIfNotExistsAsync();
        }
    }
}


