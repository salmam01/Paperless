using Microsoft.EntityFrameworkCore;
using Npgsql;
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

        public async Task<IEnumerable<DocumentEntity>> GetAllDocuments()
        {
            return await _context.Documents.AsNoTracking().ToListAsync();
        }

        public async Task<DocumentEntity?> GetDocumentById(Guid id) {
            DocumentEntity? document = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            return document ?? throw new KeyNotFoundException($"Document {id} not found");
        }
        
        public async Task InsertDocument(DocumentEntity document)
        {
            if (document == null)  
                throw new ArgumentNullException(nameof(document), "InsertDocument: Document shouldn't be empty!");

            await _context.AddAsync(document);
            await SaveChangesAsync();
        }

        public async Task UpdateDocument(DocumentEntity document)
        {
            DocumentEntity? existDocument = await _context.Documents.FindAsync(document.Id);
            if (existDocument == null) 
                throw new ArgumentNullException(nameof(existDocument), "UpdateDocument: Document doesnt exist!");

            existDocument.Name = document.Name ?? existDocument.Name;
            existDocument.Content = document.Content ?? existDocument.Content;
            existDocument.Summary = document.Summary ?? existDocument.Summary;
            existDocument.Type = document.Type ?? existDocument.Type;
            existDocument.Size = document.Size;
                
            await SaveChangesAsync();
        }

        public async Task DeleteAllDocuments()
        {
            await _context.Documents.ExecuteDeleteAsync();
        }

        public async Task DeleteDocumentAsync(Guid id)
        {
            DocumentEntity? document = await _context.Documents.FindAsync(id);
            if (document == null) 
                throw new ArgumentNullException(nameof(document), "DeleteDocument: Document doesn't exist!");

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }

        //  TODO: Full-text search
        public IEnumerable<DocumentEntity> SearchForDocument(string query)
        {
            throw new NotImplementedException();
        }

        private async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                { /*do something*/ }

                else
                    throw;
            }

        }

        private bool IsDatabaseException(Exception ex)
        {
            return (ex is DbUpdateException ||
                    ex is PostgresException ||
                    ex is InvalidOperationException);
        }
    }
}
