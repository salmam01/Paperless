using Paperless.Batch;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<BatchProcessingWorker>();

var host = builder.Build();
host.Run();
