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

builder.Services.Configure<AccessDataConfiguration>(
    builder.Configuration.GetSection("AccessData")
);
builder.Services.Configure<JobConfiguration>(
    builder.Configuration.GetSection("Job")
);

builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService();
builder.Services.ConfigureOptions<AccessDataJobSetup>();


string basePath = AppContext.BaseDirectory;



var host = builder.Build();
host.Run();
