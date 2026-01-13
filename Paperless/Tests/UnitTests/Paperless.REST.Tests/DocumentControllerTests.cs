using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Paperless.API.Controllers;
using Paperless.API.DTOs;
using Paperless.BL.Exceptions;
using Paperless.BL.Models.Domain;
using Paperless.BL.Services.Documents;

namespace Paperless.REST.Tests
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentService> _documentService = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<DocumentController>> _logger = new();

        private DocumentController CreateController()
        {
            DocumentController controller = new DocumentController(_documentService.Object, _mapper.Object, _logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            Guid doc1Id = Guid.NewGuid();
            Guid doc2Id = Guid.NewGuid();
            IEnumerable<Document> docs = new List<Document>
            {
                new Document(doc1Id, "Doc1", null, "Content1", "Summary1", "file1.txt", DateTime.UtcNow, "txt", 1.0),
                new Document(doc2Id, "Doc2", null, "Content2", "Summary2", "file2.pdf", DateTime.UtcNow, "pdf", 2.0)
            };

            IEnumerable<DocumentDTO> docDTOs = new List<DocumentDTO>
            {
                new DocumentDTO(doc1Id, "Doc1", null, "Content1", "Summary1", "file1.txt", DateTime.UtcNow, "txt", 1.0),
                new DocumentDTO(doc2Id, "Doc2", null, "Content2", "Summary2", "file2.pdf", DateTime.UtcNow, "pdf", 2.0)
            };

            _documentService.Setup(s => s.GetDocumentsAsync()).ReturnsAsync(docs);
            _mapper.Setup(m => m.Map<IEnumerable<DocumentDTO>>(It.IsAny<IEnumerable<Document>>()))
                   .Returns(docDTOs);

            DocumentController controller = CreateController();
            ActionResult<IEnumerable<DocumentDTO>> result = await controller.GetAll();

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetAll_ServiceException_ReturnsProblem()
        {
            _documentService.Setup(s => s.GetDocumentsAsync())
                .ThrowsAsync(new ServiceException("Test error", ExceptionType.Internal));

            DocumentController controller = CreateController();
            ActionResult<IEnumerable<DocumentDTO>> result = await controller.GetAll();

            Assert.IsType<ObjectResult>(result.Result);
            ObjectResult objectResult = (ObjectResult)result.Result!;
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task Get_WithValidId_ReturnsOk()
        {
            Guid id = Guid.NewGuid();
            Document doc = new Document(id, "Doc", null, "Content", "Summary", "file.txt", DateTime.UtcNow, "txt", 1.0);
            DocumentDTO docDTO = new DocumentDTO(id, "Doc", null, "Content", "Summary", "file.txt", DateTime.UtcNow, "txt", 1.0);

            _documentService.Setup(s => s.GetDocumentAsync(id)).ReturnsAsync(doc);
            _mapper.Setup(m => m.Map<DocumentDTO>(It.IsAny<Document>()))
                   .Returns(docDTO);

            DocumentController controller = CreateController();
            ActionResult result = await controller.Get(id.ToString());

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Get_WithInvalidId_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.Get("not-a-guid");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Get_ServiceException_ReturnsProblem()
        {
            Guid id = Guid.NewGuid();
            _documentService.Setup(s => s.GetDocumentAsync(id))
                .ThrowsAsync(new ServiceException("Not found", ExceptionType.Validation));

            DocumentController controller = CreateController();
            ActionResult result = await controller.Get(id.ToString());

            Assert.IsType<ObjectResult>(result);
            ObjectResult objectResult = (ObjectResult)result;
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetByQuery_ReturnsOk()
        {
            string query = "test";
            List<Document> docs = new List<Document>
            {
                new Document(Guid.NewGuid(), "Doc1", null, "Content1", "Summary1", "file1.txt", DateTime.UtcNow, "txt", 1.0)
            };
            List<DocumentDTO> docDTOs = new List<DocumentDTO>
            {
                new DocumentDTO(Guid.NewGuid(), "Doc1", null, "Content1", "Summary1", "file1.txt", DateTime.UtcNow, "txt", 1.0)
            };

            _documentService.Setup(s => s.SearchForDocumentAsync(query)).ReturnsAsync(docs);
            _mapper.Setup(m => m.Map<List<DocumentDTO>>(It.IsAny<List<Document>>()))
                   .Returns(docDTOs);

            DocumentController controller = CreateController();
            ActionResult result = await controller.GetByQuery(query);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetByQuery_ServiceException_ReturnsProblem()
        {
            string query = "test";
            _documentService.Setup(s => s.SearchForDocumentAsync(query))
                .ThrowsAsync(new ServiceException("Search error", ExceptionType.Internal));

            DocumentController controller = CreateController();
            ActionResult result = await controller.GetByQuery(query);

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task Post_ValidFile_ReturnsCreated()
        {
            Mock<IFormFile> file = new();
            file.SetupGet(f => f.FileName).Returns("test.pdf");
            file.SetupGet(f => f.Length).Returns(1024);
            file.SetupGet(f => f.ContentType).Returns("application/pdf");
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            Guid docId = Guid.NewGuid();
            Document doc = new Document(docId, "test.pdf", null, "loading...", "loading...", $"{docId}.test.pdf", DateTime.UtcNow, ".pdf", 1.0);
            DocumentDTO docDTO = new DocumentDTO(docId, "test.pdf", null, "loading...", "loading...", $"{docId}.test.pdf", DateTime.UtcNow, ".pdf", 1.0);

            _documentService.Setup(s => s.UploadDocumentAsync(It.IsAny<Document>(), It.IsAny<Stream>()))
                .Returns(Task.CompletedTask);
            _mapper.Setup(m => m.Map<Document>(It.IsAny<DocumentDTO>())).Returns(doc);

            DocumentController controller = CreateController();
            ActionResult<DocumentDTO> result = await controller.Post(file.Object);

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public async Task Post_NullFile_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult<DocumentDTO> result = await controller.Post(null!);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Post_EmptyFile_ReturnsBadRequest()
        {
            Mock<IFormFile> file = new();
            file.SetupGet(f => f.Length).Returns(0);

            DocumentController controller = CreateController();
            ActionResult<DocumentDTO> result = await controller.Post(file.Object);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Post_ServiceException_ReturnsProblem()
        {
            Mock<IFormFile> file = new();
            file.SetupGet(f => f.FileName).Returns("test.pdf");
            file.SetupGet(f => f.Length).Returns(1024);
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            _documentService.Setup(s => s.UploadDocumentAsync(It.IsAny<Document>(), It.IsAny<Stream>()))
                .ThrowsAsync(new ServiceException("Upload error", ExceptionType.Internal));

            DocumentController controller = CreateController();
            ActionResult<DocumentDTO> result = await controller.Post(file.Object);

            Assert.IsType<ObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostServicesResult_ValidPayload_ReturnsCreated()
        {
            Guid docId = Guid.NewGuid();
            ServicesResultDTO payload = new ServicesResultDTO
            {
                DocumentId = docId.ToString(),
                CategoryId = Guid.NewGuid().ToString(),
                OcrResult = "OCR content",
                Summary = "Summary content"
            };

            _documentService.Setup(s => s.UpdateDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(Task.CompletedTask);

            DocumentController controller = CreateController();
            ActionResult result = await controller.PostServicesResult(docId.ToString(), payload);

            Assert.IsType<CreatedResult>(result);
        }

        [Fact]
        public async Task PostServicesResult_NullPayload_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.PostServicesResult(Guid.NewGuid().ToString(), null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostServicesResult_MismatchedId_ReturnsBadRequest()
        {
            ServicesResultDTO payload = new ServicesResultDTO
            {
                DocumentId = Guid.NewGuid().ToString(),
                CategoryId = Guid.NewGuid().ToString(),
                OcrResult = "OCR content",
                Summary = "Summary content"
            };

            DocumentController controller = CreateController();
            ActionResult result = await controller.PostServicesResult(Guid.NewGuid().ToString(), payload);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostServicesResult_ServiceException_ReturnsProblem()
        {
            Guid docId = Guid.NewGuid();
            ServicesResultDTO payload = new ServicesResultDTO
            {
                DocumentId = docId.ToString(),
                CategoryId = Guid.NewGuid().ToString(),
                OcrResult = "OCR content",
                Summary = "Summary content"
            };

            _documentService.Setup(s => s.UpdateDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ThrowsAsync(new ServiceException("Update error", ExceptionType.Internal));

            DocumentController controller = CreateController();
            ActionResult result = await controller.PostServicesResult(docId.ToString(), payload);

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task PutDocumentCategory_ValidIds_ReturnsOk()
        {
            Guid docId = Guid.NewGuid();
            Guid catId = Guid.NewGuid();

            _documentService.Setup(s => s.UpdateDocumentCategoryAsync(docId, catId))
                .Returns(Task.CompletedTask);

            DocumentController controller = CreateController();
            ActionResult result = await controller.PutDocumentCategory(docId.ToString(), catId.ToString());

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task PutDocumentCategory_InvalidDocumentGuid_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.PutDocumentCategory("invalid-guid", Guid.NewGuid().ToString());
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PutDocumentCategory_InvalidCategoryGuid_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.PutDocumentCategory(Guid.NewGuid().ToString(), "invalid-guid");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PutDocumentCategory_EmptyDocumentId_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.PutDocumentCategory("", Guid.NewGuid().ToString());
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PutDocumentCategory_ServiceException_ReturnsProblem()
        {
            Guid docId = Guid.NewGuid();
            Guid catId = Guid.NewGuid();

            _documentService.Setup(s => s.UpdateDocumentCategoryAsync(docId, catId))
                .ThrowsAsync(new ServiceException("Update error", ExceptionType.Validation));

            DocumentController controller = CreateController();
            ActionResult result = await controller.PutDocumentCategory(docId.ToString(), catId.ToString());

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAll_ReturnsOk()
        {
            _documentService.Setup(s => s.DeleteDocumentsAsync()).Returns(Task.CompletedTask);
            DocumentController controller = CreateController();
            ActionResult result = await controller.DeleteAll();
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteAll_ServiceException_ReturnsProblem()
        {
            _documentService.Setup(s => s.DeleteDocumentsAsync())
                .ThrowsAsync(new ServiceException("Delete error", ExceptionType.Internal));

            DocumentController controller = CreateController();
            ActionResult result = await controller.DeleteAll();

            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ById_ReturnsOk()
        {
            Guid id = Guid.NewGuid();
            _documentService.Setup(s => s.DeleteDocumentAsync(id)).Returns(Task.CompletedTask);
            DocumentController controller = CreateController();
            ActionResult result = await controller.Delete(id.ToString());
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Delete_InvalidId_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.Delete("not-a-guid");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ServiceException_ReturnsProblem()
        {
            Guid id = Guid.NewGuid();
            _documentService.Setup(s => s.DeleteDocumentAsync(id))
                .ThrowsAsync(new ServiceException("Delete error", ExceptionType.Validation));

            DocumentController controller = CreateController();
            ActionResult result = await controller.Delete(id.ToString());

            Assert.IsType<ObjectResult>(result);
        }
    }
}
