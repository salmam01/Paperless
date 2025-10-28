using AutoMapper;
using Microsoft.Extensions.Logging;
using Paperless.BL.Exceptions;
using Paperless.BL.Models;
using Paperless.DAL.Entities;
using Paperless.DAL.Exceptions;
using Paperless.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services
{
    public class DocumentService (
        IDocumentRepository documentrepository,
        DocumentPublisher documentPublisher,
        DocumentStorage documentStorage,
        IMapper mapper, 
        ILogger<DocumentService> logger
        ) : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository = documentrepository;
        private readonly DocumentPublisher _documentPublisher = documentPublisher;
        private readonly DocumentStorage _documentStorage = documentStorage;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<DocumentService> _logger = logger;

        public async Task<IEnumerable<Document>> GetDocumentsAsync()
        {
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

        public async Task UploadDocumentAsync(Document document, Stream content)
        {
            try
            {
                /*
                if (!CheckMetaDataValidity(document))
                {
                    _logger.LogWarning(
                        "{method} /document failed in {layer} Layer due to {reason}.",
                        "POST", "Business", "empty or invalid file format"
                    );
                    throw new ServiceException("Could not upload document.", ExceptionType.Validation);
                }*/

                DocumentEntity entity = _mapper.Map<DocumentEntity>(document);
                await _documentRepository.InsertDocumentAsync(entity);

                await _documentStorage.UploadDocumentToStorageAsync(document.Id, content);
                await _documentPublisher.PublishDocumentAsync(document.Id);
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

        //  TODO: Implement in later sprints
        public async Task EditDocumentAsync(Document document)
        {
            throw new NotImplementedException();
        }

        //  TODO: Implement in later sprints
        public async Task SearchForDocument(string query)
        {
            throw new NotImplementedException();
        }
        public async Task DeleteDocumentsAsync()
        {
            try
            {
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
            try
            {
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
    }
}
