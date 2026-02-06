using MassTransit;
using Messaging;
using Microsoft.EntityFrameworkCore;
using ProductProductionService.Data;
using ProductProductionService.Models;

namespace ProductProductionService.Consumers;

public class ProductUpsertProductionConsumer : IConsumer<ProductUpsertMessage>
{
    private readonly AppDbContext _db;
    public ProductUpsertProductionConsumer(AppDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ProductUpsertMessage> context)
    {
        var msg = context.Message;

        // INSERT-only: jika SKU sudah ada => tolak (throw -> masuk error queue setelah retry)
        var exists = await _db.Products.AnyAsync(x => x.Sku == msg.Sku);
        if (exists)
            throw new InvalidOperationException($"Duplicate SKU in production: {msg.Sku}");

        _db.Products.Add(new Product
        {
            Sku = msg.Sku,
            Name = msg.Name,
            Description = msg.Description,
            Price = msg.Price,
            Stock = msg.Stock,
            IsActive = msg.IsActive
        });

        await _db.SaveChangesAsync();
    }
}
