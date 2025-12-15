using Paperless.BL.Models.Domain;

namespace Paperless.BL.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetDocumentsAsync();
        Task<Document> GetDocumentAsync(Guid id);
        Task<List<Document>> SearchForDocument(string query);
        Task UploadDocumentAsync(Document document, Stream content);
        Task UpdateDocumentAsync(string id, string content, string summary);
        Task DeleteDocumentsAsync();
        Task DeleteDocumentAsync(Guid id);
    }
}
