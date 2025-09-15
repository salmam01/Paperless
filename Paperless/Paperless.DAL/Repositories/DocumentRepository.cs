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

        public IEnumerable<Entities.DocumentEntity> GetAllDocuments()
        {
            throw new NotImplementedException();
        }

        public Entities.DocumentEntity GetDocumentById(Guid Id)
        {
            throw new NotImplementedException();
        }

        public void InsertDocument(Entities.DocumentEntity document)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Entities.DocumentEntity> SearchForDocument(string query)
        {
            throw new NotImplementedException();
        }

        public void UpdateDocument(Entities.DocumentEntity document)
        {
            throw new NotImplementedException();
        }
    }
}
