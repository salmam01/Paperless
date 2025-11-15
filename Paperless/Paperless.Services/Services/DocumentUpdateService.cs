/*
using Microsoft.Extensions.Logging;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

namespace Paperless.Services.Services
{
    public class DocumentUpdateService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentUpdateService> _logger;

        public DocumentUpdateService(
            IDocumentRepository documentRepository,
            ILogger<DocumentUpdateService> logger)
        {
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public async Task UpdateDocumentContentAsync(Guid documentId, string content)
        {
            try
            {
                DocumentEntity? document = await _documentRepository.GetDocumentAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found in database.", documentId);
                    throw new KeyNotFoundException($"Document {documentId} not found");
                }

                document.Content = content;
                await _documentRepository.UpdateDocumentAsync(document);

                _logger.LogInformation(
                    "Document {DocumentId} in database with content updated. Content length: {ContentLength}",
                    documentId,
                    content.Length
                );
            }
            catch (KeyNotFoundException)
            {
                throw; // Rethrow, don't wrap
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update document {DocumentId} in database with content.",
                    documentId
                );
                throw; // Re-throw for caller to handle
            }
        }

        public async Task UpdateDocumentSummaryAsync(Guid documentId, string summary)
        {
            try
            {
                var document = await _documentRepository.GetDocumentAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found in database.", documentId);
                    throw new KeyNotFoundException($"Document {documentId} not found");
                }

                document.Summary = summary;
                await _documentRepository.UpdateDocumentAsync(document);

                _logger.LogInformation(
                    "Document {DocumentId} in database with summary updated. Summary length: {SummaryLength}",
                    documentId,
                    summary.Length
                );
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update document {DocumentId} in database with summary.",
                    documentId
                );
                throw;
            }
        }
    }
}
*/
