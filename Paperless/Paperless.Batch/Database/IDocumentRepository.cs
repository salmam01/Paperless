using Paperless.Batch.Models;

namespace Paperless.Batch.Database
{
    public interface IDocumentRepository
    {
        Task<bool> UpdateDocumentsAsync(AccessEntryList accessEntryList);
    }
}
