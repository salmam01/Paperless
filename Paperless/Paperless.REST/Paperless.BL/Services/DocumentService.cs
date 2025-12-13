using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Extensions.Logging;
using Paperless.BL.Exceptions;
using Paperless.BL.Helpers;
using Paperless.BL.Models;
using Paperless.DAL.Entities;
using Paperless.DAL.Exceptions;
using Paperless.DAL.Repositories;


namespace Paperless.BL.Services
{
    public class DocumentService (
        IDocumentRepository documentrepository,
        IDocumentSearchService searchService,
        DocumentPublisher documentPublisher,
        StorageService storageService,
        Parser parser,
        IMapper mapper, 
        ILogger<DocumentService> logger
    ) : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository = documentrepository;
        private readonly IDocumentSearchService _searchService = searchService;
        private readonly DocumentPublisher _documentPublisher = documentPublisher;
        private readonly StorageService _storageService = storageService;
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

        public async Task<List<Document>> SearchForDocument(string query)
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

                if (document.Type == "PDF")
                {
                    await _storageService.UploadDocumentToStorageAsync(document.Id, document.Type, content);
                    await _documentPublisher.PublishDocumentAsync(document.Id);
                }
                else
                {
                    _parser.ParseDocument(document, content);
                    await _storageService.UploadDocumentToStorageAsync(document.Id, document.Type, content);
                }

                DocumentEntity entity = _mapper.Map<DocumentEntity>(document);
                await _documentRepository.InsertDocumentAsync(entity);
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

        public async Task UpdateDocumentAsync(string id, string content, string summary)
        {
            _logger.LogInformation(
                "Updating document. ID: {DocumentId}, Content length: {ContentLength} characters, Summary length: {SummaryLength} characters.",
                id,
                content?.Length ?? 0,
                summary?.Length ?? 0
            );

            try
            {
                if (!Guid.TryParse(id, out Guid documentId))
                {
                    _logger.LogError(
                        "Invalid document ID format received from queue: {DocumentIdString}",
                        id
                    );
                    throw new ServiceException($"Invalid document ID format: {id}", ExceptionType.Validation);
                }

                await _documentRepository.UpdateDocumentContentAndSummaryAsync(documentId, content, summary);
                _logger.LogInformation(
                    "Document {DocumentId} summary updated in database. Summary length: {SummaryLength}",
                    id,
                    summary.Length
                );
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update document {DocumentId} summary in database.",
                    id
                );
                throw new ServiceException("Could not update document summary.", ExceptionType.Internal, ex);
            }
        }

        //  TODO: Implement in later sprints
        public async Task EditDocumentAsync(Document document)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteDocumentsAsync()
        {
            _logger.LogInformation("Deleting all documents from database and storage.");

            try
            {
                await _storageService.DeleteDocumentsAsync();
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
