using Paperless.Services.Models.Search;

namespace Paperless.Services.Services.Search
{
    public interface IElasticRepository
    {
        //  Create index
        Task CreateIndexIfNotExistsAsync();

        //  Add or update document
        Task<bool> IndexAsync(SearchDocument document);

        //  TODO: Edit category

        //  Remove document
        Task<bool> RemoveAsync(string id);

        //  Remove all documents
        Task<long?> RemoveAllAsync();
    }
}
