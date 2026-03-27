using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Art_BaBomb.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptFileSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PurchaseReceiptSizeBytes",
                table: "Items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReturnReceiptSizeBytes",
                table: "Items",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseReceiptSizeBytes",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReturnReceiptSizeBytes",
                table: "Items");
        }
    }
}
