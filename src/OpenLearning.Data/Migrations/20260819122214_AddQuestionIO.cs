using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionIO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "Questions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeTag",
                table: "Questions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RowId",
                table: "Questions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "AsyncIOJob",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuestionImportJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    QuizId = table.Column<int>(type: "integer", nullable: true),
                    IsBank = table.Column<bool>(type: "boolean", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    FileKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AsyncIOJobId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorFileKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionRowErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    RowIndex = table.Column<int>(type: "integer", nullable: false),
                    Field = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionRowErrors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_IsBank_RowId",
                table: "Questions",
                columns: new[] { "IsBank", "RowId" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizId_RowId",
                table: "Questions",
                columns: new[] { "QuizId", "RowId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionImportJobs_AsyncIOJobId",
                table: "QuestionImportJobs",
                column: "AsyncIOJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionImportJobs_UserId_CreatedAt",
                table: "QuestionImportJobs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionRowErrors_JobId",
                table: "QuestionRowErrors",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionImportJobs");

            migrationBuilder.DropTable(
                name: "QuestionRowErrors");

            migrationBuilder.DropIndex(
                name: "IX_Questions_IsBank_RowId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_QuizId_RowId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "KnowledgeTag",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "AsyncIOJob");
        }
    }
}
