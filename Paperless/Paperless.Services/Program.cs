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

// Configuration
builder.Services.Configure<MinIoConfig>(builder.Configuration.GetSection("MinIo"));
builder.Services.Configure<OcrConfig>(builder.Configuration.GetSection("Ocr"));
builder.Services.Configure<GenAIConfig>(builder.Configuration.GetSection("GenAI"));
builder.Services.Configure<RabbitMqConfig>("RabbitMqOcr", builder.Configuration.GetSection("RabbitMqOcr"));

// Services
builder.Services.AddSingleton<MQListener>(sp =>
{
    RabbitMqConfig config = sp.GetRequiredService<IOptionsMonitor<RabbitMqConfig>>().Get("RabbitMqOcr");
    return new MQListener(
        sp.GetRequiredService<ILogger<MQListener>>(),
        config
    );
});
builder.Services.AddSingleton<MQListener>(sp =>
{
    RabbitMqConfig config = sp.GetRequiredService<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMqSummary"));
    return new MQListener(
        sp.GetRequiredService<ILogger<MQListener>>(),
        config
    );
});

builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OcrService>();

builder.Services.AddHostedService<OcrWorker>();

var host = builder.Build();
host.Run();
