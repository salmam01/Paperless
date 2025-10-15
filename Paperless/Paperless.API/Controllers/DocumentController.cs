using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Paperless.API.Dtos;
using Paperless.API.Messaging;
using Paperless.BL.Exceptions;
using Paperless.BL.Models;
using Paperless.BL.Services;

namespace Paperless.API.Controllers
{
    //  Ignore BL for now and just query directly to DAL (Sprint 1)
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController (
        IDocumentService documentService,
        DocumentPublisher documentPublisher,
        IMapper mapper, 
        ILogger<DocumentController> logger
        ) : ControllerBase 
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly DocumentPublisher _documentPublisher = documentPublisher;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<DocumentController> _logger = logger;

        [HttpGet(Name = "GetDocument")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAll()
        {
            _logger.LogInformation(
                "Incoming GET /document from {ip}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            
            try
            {
                IEnumerable<Document> documents = await _documentService.GetDocumentsAsync();
                IEnumerable<DocumentDto> documentDto = _mapper.Map<IEnumerable<DocumentDto>>(documents);

                _logger.LogInformation("GET /document retrieved {count} documents successfully.", documentDto.Count());
                return Ok(documentDto);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "GET", "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "GET", "API", ex.Message
                );
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Get(string id)
        {
            _logger.LogInformation(
                "Incoming GET /document/{id} from {ip}.",
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            if (!Guid.TryParse(id, out Guid guid))
            {
                _logger.LogWarning("GET /document/{id} failed due to an invalid ID.", id);
                return BadRequest("Invalid ID.");
            }

            try
            {
                Document document = await _documentService.GetDocumentAsync(guid);
                DocumentDto documentDto = _mapper.Map<DocumentDto>(document);

                _logger.LogInformation("GET /document/{id} retrieved document successfully.", id);
                return Ok(documentDto);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "GET", id, "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "GET", id, "API", ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        [HttpPost(Name = "PostDocument")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DocumentDto>> UploadDocument(IFormFile form) {
            _logger.LogInformation(
                "Incoming POST /document from {ip}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            if (form == null || form.Length == 0)
            {
                _logger.LogWarning("POST /document failed due to empty or invalid file format.");
                return BadRequest("Empty or invalid file format.");
            }

            try
            {
                DocumentDto documentDto = parseFormData(form);
                if (documentDto == null)
                    return BadRequest("Empty or invalid document.");

                await _documentPublisher.PublishDocumentAsync(documentDto);

                Document document = _mapper.Map<Document>(documentDto);
                await _documentService.UploadDocumentAsync(document);

                _logger.LogInformation("POST /document uploaded document with ID {Id} successfully.", documentDto.Id);
                return CreatedAtAction(nameof(Get), new { id = documentDto.Id }, documentDto);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "POST", "API", ex.Message
                );
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
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteAll()
        {
            _logger.LogInformation(
                "Incoming DELETE /document from {ip}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            try
            {
                await _documentService.DeleteDocumentsAsync();
                _logger.LogInformation("DELETE /document deleted documents successfully.");
                return Ok();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "DELETE", "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document failed in {layer} Layer due to {reason}.",
                    "DELETE", "API", ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        [HttpDelete("{id}", Name = "DeleteDocumentById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(string id)
        {
            _logger.LogInformation(
                "Incoming DELETE /document/{id} from {ip}.",
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            try
            {
                if (!Guid.TryParse(id, out Guid guid))
                    return BadRequest("Invalid ID");

                await _documentService.DeleteDocumentAsync(guid);
                _logger.LogInformation("DELETE /document/{id} deleted document successfully.", id);
                return Ok();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "DELETE", id, "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "DELETE", id, "API", ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        private DocumentDto parseFormData(IFormFile form)
        {
            DocumentDto documentDto = new
            (
                Guid.NewGuid(),
                form.FileName,
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, ...", // temporary 
                "Summary coming soon.", // temporary

                "FilePath", // temporary

                DateTime.UtcNow,
                form.ContentType,
                Math.Round(form.Length / Math.Pow(1024.0, 2), 2)
            );

            return documentDto;
        }

        private int GetHttpStatusCode(ExceptionType type)
        {
            switch (type)
            {
                case ExceptionType.Validation: return StatusCodes.Status400BadRequest;
                case ExceptionType.Internal: return StatusCodes.Status500InternalServerError;
                default: return StatusCodes.Status500InternalServerError;
            }
        }

        private string GetExceptionMessage(ExceptionType type)
        {
            switch(type)
            {
                case ExceptionType.Validation:
                    return "Validation Failed";
                case ExceptionType.Internal:
                    return "Internal Server Error";
                default:
                    return "Unknown Error";
            }
        }
    }
}
