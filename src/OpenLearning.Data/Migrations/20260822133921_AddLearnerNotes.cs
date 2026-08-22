using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearnerNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    ContextType = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    MediaOffsetSeconds = table.Column<int>(type: "integer", nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerNotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerNotes_UserId",
                table: "LearnerNotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerNotes_UserId_ContextType_ContextId",
                table: "LearnerNotes",
                columns: new[] { "UserId", "ContextType", "ContextId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerNotes_UserId_CreatedAt",
                table: "LearnerNotes",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerNotes");
        }
    }
}
