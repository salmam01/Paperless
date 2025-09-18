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

        public DocumentEntity? GetDocumentById(Guid id) {
            DocumentEntity?
                document = _context.Documents.AsNoTracking()
                    .FirstOrDefault(d =>
                        d.Id == id); //vll kennst du das eh aber hier noxhmal: AsNoTracking verbessert die Performance und verhindert, dass EF den Entity-Status verfolgt; wenn wir im Datenbank nichts ändern wollen dann sollten wir immer notracking verwenden
            //statt exceptions verwendn wir FirstOrDefault, weil es sicherer ist und wir so vermeiden, dass die Anwendung abstürzt
            return document ?? throw new KeyNotFoundException($"Document {id} not found");
        }

        public IEnumerable<DocumentEntity> GetAllDocuments()
        {
            return _context.Documents.AsNoTracking().ToList();;
        }
        
        public void InsertDocument(DocumentEntity document)
        {
            if (document == null)  throw new ArgumentNullException(nameof(document), "InserDocument: Document shouldn't be empty!");

            _context.Add(document);
            Save();
        }

        public void UpdateDocument(DocumentEntity document) //Ich würde sagen wir ändern nur die Felder die veändert wurden; nicht alles überschreiben; was sagst du?
        {
            DocumentEntity? existDocument = _context.Documents.Find(document.Id);
            if (existDocument == null) throw new ArgumentNullException(nameof(existDocument), "UpdateDocument: Document doesnt exist!");
                //_context.Documents.Update(document); //Das überschreibt alles; auch Felder die nicht geändert wurden 
                existDocument.Name = document.Name ?? existDocument.Name;
                existDocument.Content = document.Content ?? existDocument.Content;
                existDocument.Summary = document.Summary ?? existDocument.Summary;
                existDocument.Type = document.Type ?? existDocument.Type;
                existDocument.Size = document.Size; //// Wenn sich die Größe d. Dokuments ändert, könnte es sinnvoll sein, das auch zu aktualisieren.
                
                Save();
        }
        
        public void DeleteDocument(Guid id)
        {
            DocumentEntity? document = _context.Documents.Find(id);
            if (document == null) throw new ArgumentNullException(nameof(document), "DeleteDocument: Document doesn't exist!");
            _context.Documents.Remove(document);
            Save();
        }

        public void DeleteAllDocuments()
        {
            _context.Documents.ExecuteDelete();
            
        }

        //  TODO: Full-text search
        //  For now:simple case-insensitive search in Name, Content, Summary:)
        
        public IEnumerable<DocumentEntity> SearchForDocument(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];//== return Enumerable.Empty<DocumentEntity>();
            string lowered = query.ToLowerInvariant();
            return _context.Documents.AsNoTracking()
                .Where(d => (d.Name ?? "").ToLower().Contains(lowered)
                            || (d.Content ?? "").ToLower().Contains(lowered)
                            || (d.Summary ?? "").ToLower().Contains(lowered))
                .ToList();

        }

        private void Save()
        {
            _context.SaveChanges();
        }
    }
}
