using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Paperless.API.DTOs;
using Paperless.BL.Models;
using Paperless.BL.Services;

namespace Paperless.API.Controllers
{
    //  Ignore BL for now and just query directly to DAL (Sprint 1)
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController
        (IDocumentService documentService, IMapper mapper, ILogger<DocumentController> logger) 
        : ControllerBase 
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<DocumentController> _logger = logger;

        [HttpGet(Name = "GetDocument")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DocumentDTO>>> GetAll()
        {
            _logger.LogInformation(
                "Incoming GET /document from {IP}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            
            try
            {
                var entities = await _documentService.GetDocumentsAsync();
                IEnumerable<DocumentDTO> documents = _mapper.Map<IEnumerable<DocumentDTO>>(entities);

                _logger.LogInformation("GET /document retrieved {Count} documents successfully.", documents.Count());
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /document failed due to an internal server error.");
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        [HttpGet("{id}", Name = "GetDocumentById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Get(string id)
        {
            _logger.LogInformation(
                "Incoming GET /document/{Id} from {IP}.",
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            if (!Guid.TryParse(id, out Guid guid))
            {
                _logger.LogWarning("GET /document/{Id} failed due to an invalid ID.", id);
                return BadRequest("Invalid ID.");
            }

            try
            {
                var entities = await _documentService.GetDocumentAsync(guid);
                DocumentDTO document = _mapper.Map<DocumentDTO>(entities);

                _logger.LogInformation("GET /document/{Id} retrieved document successfully.", id);
                return Ok(document);
            } //    TODO: tweak
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET /document/{Id} failed due to an internal server error.", id);
                return NotFound($"Document ID {id} not found:\n" + ex.Message);
            }
        }

        [HttpPost(Name = "PostDocument")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DocumentDTO>> UploadDocument(IFormFile form) {
            _logger.LogInformation(
                "Incoming POST /document from {IP}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            if (form == null || form.Length == 0)
            {
                _logger.LogWarning("POST /document failed due to empty or invalid file format.");
                return BadRequest("Empty or invalid file format.");
            }

            try
            {
                DocumentDTO document = parseFormData(form);
                if (document == null)
                    return BadRequest("Empty or invalid document.");

                var entities = _mapper.Map<Document>(document);
                await _documentService.UploadDocumentAsync(entities);

                _logger.LogInformation("POST /document uploaded document with ID {Id} successfully.", document.Id);
                return CreatedAtAction(nameof(Get), new { id = document.Id }, document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "POST /document failed due to an internal server error.");
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }
        
        [HttpPut(Name = "PutDocument")]
        public void Put()
        {
            //  TODO: implement later, edit document
        }

        [HttpDelete(Name = "DeleteDocument")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult> DeleteAll()
        {
            _logger.LogInformation(
                "Incoming DELETE /document from {IP}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            try
            {
                await _documentService.DeleteDocumentsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE /document failed due to an internal server error.");
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
            return Ok();
        }

        [HttpDelete("{id}", Name = "DeleteDocumentById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(string id)
        {
            _logger.LogInformation(
                "Incoming DELETE /document/{Id} from {IP}.",
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            try
            {
                if (!Guid.TryParse(id, out Guid guid))
                    return BadRequest("Invalid ID");

                await _documentService.DeleteDocumentAsync(guid);
                return Ok();
            }
            catch (Exception ex)
            {
                return NotFound($"Document ID {id} not found:\n" + ex.Message);
            }
        }

        //  Temporary method, should be in BL
        private DocumentDTO parseFormData(IFormFile form)
        {
            DocumentDTO document = new
            (
                Guid.NewGuid(),
                form.FileName,
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, ...", // temporary 
                "Summary coming soon.",

                "FilePath",    // also temporary

                DateTime.UtcNow,
                form.ContentType,
                Math.Round(form.Length / Math.Pow(1024.0, 2), 2)
            );

            return document;
        }

        //  Temporary method, should be in BL
        private bool checkDocumentValidity(DocumentDTO document)
        {
            if (String.IsNullOrWhiteSpace(document.Name))
                return false;
            if (String.IsNullOrWhiteSpace(document.Content))
                return false;

            return true;
        }
    }
}
