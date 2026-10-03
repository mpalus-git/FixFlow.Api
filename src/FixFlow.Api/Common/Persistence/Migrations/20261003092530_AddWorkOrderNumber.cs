using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Common.Persistence.Migrations
{
    public partial class AddWorkOrderNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "number",
                table: "work_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "work_order_number_counters",
                columns: table => new
                {
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_order_number_counters", x => x.year);
                });

            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT
                        id,
                        extract(year FROM created_at AT TIME ZONE 'Europe/Warsaw')::integer AS year,
                        row_number() OVER (
                            PARTITION BY extract(year FROM created_at AT TIME ZONE 'Europe/Warsaw')
                            ORDER BY created_at, id)::integer AS sequence
                    FROM work_orders)
                UPDATE work_orders
                SET number = 'ZL/' || numbered.year || '/' || CASE WHEN numbered.sequence < 10000 THEN lpad(numbered.sequence::text, 4, '0') ELSE numbered.sequence::text END
                FROM numbered
                WHERE work_orders.id = numbered.id;
                """);

            migrationBuilder.Sql("""
                INSERT INTO work_order_number_counters (year, last_number)
                SELECT extract(year FROM created_at AT TIME ZONE 'Europe/Warsaw')::integer, count(*)
                FROM work_orders
                GROUP BY 1;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_number",
                table: "work_orders",
                column: "number",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_order_number_counters");

            migrationBuilder.DropIndex(
                name: "ix_work_orders_number",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "number",
                table: "work_orders");
        }
    }
}
