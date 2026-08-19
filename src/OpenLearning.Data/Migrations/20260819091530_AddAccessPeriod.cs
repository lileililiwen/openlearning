using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                table: "Enrollments");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessExpiresAt",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "Enrollments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedReason",
                table: "Enrollments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultAccessDays",
                table: "Courses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "AccessExpiresAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "RevokedReason",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "DefaultAccessDays",
                table: "Courses");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);
        }
    }
}
