using Microsoft.Extensions.Options;
using Paperless.Batch.Configuration;
using Paperless.Batch.Database;
using Paperless.Batch.Tasks;
using Quartz;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Load local configuration (overrides appsettings.json)
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

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
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AccessDataBatchProcessor>>();
    var options = sp.GetRequiredService<IOptions<AccessDataConfiguration>>();
    var basePath = AppContext.BaseDirectory;

    return new AccessDataBatchProcessor(logger, options, basePath);
});

builder.Services.AddSingleton<IDocumentRepository, DocumentRepository>();
builder.Services.AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<DatabaseConnection>>();
        var configuration = sp.GetRequiredService<IConfiguration>();

        var connectionString = configuration["ConnectionString"]
            ?? throw new InvalidOperationException("Postgres connection string missing");

        return new DatabaseConnection(logger, connectionString);
    }
);

var host = builder.Build();
host.Run();
