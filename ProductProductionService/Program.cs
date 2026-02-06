using MassTransit;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using ProductProductionService.Data;
using ProductProductionService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductUpsertProductionConsumer>();

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

        cfg.ReceiveEndpoint("prd-product-upsert", e =>
        {
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            e.Bind<Messaging.ProductUpsertMessage>(b =>
            {
                b.ExchangeType = ExchangeType.Direct;
                b.RoutingKey = "production";
            });

            e.ConfigureConsumer<ProductUpsertProductionConsumer>(context);
        });
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
