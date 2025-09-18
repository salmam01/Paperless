using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Data;
using Paperless.DAL.Entities;
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
        private PaperlessDbContext _context;

        public DocumentRepository(PaperlessDbContext context)
        {
            _context = context;
        }

        public DocumentEntity? GetDocumentById(Guid id)
        {
            return _context.Documents.Find(id);
        }

        public IEnumerable<DocumentEntity> GetAllDocuments()
        {
            return _context.Documents;
        }

        public void InsertDocument(DocumentEntity document)
        {
            if (document == null)
                return;

            _context.Add(document);
            Save();
        }

        public void UpdateDocument(DocumentEntity document)
        {
            throw new NotImplementedException();
        }

        public void DeleteDocument(Guid id)
        {
            DocumentEntity? document = _context.Documents.Find(id);
            if (document == null)
                return;

            _context.Documents.Remove(document);
            Save();
        }

        public void DeleteAllDocuments()
        {
            _context.Documents.ExecuteDelete();
            
        }

        //  TODO: Full-text search
        public IEnumerable<DocumentEntity> SearchForDocument(string query)
        {
            throw new NotImplementedException();
        }

        private void Save()
        {
            _context.SaveChanges();
        }
    }
}
