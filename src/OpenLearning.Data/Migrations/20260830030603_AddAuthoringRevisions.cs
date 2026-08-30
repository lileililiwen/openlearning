using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthoringRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseRevision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LearningOutcomes = table.Column<string>(type: "text", nullable: false),
                    Prerequisites = table.Column<string>(type: "text", nullable: false),
                    ContentSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRevision", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseRevisionPointer",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DraftRevisionId = table.Column<int>(type: "integer", nullable: true),
                    ActiveRevisionId = table.Column<int>(type: "integer", nullable: true),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRevisionPointer", x => x.CourseId);
                });

            migrationBuilder.CreateTable(
                name: "RevisionAudit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    RevisionId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DetailJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevisionValidationResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    RevisionId = table.Column<int>(type: "integer", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionValidationResult", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseRevision_CourseId",
                table: "CourseRevision",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRevision_CourseId_State",
                table: "CourseRevision",
                columns: new[] { "CourseId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseRevisionPointer_ActiveRevisionId",
                table: "CourseRevisionPointer",
                column: "ActiveRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRevisionPointer_DraftRevisionId",
                table: "CourseRevisionPointer",
                column: "DraftRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionAudit_At",
                table: "RevisionAudit",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionAudit_CourseId",
                table: "RevisionAudit",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionValidationResult_CourseId_RevisionId",
                table: "RevisionValidationResult",
                columns: new[] { "CourseId", "RevisionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseRevision");

            migrationBuilder.DropTable(
                name: "CourseRevisionPointer");

            migrationBuilder.DropTable(
                name: "RevisionAudit");

            migrationBuilder.DropTable(
                name: "RevisionValidationResult");
        }
    }
}
