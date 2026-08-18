using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LearningOutcomes",
                table: "Courses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Courses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "Courses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LearningOutcomes",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "Courses");
        }
    }
}
