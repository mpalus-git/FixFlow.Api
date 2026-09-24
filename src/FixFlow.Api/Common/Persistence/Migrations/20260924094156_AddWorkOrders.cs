using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Common.Persistence.Migrations
{
    public partial class AddWorkOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    technician_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_orders_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_work_orders_users_technician_id",
                        column: x => x.technician_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_device_id",
                table: "work_orders",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_technician_id",
                table: "work_orders",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_technician_id_in_progress",
                table: "work_orders",
                column: "technician_id",
                unique: true,
                filter: "status = 'InProgress'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_orders");
        }
    }
}
