using AutoMapper;
using Paperless.API.DTOs;
using Paperless.DAL.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
ILoggerFactory loggerFactory;

//  Automapper: change to DocumentDTO => DocumentEntity later
var config = new MapperConfiguration(
    cfg => cfg.CreateMap<DocumentDTO, DocumentEntity>()
);
var mapper = config.CreateMapper();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
