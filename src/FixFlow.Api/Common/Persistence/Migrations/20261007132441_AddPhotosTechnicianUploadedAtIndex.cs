using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Common.Persistence.Migrations
{
    public partial class AddPhotosTechnicianUploadedAtIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_photos_technician_id",
                table: "photos");

            migrationBuilder.CreateIndex(
                name: "ix_photos_technician_id_uploaded_at",
                table: "photos",
                columns: new[] { "technician_id", "uploaded_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_photos_technician_id_uploaded_at",
                table: "photos");

            migrationBuilder.CreateIndex(
                name: "ix_photos_technician_id",
                table: "photos",
                column: "technician_id");
        }
    }
}
