using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.DAL.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        public void DeleteDocument(Guid Id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Entities.Document> GetAllDocuments()
        {
            throw new NotImplementedException();
        }

        public Entities.Document GetDocumentById(Guid Id)
        {
            throw new NotImplementedException();
        }

        public void InsertDocument(Entities.Document document)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Entities.Document> SearchForDocument(string query)
        {
            throw new NotImplementedException();
        }

        public void UpdateDocument(Entities.Document document)
        {
            throw new NotImplementedException();
        }
    }
}
