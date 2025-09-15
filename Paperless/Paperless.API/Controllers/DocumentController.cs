using Microsoft.AspNetCore.Mvc;
using Paperless.API.DTOs;

namespace Paperless.API.Controllers
{
    //  Ignore BL for now and just query directly to DAL
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private DocumentDTO[] createDocuments()
        {
            DocumentDTO[] documents = new DocumentDTO[10];
            for (int i = 0; i < documents.Length; i++)
            {
                DocumentDTO document = new();
                document.Name = $"Name {i.ToString()};";
                document.Content = $"Text {i.ToString()}";
                document.Summary = $"Summary {i.ToString()}";
                documents[i] = document;
            }
            return documents;
        }

        [HttpGet(Name = "Document")]
        public IEnumerable<DocumentDTO> GetAll()
        {
            return createDocuments();
        }

        
        [HttpGet("{id}")]
        public DocumentDTO Get(int id)
        {
            DocumentDTO document = new();
            document.Name = "Document";
            document.Content = "Blablabla";
            document.Summary = "Bla";

            return document;
        }

        [HttpPost(Name = "PostDocument")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<DocumentDTO> Create()
        {
            return null;
        }

        [HttpPut(Name = "PutDocument")]
        public void Put()
        {
        }

        [HttpDelete(Name = "DeleteDocument")]
        public void DeleteAll()
        {
        }

        [HttpDelete("{id}")]
        public void Delete()
        {
        }
    }
}
