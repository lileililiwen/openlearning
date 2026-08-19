using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAsyncIO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsyncIOJob",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResultFileKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorFileKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsyncIOJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AsyncIORowError",
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
                    table.PrimaryKey("PK_AsyncIORowError", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsyncIOJob_Kind_Status",
                table: "AsyncIOJob",
                columns: new[] { "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AsyncIOJob_UserId_CreatedAt",
                table: "AsyncIOJob",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AsyncIORowError_JobId",
                table: "AsyncIORowError",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsyncIOJob");

            migrationBuilder.DropTable(
                name: "AsyncIORowError");
        }
    }
}
