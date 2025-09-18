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
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IMapper _mapper;

        public DocumentController(IDocumentRepository documentRepository, IMapper mapper)
        {
            _documentRepository = documentRepository;
            _mapper = mapper;
        }

        [HttpGet(Name = "Document")]
        public ActionResult<IEnumerable<DocumentDTO>> GetAll()
        {
            IEnumerable<DocumentDTO> documents = _mapper.Map<IEnumerable<DocumentDTO>>(_documentRepository.GetAllDocuments());
            if(documents == null)
                return NotFound();

            return Ok(documents);
        }

        
        [HttpGet("{id}")]
        public ActionResult<DocumentDTO> Get(string id)
        {
            Guid guid = Guid.Parse(id);
            DocumentDTO document = _mapper.Map<DocumentDTO>(_documentRepository.GetDocumentById(guid));
            if(document == null)
                return NotFound();

            return Ok(document);
        }

        [HttpPost(Name = "PostDocument")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public void Create(DocumentDTO document)
        {
            _documentRepository.InsertDocument(_mapper.Map<DocumentEntity>(document));
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
