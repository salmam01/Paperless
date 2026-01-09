using Paperless.BL.Models.Domain;

namespace Paperless.BL.Services.Documents
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetDocumentsAsync();
        Task<Document> GetDocumentAsync(Guid id);
        Task<List<Document>> SearchForDocumentAsync(string query);
        Task UploadDocumentAsync(Document document, Stream content);
        Task UpdateDocumentAsync(string documentId, string categoryId, string content, string summary);
        Task UpdateDocumentCategoryAsync(Guid documentId, Guid categoryId);
        Task DeleteDocumentsAsync();
        Task DeleteDocumentAsync(Guid id);
    }
}
