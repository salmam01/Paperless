using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Search;

namespace Paperless.Services.Services.Search
{
    public class ElasticRepository : IElasticRepository
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticSearchConfig _config;
        private readonly ILogger _logger;
        
        public ElasticRepository(
            ILogger<ElasticRepository> logger,
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
                    "ElasticSearch Index {indexname} created.",
                    _config.Index
                );
            }
        }

        public async Task<bool> IndexAsync(SearchDocument document)
        {
            var response = await _client.IndexAsync(
                document, 
                idx => idx
                        .Index(_config.Index)
                        .Id(document.Id)
                        .OpType(OpType.Index)
                        .Refresh(Refresh.WaitFor)
            );

            if (!response.IsValidResponse)
            {
                _logger.LogInformation(
                    "Error while adding Document with ID {id} and name {Title} to Index {Index}. Error:\n{Error}",
                    document.Id,
                    document.Title,
                    _config.Index,
                    response.ElasticsearchServerError?.Error?.Reason
                );

                return false;
            }

            _logger.LogInformation(
                "New Document with ID {id} and name {Title} added to Index {Index}.",
                document.Id,
                document.Title,
                _config.Index
            );

            return true;
        }

        public async Task<bool> PutDocumentCategoryAsync(string documentId, string category)
        {
            var updateRequest = new UpdateRequest<SearchDocument, object>(
                documentId,
                _config.Index
            ) {
                Doc = new
                {
                    category = category
                },
                DocAsUpsert = true
            };

            var response = await _client.UpdateAsync(updateRequest);

            if (!response.IsValidResponse)
            {
                _logger.LogInformation(
                    "Failed to update Category '{Categoy}' for Document with ID {Id} from Index {Index}. Error:\n{Error}",
                    category,
                    documentId,
                    _config.Index,
                    response.ElasticsearchServerError?.Error?.Reason
                );
                return false;
            }

            _logger.LogInformation(
                "Updated Category '{Categoy}' for Document with ID {Id} from Index {Index}.",
                category,
                documentId,
                _config.Index
            );
            return true;
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var response = await _client.DeleteAsync<SearchDocument>(
                id,
                d => d.Index(_config.Index)
            );

            if (!response.IsValidResponse)
            {
                _logger.LogInformation(
                    "Failed to remove Document with ID {Id} from Index {Index}. Error:\n{Error}",
                    id,
                    _config.Index,
                    response.ElasticsearchServerError?.Error?.Reason
                );

                return false;
            }

            _logger.LogInformation(
                "Document with ID {Id} removed from Index {Index}.",
                id,
                _config.Index
            );
            return true;
        }

        public async Task<long?> RemoveAllAsync()
        {
            var response = await _client.DeleteByQueryAsync<SearchDocument> (d => d
                .Indices(_config.Index)
                .Query(q => q.MatchAll())
            );

            if (!response.IsValidResponse)
            {
                _logger.LogInformation(
                    "Failed to remove all documents from Index {Index}. Error:\n{Error}",
                    _config.Index,
                    response.ElasticsearchServerError?.Error?.Reason
                );
                return default;
            }

            _logger.LogInformation(
                "Removed all documents from Index {Index}.",
                _config.Index
            );
            return response.Deleted;
        }
    }
}
