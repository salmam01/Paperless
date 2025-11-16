using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Paperless.Services.Configurations;
using Paperless.Services.Services;
using Paperless.Services.Services.MessageQueue;
using Paperless.Services.Workers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

//  Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

//  Configuration
builder.Services.Configure<MinIoConfig>(builder.Configuration.GetSection("MinIo"));
builder.Services.Configure<OcrConfig>(builder.Configuration.GetSection("Ocr"));
builder.Services.Configure<GenAIConfig>(builder.Configuration.GetSection("GenAI"));
builder.Services.Configure<RabbitMqConfig>("RabbitMqOcr", builder.Configuration.GetSection("RabbitMqOcr"));
builder.Services.Configure<RabbitMqConfig>("RabbitMqSummary", builder.Configuration.GetSection("RabbitMqSummary"));

//  Services
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OcrService>();
builder.Services.AddSingleton<GenAIService>();
builder.Services.AddSingleton<MQPublisher>();
builder.Services.AddKeyedSingleton("OcrListener", (sp, key) =>
{
    ILogger<MQListener> logger = sp.GetRequiredService<ILogger<MQListener>>();
    RabbitMqConfig config = sp.GetRequiredService<IOptionsMonitor<RabbitMqConfig>>()
                   .Get("RabbitMqOcr");

    return new MQListener(logger, Options.Create(config));
});
builder.Services.AddKeyedSingleton("SummaryListener", (sp, key) =>
{
    ILogger<MQListener> logger = sp.GetRequiredService<ILogger<MQListener>>();
    RabbitMqConfig config = sp.GetRequiredService<IOptionsMonitor<RabbitMqConfig>>()
                   .Get("RabbitMqOcr");

    return new MQListener(logger, Options.Create(config));
});

//  Workers
builder.Services.AddHostedService<OcrWorker>();
builder.Services.AddHostedService<GenAIWorker>();

var host = builder.Build();
host.Run();
