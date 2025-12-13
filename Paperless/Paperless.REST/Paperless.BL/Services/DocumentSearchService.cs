using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Paperless.BL.Configurations;
using Paperless.BL.Models.Dtos;

namespace Paperless.BL.Services
{
    public class DocumentSearchService : IDocumentSearchService
    {
        private readonly ILogger<DocumentSearchService> _logger;
        private readonly ElasticSearchConfig _config;
        private readonly ElasticsearchClient _client;

        public DocumentSearchService(
            ILogger<DocumentSearchService> logger,
            IOptions<ElasticSearchConfig> config
        ) {
            _logger = logger;
            _config = config.Value;

            ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new Uri(_config.Url))
                .DefaultIndex(_config.Index);

            _client = new(settings);
        }

        public async Task<List<SearchResult>> SearchAsync(string query)
        {
            _logger.LogInformation(
                "Searching for Documents with Query {query} in Index {index}.",
                query,
                _config.Index
            );

            SearchResponse<SearchResult> response = await _client.SearchAsync<SearchResult>(s => s
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(query)
                        .Fields(new[] { "title", "content" })
                        .Fuzziness("AUTO")
                    )
                )
            );

            IEnumerable<SearchResult> results = response.Hits
                .Select(h => new SearchResult
                {
                    Id = Guid.Parse(h.Id),
                    Score = h.Score ?? 0
                }
            );

            _logger.LogInformation(
                "Found {count} documents matching search query. Documents:",
                results.Count()
            );

            return results.OrderByDescending(r => r.Score).ToList();
        }
    }
}
