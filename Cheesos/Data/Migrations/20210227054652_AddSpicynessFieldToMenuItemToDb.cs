using Microsoft.EntityFrameworkCore.Migrations;

namespace Cheesos.Data.Migrations
{
    public partial class AddSpicynessFieldToMenuItemToDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Spicyness",
                table: "MenuItem",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Spicyness",
                table: "MenuItem");
        }
    }
}
