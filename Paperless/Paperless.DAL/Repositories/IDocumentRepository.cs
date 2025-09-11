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
        Document GetDocumentById(Guid Id);
        IEnumerable<Document> GetAllDocuments();
        IEnumerable<Document> SearchForDocument(string query);
        void InsertDocument(Document document);
        void UpdateDocument(Document document);
        void DeleteDocument(Guid Id);
    }
}
