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
        IEnumerable<DocumentEntity> GetAllDocuments();
        DocumentEntity? GetDocumentById(Guid Id);
        IEnumerable<DocumentEntity> SearchForDocument(string query);
        void InsertDocument(DocumentEntity document);
        void UpdateDocument(DocumentEntity document);
        void DeleteAllDocuments();
        void DeleteDocument(Guid Id);
    }
}
