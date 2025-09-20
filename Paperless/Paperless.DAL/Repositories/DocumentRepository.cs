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
        private readonly PaperlessDbContext _context;

        public DocumentRepository(PaperlessDbContext context)
        {
            _context = context;
        }

        public DocumentEntity? GetDocumentById(Guid id) {
            DocumentEntity? document = _context.Documents
                .AsNoTracking()
                .FirstOrDefault(d => d.Id == id);

            return document ?? throw new KeyNotFoundException($"Document {id} not found");
        }

        public IEnumerable<DocumentEntity> GetAllDocuments()
        {
            return _context.Documents.AsNoTracking().ToList();;
        }
        
        public void InsertDocument(DocumentEntity document)
        {
            if (document == null)  
                throw new ArgumentNullException(nameof(document), "InsertDocument: Document shouldn't be empty!");

            _context.Add(document);
            Save();
        }

        public void UpdateDocument(DocumentEntity document)
        {
            DocumentEntity? existDocument = _context.Documents.Find(document.Id);
            if (existDocument == null) 
                throw new ArgumentNullException(nameof(existDocument), "UpdateDocument: Document doesnt exist!");

            existDocument.Name = document.Name ?? existDocument.Name;
            existDocument.Content = document.Content ?? existDocument.Content;
            existDocument.Summary = document.Summary ?? existDocument.Summary;
            existDocument.Type = document.Type ?? existDocument.Type;
            existDocument.Size = document.Size;
                
            Save();
        }

        public void DeleteAllDocuments()
        {
            _context.Documents.ExecuteDelete();
        }

        public void DeleteDocument(Guid id)
        {
            DocumentEntity? document = _context.Documents.Find(id);
            if (document == null) 
                throw new ArgumentNullException(nameof(document), "DeleteDocument: Document doesn't exist!");

            _context.Documents.Remove(document);
            Save();
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
