using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Workers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

//  Logger configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();
builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddHostedService<OCRWorker>();

var host = builder.Build();
host.Run();
