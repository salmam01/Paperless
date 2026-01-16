using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Paperless.API.Controllers;
using Paperless.API.DTOs;
using Paperless.BL.Services.Documents;

namespace Paperless.REST.Tests
{
    public class DocumentControllerValidationTests
    {
        private readonly Mock<IDocumentService> _service = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<DocumentController>> _logger = new();

        private DocumentController CreateController()
        {
            DocumentController controller = new DocumentController(_service.Object, _mapper.Object, _logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        [Fact]
        public async Task Delete_InvalidGuid_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.Delete("invalid");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Get_InvalidGuid_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.Get("invalid");
            Assert.IsType<BadRequestObjectResult>(result);
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
        public async Task PutDocumentCategory_EmptyCategoryId_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.PutDocumentCategory(Guid.NewGuid().ToString(), "");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostServicesResult_NullPayload_ReturnsBadRequest()
        {
            DocumentController controller = CreateController();
            ActionResult result = await controller.PostServicesResult(Guid.NewGuid().ToString(), null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostServicesResult_MismatchedDocumentId_ReturnsBadRequest()
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
    }
}


