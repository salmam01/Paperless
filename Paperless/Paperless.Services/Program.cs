using Microsoft.Extensions.Options;
using Minio;
using Paperless.Services.Configurations;
using Paperless.Services.Workers;
using Paperless.Services.Services;
using Serilog;
using Paperless.Services.Services.MessageQueue;

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

// Services
builder.Services.AddSingleton<MQListener>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OcrService>();
//builder.Services.AddScoped<DocumentUpdateService>(); 
builder.Services.AddHostedService<OcrWorker>();

var host = builder.Build();
host.Run();
