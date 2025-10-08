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
    public class DocumentService
        (IDocumentRepository documentrepository, IMapper mapper, ILogger<DocumentRepository> logger) 
        : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository = documentrepository;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<DocumentRepository> _logger = logger;

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
                    "GET", "Business", "a database error"
                );
                throw new ServiceException("Could not retrieve documents.", ex);
            }
        }

        public async Task<Document> GetDocumentAsync(Guid id)
        {
            try
            {
                DocumentEntity? entities = await _documentRepository.GetDocumentAsync(id);
                Document document = _mapper.Map<Document>(entities);

                _logger.LogInformation("GET /document/{Id} retrieved document successfully.", id);
                return document;
            } 
            catch (DatabaseException ex) 
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "GET", id, "Business", "a database error"
                );

                throw new ServiceException("Could not retrieve document.", ex);
            }
        }
        public async Task UploadDocumentAsync(Document document)
        {
            try
            {
                DocumentEntity entities = _mapper.Map<DocumentEntity>(document);
                await _documentRepository.InsertDocumentAsync(entities);

                _logger.LogInformation("POST /document uploaded document with ID {Id} successfully.", document.Id);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", "a database error"
                );

                throw new ServiceException("Could not retrieve document.", ex);
            }
        }

        public async Task EditDocumentAsync(Document document)
        {
            throw new NotImplementedException();
        }

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
                    "DELETE", "Business", "a database error"
                );

                throw new ServiceException("Could not retrieve document.", ex);
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
                    "DELETE", id, "Business", "a database error"
                );

                throw new ServiceException("Could not retrieve document.", ex);
            }
        }
    }
}
