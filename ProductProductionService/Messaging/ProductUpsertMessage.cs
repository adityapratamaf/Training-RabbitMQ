namespace Messaging
{
    public record ProductUpsertMessage(
        Guid BatchId,
        int RowNumber,
        string Sku,
        string Name,
        string Description,
        decimal Price,
        int Stock,
        bool IsActive
    );
}