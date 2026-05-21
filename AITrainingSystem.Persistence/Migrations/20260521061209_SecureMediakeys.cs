using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITrainingSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecureMediakeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoUrl",
                table: "Lessons",
                newName: "VideoKey");

            migrationBuilder.RenameColumn(
                name: "PdfUrl",
                table: "Lessons",
                newName: "PdfKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoKey",
                table: "Lessons",
                newName: "VideoUrl");

            migrationBuilder.RenameColumn(
                name: "PdfKey",
                table: "Lessons",
                newName: "PdfUrl");
        }
    }
}
