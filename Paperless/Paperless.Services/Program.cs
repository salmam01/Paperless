using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Paperless.DAL.Database;
using Paperless.DAL.Repositories;
using Paperless.Services.Configurations;
using Paperless.Services.Workers;
using Paperless.Services.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

//  Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Configuration
builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<MinIoConfig>(builder.Configuration.GetSection("MinIo"));
builder.Services.Configure<OcrConfig>(builder.Configuration.GetSection("Ocr"));
builder.Services.Configure<GenAIConfig>(builder.Configuration.GetSection("GenAI"));

// Database
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PaperlessDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Services
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OcrService>();
builder.Services.AddScoped<DocumentUpdateService>(); 
builder.Services.AddHostedService<OcrWorker>();

var host = builder.Build();
host.Run();
