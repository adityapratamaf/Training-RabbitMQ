using MassTransit;
using Messaging;
using Microsoft.EntityFrameworkCore;
using ProductStagingService.Data;
using ProductStagingService.Models;

namespace ProductStagingService.Consumers
{
    public class ProductUpsertStagingConsumer : IConsumer<ProductUpsertMessage>
    {
        private readonly AppDbContext _db;
        public ProductUpsertStagingConsumer(AppDbContext db) => _db = db;

        // handle message dari ProductUploadService
        public async Task Consume(ConsumeContext<ProductUpsertMessage> context)
        {
            var msg = context.Message;

            // Duplicate against existing staging (SKU sama) -> tolak (tidak insert)
            var existsSku = await _db.StagingProducts.AnyAsync(x => x.Sku == msg.Sku && x.Status != "Reject");
            if (existsSku)
            {
                // simpan sebagai record rejected, boleh juga skip tanpa insert
                _db.StagingProducts.Add(new StagingProduct
                {
                    BatchId = msg.BatchId,
                    RowNumber = msg.RowNumber,
                    Sku = msg.Sku,
                    Name = msg.Name,
                    Description = msg.Description,
                    Price = msg.Price,
                    Stock = msg.Stock,
                    IsActive = msg.IsActive,
                    Status = "Reject"
                });
                await _db.SaveChangesAsync();
                return;
            }

            // masukkan ke staging dengan status "Pending"
            _db.StagingProducts.Add(new StagingProduct
            {
                BatchId = msg.BatchId,
                RowNumber = msg.RowNumber,
                Sku = msg.Sku,
                Name = msg.Name,
                Description = msg.Description,
                Price = msg.Price,
                Stock = msg.Stock,
                IsActive = msg.IsActive,
                Status = "Pending"
            });

            await _db.SaveChangesAsync();
        }
    }
}



