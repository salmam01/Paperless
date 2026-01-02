using Paperless.DAL.Entities;

namespace Paperless.DAL.Repositories.Documents
{
    public interface IDocumentRepository
    {
        Task<IEnumerable<DocumentEntity>> GetDocumentsAsync();
        Task<DocumentEntity?> GetDocumentAsync(Guid id);
        Task AddDocumentAsync(DocumentEntity document);
        Task UpdateDocumentContentAsync(Guid id, string content, string summary);
        Task DeleteDocumentsAsync();
        Task DeleteDocumentAsync(Guid id);
    }
}
