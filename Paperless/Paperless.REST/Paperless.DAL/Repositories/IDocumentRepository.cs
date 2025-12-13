using Paperless.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.DAL.Repositories
{
    public interface IDocumentRepository
    {
        Task<IEnumerable<DocumentEntity>> GetDocumentsAsync();
        Task<DocumentEntity?> GetDocumentAsync(Guid Id);
        Task InsertDocumentAsync(DocumentEntity document);
        Task UpdateDocumentContentAndSummaryAsync(Guid id, string content, string summary);
        Task DeleteDocumentsAsync();
        Task DeleteDocumentAsync(Guid Id);
    }
}
