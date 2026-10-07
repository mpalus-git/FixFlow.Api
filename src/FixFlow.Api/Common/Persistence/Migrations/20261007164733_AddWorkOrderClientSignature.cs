using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Common.Persistence.Migrations
{
    public partial class AddWorkOrderClientSignature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_signature_photo_id",
                table: "work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_client_signature_photo_id",
                table: "work_orders",
                column: "client_signature_photo_id");

            migrationBuilder.AddForeignKey(
                name: "fk_work_orders_photos_client_signature_photo_id",
                table: "work_orders",
                column: "client_signature_photo_id",
                principalTable: "photos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_work_orders_photos_client_signature_photo_id",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "ix_work_orders_client_signature_photo_id",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "client_signature_photo_id",
                table: "work_orders");
        }
    }
}
