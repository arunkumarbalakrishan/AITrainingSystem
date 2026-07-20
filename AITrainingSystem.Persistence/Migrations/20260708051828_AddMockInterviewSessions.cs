using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITrainingSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMockInterviewSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockInterviewSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseTopic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfigSettings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<int>(type: "int", nullable: false),
                    CommunicationScore = table.Column<int>(type: "int", nullable: false),
                    TechnicalScore = table.Column<int>(type: "int", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    GrammarScore = table.Column<int>(type: "int", nullable: false),
                    EyeContactPercentage = table.Column<int>(type: "int", nullable: false),
                    BodyLanguageScore = table.Column<int>(type: "int", nullable: false),
                    SpeechAnalyticsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BehavioralAnalyticsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionByQuestionLogsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoReplayUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockInterviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockInterviewSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockInterviewSessions_UserId",
                table: "MockInterviewSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockInterviewSessions");
        }
    }
}
