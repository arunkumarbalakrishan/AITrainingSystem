using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITrainingSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoProgressNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalDurationSeconds",
                table: "VideoProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VideoProgresses_LessonId",
                table: "VideoProgresses",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_VideoProgresses_Lessons_LessonId",
                table: "VideoProgresses",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VideoProgresses_Lessons_LessonId",
                table: "VideoProgresses");

            migrationBuilder.DropIndex(
                name: "IX_VideoProgresses_LessonId",
                table: "VideoProgresses");

            migrationBuilder.DropColumn(
                name: "TotalDurationSeconds",
                table: "VideoProgresses");
        }
    }
}
