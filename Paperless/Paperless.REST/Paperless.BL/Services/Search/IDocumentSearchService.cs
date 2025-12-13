using Paperless.BL.Models.DTOs;

namespace Paperless.BL.Services.Search
{
    public interface IDocumentSearchService
    {
        //  Get document
        Task<List<SearchResult>> SearchAsync(string query);
    }
}
