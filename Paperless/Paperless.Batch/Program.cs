using Paperless.Batch;
using Paperless.Batch.Configuration;
using Quartz;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

//  Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

string basePath = AppContext.BaseDirectory;

builder.Services.Configure<AccessDataConfiguration>(
    builder.Configuration.GetSection("AccessData")
);
builder.Services.Configure<JobConfiguration>(
    builder.Configuration.GetSection("Job")
);

builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService();
builder.Services.ConfigureOptions<AccessDataJobSetup>();
builder.Services.AddSingleton<AccessDataBatchProcessor>();

var host = builder.Build();
host.Run();
