namespace ProductStagingService.Models
{
    public class StagingProduct
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BatchId { get; set; }
        public int RowNumber { get; set; }

        public string Sku { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAtUtc { get; set; }
    }
}
