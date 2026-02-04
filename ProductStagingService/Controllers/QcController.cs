using MassTransit;
using Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStagingService.Data;

namespace ProductStagingService.Controllers;

[ApiController]
[Route("api/qc")]
public class QcController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPublishEndpoint _publish;

    // constructor
    public QcController(AppDbContext db, IPublishEndpoint publish)
    {
        _db = db;
        _publish = publish;
    }

    // mendapatkan daftar item dalam batch
    [HttpGet("batches/{batchId:guid}")]
    public async Task<IActionResult> GetBatch(Guid batchId)
    {
        // get item berdasarkan batchId
        var items = await _db.StagingProducts
            .Where(x => x.BatchId == batchId)
            .OrderBy(x => x.RowNumber)
            .ToListAsync();

        return Ok(items);
    }

    // approve batch
    [HttpPost("batches/{batchId:guid}/approve")]
    public async Task<IActionResult> ApproveBatch(Guid batchId)
    {
        var items = await _db.StagingProducts
            .Where(x => x.BatchId == batchId && x.Status == "Pending")
            .ToListAsync();

        foreach (var i in items)
        {
            // publish ke production
            await _publish.Publish(
                new ProductUpsertMessage(i.BatchId, i.RowNumber, i.Sku, i.Name, i.Description, i.Price, i.Stock, i.IsActive),
                ctx => ctx.SetRoutingKey("production"));

            // update status
            i.Status = "Approve";
            i.ApprovedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new { batchId, approvedCount = items.Count });
    }

    // reject batch
    [HttpPost("batches/{batchId:guid}/reject")]
    public async Task<IActionResult> RejectBatch(Guid batchId)
    {
        var items = await _db.StagingProducts
            .Where(x => x.BatchId == batchId && x.Status == "Pending")
            .ToListAsync();

        // update status to reject
        foreach (var i in items)
            i.Status = "Reject";

        await _db.SaveChangesAsync();
        return Ok(new { batchId, rejectedCount = items.Count });
    }
}
