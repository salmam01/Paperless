using Paperless.BL.Models.Domain;

namespace Paperless.BL.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetDocumentsAsync();
        Task<Document> GetDocumentAsync(Guid id);
        Task UploadDocumentAsync(Document document, Stream content);
        Task UpdateDocumentAsync(string documentId, string content, string summary);
        Task<List<Document>> SearchForDocument(string query);
        Task EditDocumentAsync(Document document);
        Task DeleteDocumentsAsync();
        Task DeleteDocumentAsync(Guid id);
    }
}
