using Microsoft.Extensions.Options;
using Minio;
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

builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<MinIOConfig>(builder.Configuration.GetSection("Minio"));
builder.Services.Configure<MinIOConfig>(builder.Configuration.GetSection("OCR"));

builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OCRService>();
builder.Services.AddHostedService<OCRWorker>();

var host = builder.Build();
host.Run();
