using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "ReviewComment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "QuestionReply",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Question",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "PostReply",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Post",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "Courses",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    ContentId = table.Column<int>(type: "integer", nullable: false),
                    ReportedById = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Resolution = table.Column<int>(type: "integer", nullable: false),
                    ResolvedById = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ContentType_ContentId_Resolution",
                table: "ContentReports",
                columns: new[] { "ContentType", "ContentId", "Resolution" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_Resolution",
                table: "ContentReports",
                column: "Resolution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentReports");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "ReviewComment");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "QuestionReply");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "PostReply");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Post");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "Courses");
        }
    }
}
