using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Paperless.API.DTOs;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

namespace Paperless.API.Controllers
{
    //  Ignore BL for now and just query directly to DAL (Sprint 1)
    //  TODO: change calls to async
    [ApiController]
    [Route("[controller]")]
    public class DocumentController(IDocumentRepository documentRepository, IMapper mapper) : ControllerBase {
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IMapper _mapper = mapper;

        [HttpGet(Name = "Document")]
        public ActionResult<IEnumerable<DocumentDTO>> GetAll()
        {
            IEnumerable<DocumentDTO> documents = _mapper.Map<IEnumerable<DocumentDTO>>(_documentRepository.GetAllDocuments());
            if(documents == null)
                return NotFound();

            return Ok(documents);
        }

        
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            if (!Guid.TryParse(id, out Guid guid))
                return BadRequest("Invalid ID");
        
            DocumentEntity? document = _documentRepository.GetDocumentById(guid);
            if (document == null)
                return NotFound($"Document ID {id} not found");
        
            return Ok(document);
        }

         [HttpPost(Name = "PostDocument")] 
         [ProducesResponseType(StatusCodes.Status201Created)]
         [ProducesResponseType(StatusCodes.Status400BadRequest)]
         public ActionResult<DocumentDTO> Create([Bind("Name,Content")] DocumentDTO document) {
            if (document == null) 
                return BadRequest("Empty document.");

            document.Id = Guid.NewGuid();
            document.Summary = "summary";
            document.CreationDate = DateTime.UtcNow;
            document.Type = "pdf";
            document.Size = 25;

            if (!CheckDocumentValidity(document))
                return BadRequest("Invalid document data.");

            _documentRepository.InsertDocument(_mapper.Map<DocumentEntity>(document));

            return CreatedAtAction(nameof(Get), new { id = document.Id }, document); 
         }
        
        [HttpPut(Name = "PutDocument")]
        public void Put()
        {
            //  TODO: implement later, edit document
        }

        [HttpDelete(Name = "DeleteDocument")]
        public ActionResult DeleteAll()
        {
            try
            {
                _documentRepository.DeleteAllDocuments();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
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
                return BadRequest(ex.Message);
            }
        }

        //  Temporary method
        private bool CheckDocumentValidity(DocumentDTO document)
        {
            if (String.IsNullOrWhiteSpace(document.Name))
                return false;
            if (String.IsNullOrWhiteSpace(document.Content))
                return false;
            if (String.IsNullOrWhiteSpace(document.Summary))
                return false;
            if (String.IsNullOrWhiteSpace(document.Type))
                return false;
            if (document.Size <= 0)
                return false;

            return true;
        }
    }
}
