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

builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<MinIoConfig>(builder.Configuration.GetSection("MinIo"));
builder.Services.Configure<OcrConfig>(builder.Configuration.GetSection("Ocr"));

builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OcrService>();
builder.Services.AddHostedService<OcrWorker>();

var host = builder.Build();
host.Run();
