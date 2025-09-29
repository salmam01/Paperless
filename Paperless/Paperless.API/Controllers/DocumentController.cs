using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Paperless.API.DTOs;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

namespace Paperless.API.Controllers
{
    //  Ignore BL for now and just query directly to DAL (Sprint 1)
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController(IDocumentRepository documentRepository, IMapper mapper) : ControllerBase {
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IMapper _mapper = mapper;

        [HttpGet(Name = "GetDocument")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<DocumentDTO>> GetAll()
        {
            IEnumerable<DocumentDTO> documents = _mapper.Map<IEnumerable<DocumentDTO>>(_documentRepository.GetAllDocuments());
            if(documents == null)
                return NotFound();

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
        public ActionResult<DocumentDTO> Create(DocumentDTO document) {
            if (document == null || !CheckDocumentValidity(document)) 
                return BadRequest("Empty or invalid document data.");

            document.Id = Guid.NewGuid();
            document.Summary = "summary";
            document.CreationDate = DateTime.UtcNow;
            document.Type = "pdf";
            document.Size = 25;

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

                _documentRepository.DeleteDocument(guid);
                return Ok();
            }
            catch (Exception ex)
            {
                return NotFound($"Document ID {id} not found:\n" + ex.Message);
            }
        }

        //  Temporary method
        private bool CheckDocumentValidity(DocumentDTO document)
        {
            if (String.IsNullOrWhiteSpace(document.Name))
                return false;
            if (String.IsNullOrWhiteSpace(document.Content))
                return false;

            return true;
        }
    }
}
