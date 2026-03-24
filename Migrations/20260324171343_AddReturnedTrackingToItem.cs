using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Art_BaBomb.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnedTrackingToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnReceiptFileName",
                table: "Items",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReturnReceiptPath",
                table: "Items",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "Items",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnReceiptFileName",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReturnReceiptPath",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "Items");
        }
    }
}
