using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Data;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

namespace Paperless.Tests;

public class DocumentRepositoryTests
{
    private static PaperlessDbContext CreateInMemoryDbContext()
    {
        DbContextOptions<PaperlessDbContext> options = new DbContextOptionsBuilder<PaperlessDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaperlessDbContext(options);
    }

    [Fact]
    public void Insert_And_Get_By_Id_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        DocumentEntity entity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Name = "Doc 1",
            Content = "Hello",
            Summary = "Sum",
            CreationDate = DateTime.UtcNow,
            Type = "txt",
            Size = 1.23
        };
        repo.InsertDocument(entity);

        DocumentEntity? loaded = repo.GetDocumentById(entity.Id);
        Assert.Equal("Doc 1", loaded.Name);
    }

    [Fact]
    public void Update_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        DocumentEntity entity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Name = "Doc",
            Content = "C",
            Summary = "S",
            CreationDate = DateTime.UtcNow,
            Type = "t",
            Size = 1
        };
        repo.InsertDocument(entity);

        entity.Name = "Updated";
        repo.UpdateDocument(entity);

        DocumentEntity? loaded = repo.GetDocumentById(entity.Id);
        Assert.Equal("Updated", loaded.Name);
    }

    [Fact]
    public void Delete_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        DocumentEntity entity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Name = "Doc",
            Content = "C",
            Summary = "S",
            CreationDate = DateTime.UtcNow,
            Type = "t",
            Size = 1
        };
        repo.InsertDocument(entity);
        repo.DeleteDocument(entity.Id);

        Assert.Throws<KeyNotFoundException>(() => repo.GetDocumentById(entity.Id));
    }

    [Fact]
    public void GetAllDocuments_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        
        // Insert multiple documents
        repo.InsertDocument(new DocumentEntity { Id = Guid.NewGuid(), Name = "Doc1", Content = "Content1", Summary = "Summary1", CreationDate = DateTime.UtcNow, Type = "txt", Size = 1 });
        repo.InsertDocument(new DocumentEntity { Id = Guid.NewGuid(), Name = "Doc2", Content = "Content2", Summary = "Summary2", CreationDate = DateTime.UtcNow, Type = "pdf", Size = 2 });

        List<DocumentEntity> allDocs = repo.GetAllDocuments().ToList();
        Assert.Equal(2, allDocs.Count);
        Assert.Contains(allDocs, d => d.Name == "Doc1");
        Assert.Contains(allDocs, d => d.Name == "Doc2");
    }
}
