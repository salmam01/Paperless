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
        Task<IEnumerable<DocumentEntity>> GetAllDocuments();
        Task<DocumentEntity?> GetDocumentById(Guid Id);
        IEnumerable<DocumentEntity> SearchForDocument(string query);
        Task InsertDocument(DocumentEntity document);
        Task UpdateDocument(DocumentEntity document);
        Task DeleteAllDocuments();
        Task DeleteDocumentAsync(Guid Id);
    }
}
