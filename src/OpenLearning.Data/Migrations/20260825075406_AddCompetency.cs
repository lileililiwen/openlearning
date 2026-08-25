using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompetencyFrameworks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FrameworkId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyNodes_CompetencyFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "CompetencyFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FrameworkScaleLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FrameworkId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkScaleLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrameworkScaleLevels_CompetencyFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "CompetencyFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetencyId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    AssignmentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityMappings_CompetencyNodes_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "CompetencyNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyEvidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetencyId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LevelSortOrder = table.Column<int>(type: "integer", nullable: true),
                    FrameworkVersion = table.Column<int>(type: "integer", nullable: false),
                    CompetencyTitleSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyEvidence_CompetencyNodes_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "CompetencyNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMappings_AssignmentId",
                table: "ActivityMappings",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMappings_CompetencyId",
                table: "ActivityMappings",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityMappings_CourseId",
                table: "ActivityMappings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyEvidence_CompetencyId_SourceKey",
                table: "CompetencyEvidence",
                columns: new[] { "CompetencyId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyEvidence_UserId_Status",
                table: "CompetencyEvidence",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyFrameworks_IsArchived",
                table: "CompetencyFrameworks",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyNodes_FrameworkId_SortOrder",
                table: "CompetencyNodes",
                columns: new[] { "FrameworkId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyNodes_ParentId",
                table: "CompetencyNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkScaleLevels_FrameworkId_SortOrder",
                table: "FrameworkScaleLevels",
                columns: new[] { "FrameworkId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityMappings");

            migrationBuilder.DropTable(
                name: "CompetencyEvidence");

            migrationBuilder.DropTable(
                name: "FrameworkScaleLevels");

            migrationBuilder.DropTable(
                name: "CompetencyNodes");

            migrationBuilder.DropTable(
                name: "CompetencyFrameworks");
        }
    }
}
