using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Art_BaBomb.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Receipts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Receipts");
        }
    }
}
