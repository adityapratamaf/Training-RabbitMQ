using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductImportService.Controllers;
using ProductImportService.Validators;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ProductCsvRowValidator>();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rmq = builder.Configuration.GetSection("RabbitMq");

        cfg.Host(rmq["Host"], "/", h =>
        {
            h.Username(rmq["Username"]!);
            h.Password(rmq["Password"]!);
        });

        // Exchange name fixed
        cfg.Message<Messaging.ProductUpsertMessage>(m => m.SetEntityName("product-upsert"));
        cfg.Publish<Messaging.ProductUpsertMessage>(p => p.ExchangeType = ExchangeType.Direct);
    });
});

// Hosted service untuk start/stop bus otomatis
//builder.Services.AddMassTransitHostedService();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
