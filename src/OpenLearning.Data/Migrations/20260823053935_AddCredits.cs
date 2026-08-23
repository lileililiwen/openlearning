using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RuleVersion = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditAwards_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GraduationPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinTotalCredits = table.Column<decimal>(type: "numeric", nullable: false),
                    CategoryMinimums = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequiredCourseIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraduationPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GraduationDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraduationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GraduationDecisions_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GraduationDecisions_GraduationPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "GraduationPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnerPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerPrograms_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearnerPrograms_GraduationPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "GraduationPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditAwards_StudentId",
                table: "CreditAwards",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditAwards_StudentId_SourceType_SourceId",
                table: "CreditAwards",
                columns: new[] { "StudentId", "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraduationDecisions_ProgramId",
                table: "GraduationDecisions",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_GraduationDecisions_StudentId_ProgramId",
                table: "GraduationDecisions",
                columns: new[] { "StudentId", "ProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_GraduationPrograms_Name_Version",
                table: "GraduationPrograms",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPrograms_ProgramId",
                table: "LearnerPrograms",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPrograms_StudentId_ProgramId",
                table: "LearnerPrograms",
                columns: new[] { "StudentId", "ProgramId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditAwards");

            migrationBuilder.DropTable(
                name: "GraduationDecisions");

            migrationBuilder.DropTable(
                name: "LearnerPrograms");

            migrationBuilder.DropTable(
                name: "GraduationPrograms");
        }
    }
}
