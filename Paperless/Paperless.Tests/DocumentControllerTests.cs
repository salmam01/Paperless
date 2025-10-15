using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Paperless.API.Controllers;
using Paperless.API.Dtos;
using Paperless.API.Messaging;
using Paperless.BL.Models;
using Paperless.BL.Services;
using Microsoft.Extensions.Options;
using Paperless.API.Configurations;

namespace Paperless.Tests
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentService> _documentService = new();
        private readonly DocumentPublisher _documentPublisher;
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<DocumentController>> _logger = new();

        private DocumentController CreateController()
        {
            DocumentController controller = new DocumentController(_documentService.Object, _documentPublisher, _mapper.Object, _logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        public DocumentControllerTests()
        {
            RabbitMqConfig cfg = new RabbitMqConfig { Host = "localhost", Port = 5672, User = "guest", Password = "guest", QueueName = "test" };
            _documentPublisher = new DocumentPublisher(Options.Create(cfg), Mock.Of<ILogger<DocumentPublisher>>());
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            IEnumerable<Document> docs = new List<Document>
            {
                new Document(Guid.NewGuid(), "Doc1", "C1", "S1", "f", DateTime.UtcNow, "txt", 1.0),
                new Document(Guid.NewGuid(), "Doc2", "C2", "S2", "f", DateTime.UtcNow, "pdf", 2.0)
            };

            _documentService.Setup(s => s.GetDocumentsAsync()).ReturnsAsync(docs);
            _mapper.Setup(m => m.Map<IEnumerable<DocumentDto>>(It.IsAny<IEnumerable<Document>>()))
                   .Returns(new List<DocumentDto>());

            DocumentController controller = CreateController();
            ActionResult<IEnumerable<DocumentDto>> result = await controller.GetAll();

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Get_WithValidId_ReturnsOk()
        {
            Guid id = Guid.NewGuid();
            Document doc = new Document(id, "Doc", "C", "S", "f", DateTime.UtcNow, "txt", 1.0);

            _documentService.Setup(s => s.GetDocumentAsync(id)).ReturnsAsync(doc);
            _mapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>()))
                   .Returns(new DocumentDto { Id = id, Name = "Doc" });

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
        public async Task UploadDocument_EmptyFile_ReturnsBadRequest()
        {
            Mock<IFormFile> file = new();
            file.SetupGet(f => f.Length).Returns(0);

            DocumentController controller = CreateController();
            ActionResult<DocumentDto> result = await controller.UploadDocument(file.Object);
            Assert.IsType<BadRequestObjectResult>(result.Result);
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
        public async Task Delete_ById_ReturnsOk()
        {
            Guid id = Guid.NewGuid();
            _documentService.Setup(s => s.DeleteDocumentAsync(id)).Returns(Task.CompletedTask);
            DocumentController controller = CreateController();
            ActionResult result = await controller.Delete(id.ToString());
            Assert.IsType<OkResult>(result);
        }
    }
}
