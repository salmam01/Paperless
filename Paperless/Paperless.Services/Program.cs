using Microsoft.Extensions.Options;
using Minio;
using Paperless.Services.Configurations;
using Paperless.Services.Workers;
using Paperless.Services.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

//  Logger configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<MinIOConfig>(builder.Configuration.GetSection("Minio"));

builder.Services.AddSingleton<MinIOService>();
builder.Services.AddHostedService<OCRWorker>();

var host = builder.Build();
host.Run();
