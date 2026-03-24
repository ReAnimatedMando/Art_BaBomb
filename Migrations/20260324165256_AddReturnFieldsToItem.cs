using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Art_BaBomb.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnFieldsToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReturnRequired",
                table: "Items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReturned",
                table: "Items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnByDate",
                table: "Items",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnLocation",
                table: "Items",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReturnNotes",
                table: "Items",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReturnRequired",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsReturned",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReturnByDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReturnLocation",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReturnNotes",
                table: "Items");
        }
    }
}
