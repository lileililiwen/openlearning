using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScorm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScormPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScormVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntryPoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PackagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScormPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScormPackages_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScormRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnrollmentId = table.Column<int>(type: "integer", nullable: false),
                    ScormPackageId = table.Column<int>(type: "integer", nullable: false),
                    LessonLocation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SuspendData = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    LessonStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScoreRaw = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SessionTime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScormRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScormRecords_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScormRecords_ScormPackages_ScormPackageId",
                        column: x => x.ScormPackageId,
                        principalTable: "ScormPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScormPackages_LessonId",
                table: "ScormPackages",
                column: "LessonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScormRecords_EnrollmentId_ScormPackageId",
                table: "ScormRecords",
                columns: new[] { "EnrollmentId", "ScormPackageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScormRecords_ScormPackageId",
                table: "ScormRecords",
                column: "ScormPackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScormRecords");

            migrationBuilder.DropTable(
                name: "ScormPackages");
        }
    }
}
