using Microsoft.EntityFrameworkCore.Migrations;

namespace Cheesos.Data.Migrations
{
    public partial class AddDiscountFieldToCoupon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Discount",
                table: "Coupon",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Coupon");
        }
    }
}
