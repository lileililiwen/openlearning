using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations;

/// <inheritdoc />
[Migration("20260823054500_ExtendCredits")]
[DbContext(typeof(ApplicationDbContext))]
public sealed class ExtendCredits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CreditExpiryDays",
            table: "GraduationPrograms",
            type: "integer",
            nullable: true);

        migrationBuilder.DropIndex(
            name: "IX_LearnerPrograms_StudentId_ProgramId",
            table: "LearnerPrograms");

        migrationBuilder.CreateIndex(
            name: "IX_LearnerPrograms_StudentId",
            table: "LearnerPrograms",
            column: "StudentId",
            unique: true);

        migrationBuilder.CreateTable(
            name: "CourseCreditRules",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CourseId = table.Column<int>(type: "integer", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_CourseCreditRules", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_CourseCreditRules_CourseId_IsActive",
            table: "CourseCreditRules",
            columns: new[] { "CourseId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_CourseCreditRules_CourseId_Version",
            table: "CourseCreditRules",
            columns: new[] { "CourseId", "Version" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CourseCreditRules");
        migrationBuilder.DropIndex(name: "IX_LearnerPrograms_StudentId", table: "LearnerPrograms");
        migrationBuilder.DropColumn(name: "CreditExpiryDays", table: "GraduationPrograms");
        migrationBuilder.CreateIndex(
            name: "IX_LearnerPrograms_StudentId_ProgramId",
            table: "LearnerPrograms",
            columns: new[] { "StudentId", "ProgramId" },
            unique: true);
    }
}
