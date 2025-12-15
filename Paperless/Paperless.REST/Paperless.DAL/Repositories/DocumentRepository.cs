using Microsoft.EntityFrameworkCore;
using Npgsql;
using Paperless.DAL.Database;
using Paperless.DAL.Entities;
using Paperless.DAL.Exceptions;

namespace Paperless.DAL.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly PaperlessDbContext _context;

        public DocumentRepository(PaperlessDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DocumentEntity>> GetDocumentsAsync()
        {
            try
            {
                return await _context.Documents.AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while retrieving all documents.", ex);
                else
                    throw;
            }
        }

        public async Task<DocumentEntity?> GetDocumentAsync(Guid id) {
            try
            {
                DocumentEntity? document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);

                return document ?? throw new KeyNotFoundException($"Document {id} not found");
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while retrieving Document by ID.", ex);
                else
                    throw;
            }
        }
        
        public async Task AddDocumentAsync(DocumentEntity document)
        {
            try
            {
                if (document == null)
                    throw new ArgumentNullException(nameof(document), "InsertDocument: Document shouldn't be empty!");

                await _context.AddAsync(document);
                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while adding Document to Database.", ex);
                else
                    throw;
            }
        }

        public async Task UpdateDocumentContentAsync(Guid id, string content, string summary)
        {
            try
            {
                DocumentEntity? existDocument = await _context.Documents.FindAsync(id);
                if (existDocument == null)
                    throw new ArgumentNullException(nameof(existDocument), "UpdateDocument: Document doesnt exist!");

                existDocument.Content = content;
                existDocument.Summary = summary;

                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while updating a Document.", ex);
                else
                    throw;
            }
        }

        public async Task DeleteDocumentsAsync()
        {
            try
            {
                await _context.Documents.ExecuteDeleteAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while deleting all documents.", ex);
                else
                    throw;
            }
        }

        public async Task DeleteDocumentAsync(Guid id)
        {
            try
            {
                DocumentEntity? document = await _context.Documents.FindAsync(id);
                if (document == null)
                    throw new ArgumentNullException(nameof(document), "DeleteDocument: Document doesn't exist!");

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (IsDatabaseException(ex))
                    throw new DatabaseException("An Error occurred while deleting a document.", ex);
                else
                    throw;
            }
        }

        private async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private bool IsDatabaseException(Exception ex)
        {
            return (ex is DbUpdateException ||
                    ex is PostgresException ||
                    ex is InvalidOperationException);
        }
    }
}
