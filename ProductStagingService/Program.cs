using MassTransit;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using ProductStagingService.Data;
using ProductStagingService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductUpsertStagingConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rmq = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rmq["Host"], "/", h =>
        {
            h.Username(rmq["Username"]!);
            h.Password(rmq["Password"]!);
        });

        cfg.Message<Messaging.ProductUpsertMessage>(m => m.SetEntityName("product-upsert"));
        cfg.Publish<Messaging.ProductUpsertMessage>(p => p.ExchangeType = ExchangeType.Direct);

        // Staging endpoint
        cfg.ReceiveEndpoint("stg-product-upsert", e =>
        {
            e.Bind<Messaging.ProductUpsertMessage>(b =>
            {
                b.ExchangeType = ExchangeType.Direct;
                b.RoutingKey = "staging";
            });

            e.ConfigureConsumer<ProductUpsertStagingConsumer>(context);
        });
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
