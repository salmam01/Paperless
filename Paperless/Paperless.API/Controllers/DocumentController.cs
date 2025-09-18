using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Paperless.API.DTOs;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

namespace Paperless.API.Controllers
{
    //  Ignore BL for now and just query directly to DAL (Sprint 1)
    //  TODO: implement automapper
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
                return BadRequest("Invalid Id");
        
            DocumentEntity? document = _documentRepository.GetDocumentById(guid);
            if (document == null)
                return NotFound($"Document {id} not found");
        
            return Ok(document);
        }

         [HttpPost(Name = "PostDocument")] 
         [ProducesResponseType(StatusCodes.Status201Created)]
         [ProducesResponseType(StatusCodes.Status400BadRequest)]
         public ActionResult<DocumentDTO> Create(DocumentDTO document) {
            if (document.Id != Guid.Empty) return BadRequest();
            if (document == null || string.IsNullOrWhiteSpace(document.Name)) {
                return BadRequest("Invalid document data.");
            }
            document.Id = Guid.NewGuid();
            _documentRepository.InsertDocument(_mapper.Map<DocumentEntity>(document));
            return CreatedAtAction(nameof(Get), new { id = document.Id }, document); 
         }
        
        [HttpPut(Name = "PutDocument")]
        public void Put()
        {
            //  TODO: implement later
        }

        [HttpDelete(Name = "DeleteDocument")]
        public void DeleteAll()
        {
            _documentRepository.DeleteAllDocuments();
        }

        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            Guid guid = new Guid(id);
            _documentRepository.DeleteDocument(guid);
        }
    }
}
