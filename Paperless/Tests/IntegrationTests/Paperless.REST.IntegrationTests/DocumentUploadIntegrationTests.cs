using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Paperless.API.DTOs;
using Paperless.BL.Models.Domain;
using Paperless.BL.Services.Messaging;
using Paperless.BL.Services.Search;
using Paperless.BL.Services.Storage;
using Paperless.DAL.Database;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Testcontainers.PostgreSql;
using Xunit;

namespace Paperless.REST.IntegrationTests
{
    /// <summary>
    /// Integration test for the "document upload" use case.
    /// Tests the complete flow: Upload -> Storage -> RabbitMQ -> Database
    /// Uses Testcontainers for real PostgreSQL database.
    /// </summary>
    public class DocumentUploadIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public DocumentUploadIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UploadDocument_CompleteFlow_Success()
        {
            // Arrange
            var fileName = "test-document.txt";
            var fileContent = "This is a test document content for integration testing.";
            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(streamContent, "form", fileName);

            // Act
            var response = await _client.PostAsync("/api/document", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var documentDto = await response.Content.ReadFromJsonAsync<DocumentDTO>();
            Assert.NotNull(documentDto);
            Assert.NotEqual(Guid.Empty, documentDto.Id);
            Assert.Equal(fileName, documentDto.Name);

            // DTO might have original extension, but database will have normalized type
            // Check database for normalized type instead

            // Verify document was saved in database
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PaperlessDbContext>();
                var document = await dbContext.Documents.FindAsync(documentDto.Id);
                Assert.NotNull(document);
                Assert.Equal(fileName, document.Name);
                Assert.Equal("TXT", document.Type);
            }
            
                // AdjustFileType() normalizes after DTO creation

            // Verify RabbitMQ message was published (via mock veriication)
            var publisherMock = _factory.DocumentPublisherMock;
            Assert.NotNull(publisherMock);
            publisherMock.Verify(
                x => x.PublishDocumentAsync(
                    It.Is<Guid>(id => id == documentDto.Id),
                    It.IsAny<List<Category>>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadDocument_InvalidFile_ReturnsBadRequest()
        {
            
            var content = new MultipartFormDataContent();
            var emptyStream = new StreamContent(new MemoryStream());
            content.Add(emptyStream, "form", "empty.txt");

          
            var response = await _client.PostAsync("/api/document", content);
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadDocument_PDF_StoredInMinIO()
        {
           
            var fileName = "test-document.pdf";
            var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(new MemoryStream(fileContent));
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "form", fileName);

            
            var response = await _client.PostAsync("/api/document", content);

       
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var documentDto = await response.Content.ReadFromJsonAsync<DocumentDTO>();
            Assert.NotNull(documentDto);
           
            
            // Verify document was saved in database with normalized type
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PaperlessDbContext>();
                var document = await dbContext.Documents.FindAsync(documentDto.Id);
                Assert.NotNull(document);
                Assert.Equal("PDF", document.Type);
            }

            // Verify storage service was called (via mock)
            var storageMock = _factory.StorageServiceMock;
            Assert.NotNull(storageMock);
            storageMock.Verify(
                x => x.StoreDocumentAsync(
                    It.Is<Document>(d => d.Id == documentDto.Id && d.Type == "PDF"),
                    It.IsAny<Stream>()),
                Times.Once);
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task DisposeAsync() => Task.CompletedTask;
    }

    /// <summary>
    /// Custom WebApplicationFactory for integration tests.
    /// Uses Testcontainers to provide a real PostgreSQL database.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer;
        public Mock<IDocumentPublisher> DocumentPublisherMock { get; private set; } = null!;
        public Mock<IStorageService> StorageServiceMock { get; private set; } = null!;
        public Mock<IDocumentSearchService> SearchServiceMock { get; private set; } = null!;

        public CustomWebApplicationFactory()
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .Build();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing PostgreSQL registration from Program.cs
                var dbContextOptionsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<PaperlessDbContext>));
                if (dbContextOptionsDescriptor != null)
                {
                    services.Remove(dbContextOptionsDescriptor);
                }

                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(PaperlessDbContext));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // Register PostgreSQL database using Testcontainer connection string
                services.AddDbContext<PaperlessDbContext>(options =>
                {
                    options.UseNpgsql(_postgresContainer.GetConnectionString());
                }, ServiceLifetime.Scoped);

                // Create mocks for external services
                DocumentPublisherMock = new Mock<IDocumentPublisher>();
                DocumentPublisherMock.Setup(x => x.PublishDocumentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<List<Category>>()))
                    .Returns(Task.CompletedTask);

                StorageServiceMock = new Mock<IStorageService>();
                StorageServiceMock.Setup(x => x.StoreDocumentAsync(
                    It.IsAny<Document>(),
                    It.IsAny<Stream>()))
                    .Returns(Task.CompletedTask);

                SearchServiceMock = new Mock<IDocumentSearchService>();
                SearchServiceMock.Setup(x => x.SearchAsync(It.IsAny<string>()))
                    .ReturnsAsync(new List<Paperless.BL.Models.DTOs.SearchResult>());

                // Replace services with mocks
                var publisherDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IDocumentPublisher));
                if (publisherDescriptor != null)
                {
                    services.Remove(publisherDescriptor);
                }
                services.AddSingleton(DocumentPublisherMock.Object);

                var storageDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IStorageService));
                if (storageDescriptor != null)
                {
                    services.Remove(storageDescriptor);
                }
                services.AddSingleton(StorageServiceMock.Object);

                var searchDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IDocumentSearchService));
                if (searchDescriptor != null)
                {
                    services.Remove(searchDescriptor);
                }
                services.AddSingleton(SearchServiceMock.Object);
            });

            builder.UseEnvironment("Testing");
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            
            // Initialize database with migrations and test data
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PaperlessDbContext>();
                db.Database.Migrate();

               
                if (!db.Categories.Any())
                {
                    var categories = new[]
                    {
                        new Paperless.DAL.Entities.CategoryEntity { Id = Guid.NewGuid(), Name = "School" },
                        new Paperless.DAL.Entities.CategoryEntity { Id = Guid.NewGuid(), Name = "Work" },
                        new Paperless.DAL.Entities.CategoryEntity { Id = Guid.NewGuid(), Name = "Medical" },
                        new Paperless.DAL.Entities.CategoryEntity { Id = Guid.NewGuid(), Name = "Finance" },
                        new Paperless.DAL.Entities.CategoryEntity { Id = Guid.NewGuid(), Name = "Legal" }
                    };
                    db.Categories.AddRange(categories);
                    db.SaveChanges();
                }
            }
            return host;
        }

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();
        }

        public new async Task DisposeAsync()
        {
            await _postgresContainer.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
