using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGradebook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradebookConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradebookSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    Aggregate = table.Column<int>(type: "integer", nullable: true),
                    BasisJson = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradebookItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradebookItems_GradebookConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "GradebookConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradebookAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    IsExcusal = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideScore = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradebookAdjustments_GradebookItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "GradebookItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradebookAdjustments_ItemId",
                table: "GradebookAdjustments",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GradebookItems_ConfigId",
                table: "GradebookItems",
                column: "ConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradebookAdjustments");

            migrationBuilder.DropTable(
                name: "GradebookSnapshots");

            migrationBuilder.DropTable(
                name: "GradebookItems");

            migrationBuilder.DropTable(
                name: "GradebookConfigs");
        }
    }
}
