using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Paperless.API.DTOs;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

namespace Paperless.API.Controllers
{
    //  Ignore BL for now and just query directly to DAL (Sprint 1)
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController
        (IDocumentRepository documentRepository, IMapper mapper, ILogger<DocumentController> logger) 
        : ControllerBase 
    {
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<DocumentController> _logger = logger;

        [HttpGet(Name = "GetDocument")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<DocumentDTO>>> GetAll()
        {
            IEnumerable<DocumentDTO> documents = await _mapper.Map<IEnumerable<DocumentDTO>>(_documentRepository.GetAllDocuments());
            if(documents == null)
            {
                _logger.LogWarning("No documents found.");
                return NotFound();
            }
            
            return Ok(documents);
        }

        [HttpGet("{id}", Name = "GetDocumentById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Get(string id)
        {
            if (!Guid.TryParse(id, out Guid guid))
                return BadRequest("Invalid ID");

            try
            {
                DocumentDTO document = _mapper.Map<DocumentDTO>(_documentRepository.GetDocumentById(guid));
                if (document == null)
                    return NotFound($"Document ID {id} not found");
                return Ok(document);
            }
            catch (Exception ex)
            {
                return NotFound($"Document ID {id} not found:\n" + ex.Message);
            }
        }

        [HttpPost(Name = "PostDocument")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<DocumentDTO> UploadDocument(IFormFile form) {
            if (form == null || form.Length == 0) 
                return BadRequest("Empty or invalid document.");

            DocumentDTO document = parseFormData(form);
            if (document == null)
                return BadRequest("Empty or invalid document.");

            _documentRepository.InsertDocument(_mapper.Map<DocumentEntity>(document));

            return CreatedAtAction(nameof(Get), new { id = document.Id }, document); 
        }
        
        [HttpPut(Name = "PutDocument")]
        public void Put()
        {
            //  TODO: implement later, edit document
        }

        [HttpDelete(Name = "DeleteDocument")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult DeleteAll()
        {
            _documentRepository.DeleteAllDocuments();
            return Ok();
        }

        [HttpDelete("{id}", Name = "DeleteDocumentById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Delete(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out Guid guid))
                    return BadRequest("Invalid ID");

                _documentRepository.DeleteDocumentAsync(guid);
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
