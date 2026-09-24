using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Common.Persistence.Migrations
{
    public partial class AddWorkOrderConcurrencyToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "work_orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "work_orders");
        }
    }
}
