using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Paperless.API.DTOs;
using Paperless.BL.Configurations;
using Paperless.BL.Helpers;
using Paperless.BL.Models.Domain;
using Paperless.BL.Services.Categories;
using Paperless.BL.Services.Documents;
using Paperless.BL.Services.Messaging;
using Paperless.BL.Services.Search;
using Paperless.BL.Services.Storage;
using Paperless.DAL.Database;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories.Categories;
using Paperless.DAL.Repositories.Documents;
using Paperless.Services.Configurations;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//  Load configuration from appsettings.json
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true)
    .Build();

//  Logger configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

//  Automapper configuration
MapperConfiguration mapperConfig = new(
    cfg => {
        cfg.CreateMap<DocumentDTO, Document>().ReverseMap();
        cfg.CreateMap<Document, DocumentEntity>().ReverseMap();
        cfg.CreateMap<CategoryDTO, Category>().ReverseMap();
        cfg.CreateMap<Category, CategoryEntity>().ReverseMap();
    }
);
IMapper mapper = mapperConfig.CreateMapper();

// Add services to the container
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog();
});

// Configurations
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<MinIOConfig>(builder.Configuration.GetSection("MinIO"));
builder.Services.Configure<ElasticSearchConfig>(builder.Configuration.GetSection("ElasticSearch"));
builder.Services.Configure<CategoriesConfig>(builder.Configuration.GetSection("Categories"));
builder.Services.AddScoped<PaperlessDbContext>();

builder.Services.AddSingleton<Parser>();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IDocumentSearchService, DocumentSearchService>();
builder.Services.AddSingleton<MQConnectionFactory>();
builder.Services.AddSingleton<IDocumentPublisher, DocumentPublisher>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddSingleton(mapper);

builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaperlessDbContext>();
    db.Database.Migrate();

    //  Populate Database with Categories
    ICategoryService categoryService = scope.ServiceProvider
        .GetRequiredService<ICategoryService>();
    CategoriesConfig categoriesConfig = scope.ServiceProvider
        .GetRequiredService<IOptions<CategoriesConfig>>().Value;

    List<Category> existing = await categoryService.GetCategoriesAsync();

    if (!existing.Any())
    {
        var categories = categoriesConfig.Names
            .Select(name => new Category { Name = name })
            .ToList();

        await categoryService.PopulateCategoriesAsync(categories);
    }
}

app.Run();
