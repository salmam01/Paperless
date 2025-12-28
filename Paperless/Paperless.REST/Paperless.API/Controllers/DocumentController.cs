using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Paperless.API.DTOs;
using Paperless.BL.Exceptions;
using Paperless.BL.Models.Domain;
using Paperless.BL.Services.Documents;

namespace Paperless.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController (
        IDocumentService documentService,
        IMapper mapper, 
        ILogger<DocumentController> logger
        ) : ControllerBase 
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<DocumentController> _logger = logger;

        [HttpGet(Name = "GetDocuments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DocumentDTO>>> GetAll()
        {
            _logger.LogInformation(
                "Incoming GET /document from {ip}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            
            try
            {
                IEnumerable<Document> documents = await _documentService.GetDocumentsAsync();
                IEnumerable<DocumentDTO> documentDTO = _mapper.Map<IEnumerable<DocumentDTO>>(documents);

                _logger.LogInformation("GET /document retrieved {count} documents successfully.", documentDTO.Count());
                return Ok(documentDTO);
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

        [HttpGet("{id}", Name = "GetDocument")]
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
                DocumentDTO documentDTO = _mapper.Map<DocumentDTO>(document);

                _logger.LogInformation("GET /document/{id} retrieved document successfully.", id);
                return Ok(documentDTO);
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

        [HttpGet("search/{query}", Name = "Search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetByQuery(string query)
        {
            _logger.LogInformation(
                "Incoming GET /document/search/{query} from {ip}.",
                query,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            try
            {
                List<Document> documents = await _documentService.SearchForDocument(query);
                List<DocumentDTO> documentDTO = _mapper.Map<List<DocumentDTO>>(documents);

                _logger.LogInformation(
                    "GET /document/search/{query} retrieved {count} document(s) successfully.",
                    documentDTO.Count(),
                    query
                );
                return Ok(documentDTO);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/search/{query} failed in {layer} Layer due to {reason}.",
                    "GET", query, "Business", GetExceptionMessage(ex.Type)
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
                    "{method} /document/search/{query} failed in {layer} Layer due to {reason}.",
                    "GET", query, "API", ex.Message
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
        public async Task<ActionResult<DocumentDTO>> Post(IFormFile form) {
            _logger.LogInformation(
                "Incoming POST /document from {ip}. File: {fileName}, ContentType: {contentType}, Size: {size}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                form?.FileName,
                form?.ContentType,
                form?.Length
            );

            if (form == null || form.Length == 0)
            {
                _logger.LogWarning("POST /document failed due to empty or invalid file format.");
                return BadRequest("Empty or invalid file format.");
            }

            try
            {
                DocumentDTO documentDTO = ParseFormMetaData(form);
                if (documentDTO == null)
                    return BadRequest("Empty or invalid document.");

                Document document = _mapper.Map<Document>(documentDTO);
                await _documentService.UploadDocumentAsync(document, form.OpenReadStream());

                _logger.LogInformation("POST /document uploaded document with ID {Id} successfully.", documentDTO.Id);
                return CreatedAtAction(nameof(Get), new { id = documentDTO.Id }, documentDTO);
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
                    "POST", 
                    "API", 
                    ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }


        [HttpPost("{id}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> PostServicesResult([FromRoute] string id, [FromBody] WorkerResultDTO result)
        {
            _logger.LogInformation(
                "Incoming POST /document/{id} from {ip}. Document ID: {DocumentId}, OCR result length: {OcrLength}, Summary length: {SummaryLength}",
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                result?.Id ?? "Unknown",
                result?.OcrResult?.Length ?? 0,
                result?.SummaryResult?.Length ?? 0
            );

            if (result == null || result.Id != id)
            {
                _logger.LogWarning("Invalid payload for document {DocumentId}. Expected ID: {ExpectedId}, Received ID: {ReceivedId}", id, id, result?.Id ?? "null");
                return BadRequest("Invalid payload");
            }
            try
            {
                await _documentService.UpdateDocumentAsync(
                    result.Id,
                    result.OcrResult,
                    result.SummaryResult
                );

                _logger.LogInformation("POST /document/{id} updated document successfully.", id);
                return Created();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /document/{id} failed in {layer} Layer due to {reason}.",
                    "POST", "Business", GetExceptionMessage(ex.Type),
                    id
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
                    "POST",
                    id,
                    "API",
                    ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
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

        private DocumentDTO ParseFormMetaData(IFormFile form)
        {
            _logger.LogInformation("Parsing file: {fileName}.", form.FileName);
            
            Guid id = Guid.NewGuid();
            string fileType = Path.GetExtension(form.FileName).ToLowerInvariant();

            DocumentDTO documentDTO = new
            (
                id,
                form.FileName,
                "loading...",
                "loading...",
                $"{id}.{form.FileName}",
                DateTime.UtcNow,
                fileType,
                Math.Round(form.Length / Math.Pow(1024.0, 2), 2)
            );

            return documentDTO;
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
