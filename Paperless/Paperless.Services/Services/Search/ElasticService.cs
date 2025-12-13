using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Search;

namespace Paperless.Services.Services.Search
{
    public class ElasticService : IElasticRepository
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticSearchConfig _config;
        private readonly ILogger _logger;
        
        public ElasticService(
            ILogger<ElasticService> logger,
            IOptions<ElasticSearchConfig> config
        ) {
            _logger = logger;
            _config = config.Value;

            ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new Uri(_config.Url))
                .DefaultIndex(_config.Index);

            _client = new(settings);
        }

        public async Task CreateIndexIfNotExistsAsync()
        {
            if (!_client.Indices.Exists(_config.Index).Exists)
            {
                await _client.Indices.CreateAsync(_config.Index);
                _logger.LogInformation(
                    "Creating new Index with name {indexname}.",
                    _config.Index
                );
            }
        }

        public async Task<bool> IndexAsync(SearchDocument document)
        {
            _logger.LogInformation(
                "Adding new Document with ID {id} and name {title} to Index {index}.",
                document.Id,
                document.Title,
                _config.Index
            );

            var response = await _client.IndexAsync(document, idx =>
                idx.Index(_config.Index)
                    .OpType(OpType.Index)
            );
            return response.IsValidResponse;
        }

        public async Task<bool> RemoveAsync(string id)
        {
            _logger.LogInformation(
                "Removing Document with ID {id} from Index {index}.",
                id,
                _config.Index
            );

            var response = await _client.DeleteAsync(id, d =>
                d.Index(_config.Index)
            );
            return response.IsValidResponse;
        }

        public async Task<long?> RemoveAllAsync()
        {
            _logger.LogInformation(
                "Removing all documents from Index {index}.",
                _config.Index
            );

            var response = await _client.DeleteByQueryAsync<SearchDocument> (
                d => d.Indices(_config.Index)
            );
            return response.IsValidResponse ? response.Deleted : default;
        }
    }
}
