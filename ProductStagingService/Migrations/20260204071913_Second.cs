using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductStagingService.Migrations
{
    /// <inheritdoc />
    public partial class Second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StagingProducts_Sku",
                table: "StagingProducts",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StagingProducts_Sku",
                table: "StagingProducts");
        }
    }
}
