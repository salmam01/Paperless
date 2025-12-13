using Microsoft.Extensions.Options;
using Paperless.Services.Configurations;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.HttpClients;
using Paperless.Services.Services.MessageQueues;
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

//  Configuration for different Queues
builder.Services.Configure<QueueConfig>("MQPublisher", builder.Configuration.GetSection("MQPublisher"));
builder.Services.Configure<QueueConfig>("OCRQueue", builder.Configuration.GetSection("OCRQueue"));
builder.Services.Configure<QueueConfig>("SummaryQueue", builder.Configuration.GetSection("SummaryQueue"));
builder.Services.Configure<QueueConfig>("IndexingQueue", builder.Configuration.GetSection("IndexingQueue"));

//  Services
builder.Services.AddSingleton<MQConnectionFactory>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OCRService>();
builder.Services.AddSingleton<GenAIService>();
builder.Services.AddSingleton<IElasticRepository, ElasticService>();

builder.Services.AddSingleton<MQPublisher>(sp =>
{
    ILogger<MQPublisher> logger = sp.GetRequiredService<ILogger<MQPublisher>>();
    QueueConfig config = sp.GetRequiredService<IOptionsMonitor<QueueConfig>>()
        .Get("MQPublisher");

    return new MQPublisher(logger, Options.Create(config), sp.GetRequiredService<MQConnectionFactory>());
});

builder.Services.AddSingleton<OCRListener> (sp =>
{
    ILogger<OCRListener> logger = sp.GetRequiredService<ILogger<OCRListener>>();
    QueueConfig config = sp.GetRequiredService<IOptionsMonitor<QueueConfig>>()
        .Get("OCRQueue");

    return new OCRListener(logger, Options.Create(config), sp.GetRequiredService<MQConnectionFactory>());
});
builder.Services.AddKeyedSingleton("SummaryListener", (sp, key) =>
{
    ILogger<MQListener> logger = sp.GetRequiredService<ILogger<MQListener>>();
    QueueConfig config = sp.GetRequiredService<IOptionsMonitor<QueueConfig>>()
        .Get("SummaryQueue");

    return new MQListener(logger, Options.Create(config), sp.GetRequiredService<MQConnectionFactory>());
});
builder.Services.AddKeyedSingleton("IndexingListener", (sp, key) =>
{
    ILogger<MQListener> logger = sp.GetRequiredService<ILogger<MQListener>>();
    QueueConfig config = sp.GetRequiredService<IOptionsMonitor<QueueConfig>>()
        .Get("IndexingQueue");

    return new MQListener(logger, Options.Create(config), sp.GetRequiredService<MQConnectionFactory>());
});

//  HttpClients
builder.Services.AddHttpClient<WorkerResultsService>();
builder.Services.AddHttpClient<GenAIService>();

//  Workers
builder.Services.AddHostedService<OCRWorker>();
builder.Services.AddHostedService<GenAIWorker>();
builder.Services.AddHostedService<IndexingWorker>();

var host = builder.Build();
host.Run();
