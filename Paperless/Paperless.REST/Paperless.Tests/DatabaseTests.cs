using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Database;
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
    public async Task Insert_And_Get_By_Id_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        DocumentEntity entity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Name = "Doc 1",
            Content = "Hello",
            Summary = "Sum",
            FilePath = "f",
            CreationDate = DateTime.UtcNow,
            Type = "txt",
            Size = 1.23
        };
        await repo.InsertDocumentAsync(entity);

        DocumentEntity? loaded = await repo.GetDocumentAsync(entity.Id);
        Assert.Equal("Doc 1", loaded.Name);
    }

    [Fact]
    public async Task Update_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        DocumentEntity entity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Name = "Doc",
            Content = "C",
            Summary = "S",
            FilePath = "f",
            CreationDate = DateTime.UtcNow,
            Type = "t",
            Size = 1
        };
        await repo.InsertDocumentAsync(entity);

        entity.Name = "Updated";
        await repo.UpdateDocumentAsync(entity);

        DocumentEntity? loaded = await repo.GetDocumentAsync(entity.Id);
        Assert.Equal("Updated", loaded.Name);
    }

    [Fact]
    public async Task Delete_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        DocumentEntity entity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            Name = "Doc",
            Content = "C",
            Summary = "S",
            FilePath = "f",
            CreationDate = DateTime.UtcNow,
            Type = "t",
            Size = 1
        };
        await repo.InsertDocumentAsync(entity);
        await repo.DeleteDocumentAsync(entity.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await repo.GetDocumentAsync(entity.Id));
    }

    [Fact]
    public async Task GetAllDocuments_Works()
    {
        using PaperlessDbContext ctx = CreateInMemoryDbContext();
        DocumentRepository repo = new DocumentRepository(ctx);
        
        await repo.InsertDocumentAsync(new DocumentEntity { Id = Guid.NewGuid(), Name = "Doc1", Content = "Content1", Summary = "Summary1", FilePath = "f1", CreationDate = DateTime.UtcNow, Type = "txt", Size = 1 });
        await repo.InsertDocumentAsync(new DocumentEntity { Id = Guid.NewGuid(), Name = "Doc2", Content = "Content2", Summary = "Summary2", FilePath = "f2", CreationDate = DateTime.UtcNow, Type = "pdf", Size = 2 });

        List<DocumentEntity> allDocs = (await repo.GetDocumentsAsync()).ToList();
        Assert.Equal(2, allDocs.Count);
        Assert.Contains(allDocs, d => d.Name == "Doc1");
        Assert.Contains(allDocs, d => d.Name == "Doc2");
    }
}
