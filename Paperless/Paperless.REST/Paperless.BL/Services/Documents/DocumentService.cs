using AutoMapper;
using Microsoft.Extensions.Logging;
using Paperless.BL.Exceptions;
using Paperless.BL.Helpers;
using Paperless.BL.Models.Domain;
using Paperless.BL.Models.DTOs;
using Paperless.BL.Services.Categories;
using Paperless.BL.Services.Messaging;
using Paperless.BL.Services.Search;
using Paperless.BL.Services.Storage;
using Paperless.DAL.Entities;
using Paperless.DAL.Exceptions;
using Paperless.DAL.Repositories.Documents;


namespace Paperless.BL.Services.Documents
{
    public class DocumentService (
        IDocumentRepository documentrepository,
        ICategoryService categoryService,
        IDocumentSearchService searchService,
        IDocumentPublisher documentPublisher,
        IStorageService storageService,
        Parser parser,
        IMapper mapper, 
        ILogger<DocumentService> logger
    ) : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository = documentrepository;
        private readonly ICategoryService _categoryService = categoryService;
        private readonly IDocumentSearchService _searchService = searchService;
        private readonly IDocumentPublisher _documentPublisher = documentPublisher;
        private readonly IStorageService _storageService = storageService;
        private readonly Parser _parser = parser;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<DocumentService> _logger = logger;

        public async Task<IEnumerable<Document>> GetDocumentsAsync()
        {
            _logger.LogInformation("Retrieving all documents from database.");

            try
            {
                IEnumerable<DocumentEntity> entities = await _documentRepository.GetDocumentsAsync();
                IEnumerable<Document> documents = _mapper.Map<IEnumerable<Document>>(entities);

                return documents;
            } catch (DatabaseException ex) {
                _logger.LogError(
                    ex, 
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "GET", "DataAccess", "a database error"
                );
                throw new ServiceException("Could not retrieve documents.", ExceptionType.Internal, ex);
            }
        }

        public async Task<Document> GetDocumentAsync(Guid id)
        {
            _logger.LogInformation("Retrieving document with ID {DocumentId} from database.", id);

            try
            {
                DocumentEntity? entity = await _documentRepository.GetDocumentAsync(id);
                Document document = _mapper.Map<Document>(entity);

                return document;
            }
            catch (DatabaseException ex) 
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "GET", id, "DataAccess", "a database error"
                );

                throw new ServiceException("Could not retrieve document.", ExceptionType.Internal, ex);
            }
        }

        public async Task<List<Document>> SearchForDocumentAsync(string query)
        {
            _logger.LogInformation("Retrieving document by query {query} from database.", query);

            try
            {
                List<SearchResult> searchResults = await _searchService.SearchAsync(query);
                List<Document> documents = [];

                foreach (SearchResult result in searchResults)
                {
                    DocumentEntity? entity = await _documentRepository.GetDocumentAsync(result.Id);
                    documents.Add(_mapper.Map<Document>(entity));
                }

                return documents;
            }
            catch (ElasticSearchException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/search/{query} failed in {layer} Layer due to {reason}.",
                    "GET", query, "Business", "a search error"
                );

                throw new ServiceException("Could not search for query.", ExceptionType.Search, ex);
            }
        }

        public async Task UploadDocumentAsync(Document document, Stream content)
        {
            _logger.LogInformation(
                "Uploading document. ID: {DocumentId}, Name: {DocumentName}, Type: {DocumentType}, Size: {DocumentSize} bytes.",
                document.Id,
                document.Name,
                document.Type,
                content?.Length ?? 0
            );

            try
            {
                AdjustFileType(document);
                List<Category> categories = await _categoryService.GetCategoriesAsync();

                if (document.Type == "PDF")
                {
                    await _storageService.StoreDocumentAsync(document, content);
                    await _documentPublisher.PublishDocumentAsync(document.Id, categories);
                }
                else
                {
                    _parser.ParseDocument(document, content);
                    await _storageService.StoreDocumentAsync(document, content);
                    // Auch für DOCX und andere Dateitypen Message senden, damit OCR und Summary generiert werden
                    await _documentPublisher.PublishDocumentAsync(document.Id, categories);
                }

                DocumentEntity entity = _mapper.Map<DocumentEntity>(document);
                await _documentRepository.AddDocumentAsync(entity);
            }
            catch (MinIOException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "a minIO error"
                );
                throw new ServiceException("Could not upload document.", ex.Type);
            }
            catch (RabbitMQException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "a RabbitMQ error"
                );
                throw new ServiceException("Could not upload document.", ex.Type);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "DataAccess", "a database error"
                );

                throw new ServiceException("Could not upload document.", ExceptionType.Internal);
            }
        }

        public async Task UpdateDocumentAsync(string documentId, string categoryId, string content, string summary)
        {
            _logger.LogInformation(
                "Updating document. " +
                "ID: {DocumentId}, Category: {Category}, Content length: {ContentLength} characters, Summary length: {SummaryLength} characters.",
                documentId,
                categoryId,
                content?.Length ?? 0,
                summary?.Length ?? 0
            );

            try
            {
                if (!Guid.TryParse(documentId, out Guid documentGuid))
                {
                    _logger.LogError(
                        "Invalid document ID format received from queue: {DocumentIdString}",
                        documentId
                    );
                    throw new ServiceException($"Invalid document ID format: {documentId}", ExceptionType.Validation);
                }

                if (!Guid.TryParse(categoryId, out Guid categoryGuid))
                {
                    _logger.LogError(
                        "Invalid document ID format received from queue: {DocumentIdString}",
                        categoryId
                    );
                    throw new ServiceException($"Invalid document ID format: {categoryId}", ExceptionType.Validation);
                }

                await _documentRepository.UpdateDocumentContentAsync(documentGuid, categoryGuid, content, summary);
                _logger.LogInformation(
                    "Document {DocumentId} summary updated in database.",
                    documentId
                );
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update document {DocumentId} summary in database.",
                    documentId
                );
                throw new ServiceException("Could not update document summary.", ExceptionType.Internal, ex);
            }
        }

        public async Task UpdateDocumentCategoryAsync(Guid documentId, Guid categoryId)
        {
            _logger.LogInformation(
                "Updating document category." +
                "Document ID: {DocumentId}, Category ID: {Category}",
                documentId,
                categoryId
            );

            try
            {
                var category = await _categoryService.GetCategoryAsync(categoryId);
                if (category == null)
                    throw new ServiceException("Could not update document summary.", ExceptionType.Internal);

                await _documentRepository.UpdateDocumentCategoryAsync(documentId, categoryId);
                _logger.LogInformation(
                    "Document {DocumentId} category updated in database.",
                    documentId
                );
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update document {DocumentId} summary in database.",
                    documentId
                );
                throw new ServiceException("Could not update document category.", ExceptionType.Internal, ex);
            }
        }

        public async Task DeleteDocumentsAsync()
        {
            _logger.LogInformation("Deleting all documents from database and storage.");

            try
            {
                await _storageService.DeleteDocumentsAsync();
                await _documentPublisher.DeleteDocumentsAsync();
                await _documentRepository.DeleteDocumentsAsync();
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "DELETE", "DataAccess", "a database error"
                );

                throw new ServiceException("Could not delete documents.", ExceptionType.Internal, ex);
            }
        }

        public async Task DeleteDocumentAsync(Guid id)
        {
            _logger.LogInformation("Deleting document with ID {DocumentId} from database and storage.", id);

            try
            {
                DocumentEntity? entity = await _documentRepository.GetDocumentAsync(id);
                Document document = _mapper.Map<Document>(entity);

                await _storageService.DeleteDocumentAsync(id, document.Type);
                await _documentPublisher.DeleteDocumentAsync(id);
                await _documentRepository.DeleteDocumentAsync(id);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "DELETE", id, "DataAccess", "a database error"
                );

                throw new ServiceException("Could not delete document.", ExceptionType.Internal, ex);
            }
        }

        private bool CheckMetaDataValidity(Document document)
        {
            if (document.Id == Guid.Empty || 
                string.IsNullOrWhiteSpace(document.Name) ||
                string.IsNullOrWhiteSpace(document.Content) ||
                string.IsNullOrWhiteSpace(document.FilePath) ||
                string.IsNullOrWhiteSpace(document.Type) ||
                document.Size <= 0)
                return false;

            return true;
        }

        private void AdjustFileType(Document document)
        {
            if (string.IsNullOrEmpty(document.Type)) {

                document.Type = "Unknown";
                return;
            }

            string extension = document.Type;

            document.Type = extension switch
            {
                ".pdf" => "PDF",
                ".docx" => "DOCX",
                ".txt" => "TXT",
                _ => "Unknown"
            };
        }
    }
}
