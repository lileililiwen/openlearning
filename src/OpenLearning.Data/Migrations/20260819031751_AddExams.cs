using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "QuizId",
                table: "Questions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ExamId",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Exam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PassPercent = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exam_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exam_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExamId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    Percent = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    ScreenSwitchCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttempt_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAttempt_Exam_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAttemptAnswer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    AnswerOptionId = table.Column<int>(type: "integer", nullable: true),
                    SelectedOptionIds = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TextAnswer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FileAnswerUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    IsGraded = table.Column<bool>(type: "boolean", nullable: false),
                    GradedScore = table.Column<int>(type: "integer", nullable: true),
                    GradingFeedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttemptAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttemptAnswer_AnswerOptions_AnswerOptionId",
                        column: x => x.AnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExamAttemptAnswer_ExamAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAttemptAnswer_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamId_OrderIndex",
                table: "Questions",
                columns: new[] { "ExamId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Exam_AuthorId_CourseId",
                table: "Exam",
                columns: new[] { "AuthorId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Exam_CourseId",
                table: "Exam",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempt_ExamId_StudentId_Status",
                table: "ExamAttempt",
                columns: new[] { "ExamId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempt_StudentId_ExamId_StartedAt",
                table: "ExamAttempt",
                columns: new[] { "StudentId", "ExamId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptAnswer_AnswerOptionId",
                table: "ExamAttemptAnswer",
                column: "AnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptAnswer_AttemptId",
                table: "ExamAttemptAnswer",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttemptAnswer_QuestionId",
                table: "ExamAttemptAnswer",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Exam_ExamId",
                table: "Questions",
                column: "ExamId",
                principalTable: "Exam",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Exam_ExamId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "ExamAttemptAnswer");

            migrationBuilder.DropTable(
                name: "ExamAttempt");

            migrationBuilder.DropTable(
                name: "Exam");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ExamId_OrderIndex",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ExamId",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "QuizId",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
