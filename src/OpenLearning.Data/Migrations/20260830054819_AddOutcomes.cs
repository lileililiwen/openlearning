using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseOutcome",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MasteryThreshold = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOutcome", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasteryResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    OutcomeId = table.Column<int>(type: "integer", nullable: false),
                    LearnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    SourceJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasteryResult", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutcomeActivityMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    OutcomeId = table.Column<int>(type: "integer", nullable: false),
                    ActivityId = table.Column<int>(type: "integer", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeActivityMapping", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOutcome_CourseId",
                table: "CourseOutcome",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOutcome_CourseId_OwnerId",
                table: "CourseOutcome",
                columns: new[] { "CourseId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_MasteryResult_CourseId_LearnerId",
                table: "MasteryResult",
                columns: new[] { "CourseId", "LearnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_MasteryResult_CourseId_OutcomeId_LearnerId",
                table: "MasteryResult",
                columns: new[] { "CourseId", "OutcomeId", "LearnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeActivityMapping_CourseId_OutcomeId",
                table: "OutcomeActivityMapping",
                columns: new[] { "CourseId", "OutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeActivityMapping_OutcomeId_ActivityId",
                table: "OutcomeActivityMapping",
                columns: new[] { "OutcomeId", "ActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseOutcome");

            migrationBuilder.DropTable(
                name: "MasteryResult");

            migrationBuilder.DropTable(
                name: "OutcomeActivityMapping");
        }
    }
}
