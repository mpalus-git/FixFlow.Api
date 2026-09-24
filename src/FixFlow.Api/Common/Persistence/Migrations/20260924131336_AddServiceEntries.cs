using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Common.Persistence.Migrations
{
    public partial class AddServiceEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technician_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_correction = table.Column<bool>(type: "boolean", nullable: false),
                    photo_urls = table.Column<string[]>(type: "text[]", nullable: false),
                    work_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    work_finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_entries_users_technician_id",
                        column: x => x.technician_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_entries_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_entry_parts",
                columns: table => new
                {
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_entry_parts", x => new { x.service_entry_id, x.part_id });
                    table.CheckConstraint("ck_service_entry_parts_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_service_entry_parts_parts_part_id",
                        column: x => x.part_id,
                        principalTable: "parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_entry_parts_service_entries_service_entry_id",
                        column: x => x.service_entry_id,
                        principalTable: "service_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_entries_technician_id",
                table: "service_entries",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_entries_work_order_id",
                table: "service_entries",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_entry_parts_part_id",
                table: "service_entry_parts",
                column: "part_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_entry_parts");

            migrationBuilder.DropTable(
                name: "service_entries");
        }
    }
}
