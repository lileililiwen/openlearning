using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassGroupId",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassGroup_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassGroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassAssignment_ClassGroup_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "ClassGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassGroupId",
                table: "Enrollments",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssignment_ClassGroupId_UserId_Role",
                table: "ClassAssignment",
                columns: new[] { "ClassGroupId", "UserId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssignment_UserId_ClassGroupId",
                table: "ClassAssignment",
                columns: new[] { "UserId", "ClassGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroup_CourseId_StartsAt",
                table: "ClassGroup",
                columns: new[] { "CourseId", "StartsAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_ClassGroup_ClassGroupId",
                table: "Enrollments",
                column: "ClassGroupId",
                principalTable: "ClassGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_ClassGroup_ClassGroupId",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "ClassAssignment");

            migrationBuilder.DropTable(
                name: "ClassGroup");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ClassGroupId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ClassGroupId",
                table: "Enrollments");
        }
    }
}
