using Paperless.Services.Models.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.Services.Services.SearchService
{
    public interface IElasticService
    {
        //  Create index
        Task CreateIndexIfNotExistsAsync();

        //  Add or update document
        Task<bool> AddOrUpdate(SearchDocument document);

        //  Get document
        Task<SearchDocument?> Get(string id);

        //  Get all documents
        Task<List<SearchDocument>?> GetAll();

        //  Remove document
        Task<bool> Remove(string id);

        //  Remove all documents
        Task<long?> RemoveAll();
    }
}
