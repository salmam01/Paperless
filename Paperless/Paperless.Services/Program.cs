using Paperless.Services.Configurations;
using Paperless.Services.Services.FileStorage;
using Paperless.Services.Services.Clients;
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
builder.Services.Configure<RESTConfig>(builder.Configuration.GetSection("REST"));
builder.Services.Configure<MinIOConfig>(builder.Configuration.GetSection("MinIO"));
builder.Services.Configure<OCRConfig>(builder.Configuration.GetSection("OCR"));
builder.Services.Configure<GenAIConfig>(builder.Configuration.GetSection("GenAI"));
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<ElasticSearchConfig>(builder.Configuration.GetSection("ElasticSearch"));
builder.Services.Configure<MQPublisherConfig>(builder.Configuration.GetSection("MQPublisher"));

//  Configuration for different Queues
builder.Services.Configure<ListenerConfig>("OCRListener", builder.Configuration.GetSection("OCRListener"));
builder.Services.Configure<ListenerConfig>("SummaryListener", builder.Configuration.GetSection("SummaryListener"));
builder.Services.Configure<ListenerConfig>("IndexingListener", builder.Configuration.GetSection("IndexingListener"));

//  Services
builder.Services.AddSingleton<MQConnectionFactory>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<OCRService>();
builder.Services.AddSingleton<GenAIService>();
builder.Services.AddSingleton<IElasticRepository, ElasticService>();
builder.Services.AddSingleton<MQPublisher>();

//  Listeners
builder.Services.AddSingleton<OCRListener>();
builder.Services.AddSingleton<GenAIListener>();
builder.Services.AddSingleton<IndexingListener>();

//  HttpClients
builder.Services.AddHttpClient<ResultClient>();
builder.Services.AddHttpClient<GenAIService>();

//  Workers
builder.Services.AddHostedService<OCRWorker>();
builder.Services.AddHostedService<GenAIWorker>();
builder.Services.AddHostedService<IndexingWorker>();

var host = builder.Build();
host.Run();
