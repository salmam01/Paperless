using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Paperless.API.Controllers;
using Paperless.BL.Services;
using Microsoft.Extensions.Options;
using Paperless.BL.Configurations;

namespace Paperless.Tests;

public class DocumentControllerValidationTests
{
    private readonly Mock<IDocumentService> _service = new();
    private readonly DocumentPublisher _publisher;
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

    public DocumentControllerValidationTests()
    {
        RabbitMqConfig cfg = new RabbitMqConfig { Host = "localhost", Port = 5672, User = "guest", Password = "guest", QueueName = "test" };
        _publisher = new DocumentPublisher(Options.Create(cfg), Mock.Of<ILogger<DocumentPublisher>>());
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
}


