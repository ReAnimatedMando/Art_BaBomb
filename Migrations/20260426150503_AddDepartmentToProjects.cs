using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Art_BaBomb.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Department",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "Projects");
        }
    }
}
