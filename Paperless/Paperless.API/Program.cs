using AutoMapper;
using Paperless.API.DTOs;
using Paperless.DAL.Data;
using Paperless.DAL.Entities;
using Paperless.DAL.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<PaperlessDbContext>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

//  Automapper: change to DocumentDTO => DocumentEntity later
var mapperConfig = new MapperConfiguration(
    cfg => {
        cfg.CreateMap<DocumentDTO, DocumentEntity>();
        cfg.CreateMap<DocumentEntity, DocumentDTO>();
    }
);
IMapper mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
