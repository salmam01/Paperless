using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Paperless.API.Controllers;
using Paperless.API.DTOs;
using Paperless.API.Mapping;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

namespace Paperless.Tests
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentRepository> _mockRepository;
        private readonly IMapper _mapper;
        private readonly DocumentController _controller;

        public DocumentControllerTests()
        {
            _mockRepository = new Mock<IDocumentRepository>();
            
            // Setup AutoMapper
            MapperConfiguration config = new MapperConfiguration(cfg => cfg.AddProfile<DocumentProfile>());
            _mapper = config.CreateMapper();
            _controller = new DocumentController(_mockRepository.Object, _mapper);
        }

        [Fact]
        public void GetAll_Works()
        {
            List<DocumentEntity> documents =
            [
                new DocumentEntity
                {
                    Id = Guid.NewGuid(), Name = "Doc1", Content = "Content1", Summary = "Summary1", CreationDate = DateTime.UtcNow,
                    Type = "txt", Size = 1.0
                },
                new DocumentEntity
                {
                    Id = Guid.NewGuid(), Name = "Doc2", Content = "Content2", Summary = "Summary2", CreationDate = DateTime.UtcNow,
                    Type = "pdf", Size = 2.0
                }
            ];
            
            _mockRepository.Setup(repo => repo.GetAllDocuments()).Returns(documents);
            ActionResult<IEnumerable<DocumentDTO>> result = _controller.GetAll();
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void Get_ValidId_Works()
        {
            Guid documentId = Guid.NewGuid();
            DocumentEntity document = new DocumentEntity 
            { 
                Id = documentId, 
                Name = "Test Doc", 
                Content = "Test Content", 
                Summary = "Test Summary", 
                CreationDate = DateTime.UtcNow, 
                Type = "txt", 
                Size = 1.0 
            };
            
            _mockRepository.Setup(repo => repo.GetDocumentById(documentId)).Returns(document);
            IActionResult result = _controller.Get(documentId.ToString());
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void Get_InvalidId_ReturnsBadRequest()
        {
            IActionResult result = _controller.Get("invalid-guid");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Create_Works()
        {
            DocumentDTO documentDto = new DocumentDTO
            {
                Name = "New Document",
                Content = "New Content"
            };

            _mockRepository.Setup(repo => repo.InsertDocument(It.IsAny<DocumentEntity>()));
            ActionResult<DocumentDTO> result = _controller.Create(documentDto);
            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public void Create_InvalidDocument_ReturnsBadRequest()
        {
            DocumentDTO documentDto = new DocumentDTO
            {
                Name = "",
                Content = "Valid Content"
            };
            
            ActionResult<DocumentDTO> result = _controller.Create(documentDto);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public void Delete_Works()
        {
            Guid documentId = Guid.NewGuid();
            _mockRepository.Setup(repo => repo.DeleteDocument(documentId));
            ActionResult result = _controller.Delete(documentId.ToString());
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public void DeleteAll_Works()
        {
            _mockRepository.Setup(repo => repo.DeleteAllDocuments());
            ActionResult result = _controller.DeleteAll();
            Assert.IsType<OkResult>(result);
        }
    }
}
