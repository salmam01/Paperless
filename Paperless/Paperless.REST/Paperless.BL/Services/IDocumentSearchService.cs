using Paperless.BL.Models.Dtos;

namespace Paperless.BL.Services
{
    public interface IDocumentSearchService
    {
        //  Get document
        Task<List<SearchResult>> SearchAsync(string query);
    }
}
