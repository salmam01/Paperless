using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Models.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Services.SearchService
{
    public class ElasticService : IElasticService
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

            ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new Uri(_config.Endpoint))
                .DefaultIndex(_config.Index);

            _client = new(settings);
        }

        public async Task CreateIndexIfNotExistsAsync()
        {
            if (!_client.Indices.Exists(_config.Index).Exists)
                await _client.Indices.CreateAsync(_config.Index);
        }

        public async Task<bool> AddOrUpdate(SearchDocument document)
        {
            var response = await _client.IndexAsync(document, idx =>
                idx.Index(_config.Index)
                    .OpType(OpType.Index)
            );

            return response.IsValidResponse;
        }

        public async Task<SearchDocument?> Get(string id)
        {
            var response = await _client.GetAsync<SearchDocument>(id, g =>
                g.Index(_config.Index)
            );

            return response.Source ?? new SearchDocument();
        }

        public async Task<List<SearchDocument>?> GetAll()
        {
            var response = await _client.SearchAsync<SearchDocument> (s =>
                s.Indices(_config.Index)
            );

            return response.IsValidResponse ? response.Documents.ToList() : [];
        }

        public async Task<bool> Remove(string id)
        {
            var response = await _client.DeleteAsync(id, d =>
                d.Index(_config.Index)
            );

            return response.IsValidResponse;
        }

        public async Task<long?> RemoveAll()
        {
            var response = await _client.DeleteByQueryAsync<SearchDocument> (
                d => d.Indices(_config.Index)
            );

            return response.IsValidResponse ? response.Deleted : default;
        }
    }
}
