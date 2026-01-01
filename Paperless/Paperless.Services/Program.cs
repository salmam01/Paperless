using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.HttpClients;
using Paperless.Services.Services.Messaging.Base;
using Paperless.Services.Services.Messaging.Listeners;
using Paperless.Services.Services.Messaging.Publishers;
using Paperless.Services.Services.OCR;
using Paperless.Services.Services.Search;
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
builder.Services.Configure<RESTConfig>(builder.Configuration.GetSection("Rest"));
builder.Services.Configure<MinIOConfig>(builder.Configuration.GetSection("MinIO"));
builder.Services.Configure<OCRConfig>(builder.Configuration.GetSection("OCR"));
builder.Services.Configure<GenAIConfig>(builder.Configuration.GetSection("GenAI"));
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<ElasticSearchConfig>(builder.Configuration.GetSection("ElasticSearch"));
builder.Services.Configure<MQPublisherConfig>(builder.Configuration.GetSection("MQPublisher"));

//  Configuration for different Queues
builder.Services.Configure<QueueConfig>("OCRQueue", builder.Configuration.GetSection("OCRQueue"));
builder.Services.Configure<QueueConfig>("SummaryQueue", builder.Configuration.GetSection("SummaryQueue"));
builder.Services.Configure<QueueConfig>("IndexingQueue", builder.Configuration.GetSection("IndexingQueue"));

//  Services
builder.Services.AddSingleton<MQConnectionFactory>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OCRService>();
builder.Services.AddSingleton<SummaryService>();
builder.Services.AddSingleton<IElasticRepository, ElasticService>();

builder.Services.AddSingleton<MQPublisher>();

builder.Services.AddSingleton<MQBaseListener, OCRListener> (sp =>
{
    ILogger<OCRListener> logger = sp.GetRequiredService<ILogger<OCRListener>>();
    QueueConfig config = sp.GetRequiredService<IOptionsMonitor<QueueConfig>>()
        .Get("OCRQueue");

    return new OCRListener(logger, Options.Create(config), sp.GetRequiredService<MQConnectionFactory>());
});
builder.Services.AddSingleton<MQBaseListener, SummaryListener> (sp =>
{
    ILogger<SummaryListener> logger = sp.GetRequiredService<ILogger<SummaryListener>>();
    QueueConfig config = sp.GetRequiredService<IOptionsMonitor<QueueConfig>>()
        .Get("SummaryQueue");

    return new SummaryListener(logger, Options.Create(config), sp.GetRequiredService<MQConnectionFactory>());
});
builder.Services.AddSingleton<MQBaseListener, IndexingListener> (sp =>
{
    ILogger<IndexingListener> logger = sp.GetRequiredService<ILogger<IndexingListener>>();
    QueueConfig config = sp.GetRequiredService<IOptionsMonitor<QueueConfig>>()
        .Get("IndexingQueue");

    return new IndexingListener(logger, Options.Create(config), sp.GetRequiredService<MQConnectionFactory>());
});

//  HttpClients
builder.Services.AddHttpClient<WorkerResultsService>();
builder.Services.AddHttpClient<SummaryService>();

//  Workers
builder.Services.AddHostedService<OCRWorker>();
builder.Services.AddHostedService<SummaryWorker>();
builder.Services.AddHostedService<IndexingWorker>();

var host = builder.Build();
host.Run();
