using AutoMapper;
using Microsoft.Extensions.Logging;
using Paperless.BL.Models;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paperless.BL.Services
{
    //  TODO: Implement a custom BL exception
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
                var entities = await _documentRepository.GetDocumentsAsync();
                IEnumerable<Document> documents = _mapper.Map<IEnumerable<Document>>(entities);

                return documents;
            } catch (Exception ex) {
                _logger.LogError(ex, "GET /document failed due to an internal server error.");
                throw;
            }
        }

        public async Task<Document> GetDocumentAsync(Guid id)
        {
            try
            {
                var entities = await _documentRepository.GetDocumentAsync(id);
                Document document = _mapper.Map<Document>(entities);

                _logger.LogInformation("GET /document/{Id} retrieved document successfully.", id);
                return document;
            } 
            catch (Exception ex) 
            {
                _logger.LogError(ex, "GET /document/{Id} failed due to an internal server error.", id);
                throw;
            }
        }
        public async Task UploadDocumentAsync(Document document)
        {
            try
            {
                var entities = _mapper.Map<DocumentEntity>(document);
                await _documentRepository.InsertDocumentAsync(entities);

                _logger.LogInformation("POST /document uploaded document with ID {Id} successfully.", document.Id);
            }
            catch (Exception ex)
            {
                throw;
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
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task DeleteDocumentAsync(Guid id)
        {
            try
            {
                await _documentRepository.DeleteDocumentAsync(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
