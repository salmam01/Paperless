using Microsoft.Extensions.Options;
using Paperless.Services;
using Paperless.Services.Configurations;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
