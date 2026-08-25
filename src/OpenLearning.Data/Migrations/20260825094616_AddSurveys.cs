using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Surveys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    AllowLiveResults = table.Column<bool>(type: "boolean", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TokenSalt = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurveyId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurveyId = table.Column<int>(type: "integer", nullable: false),
                    RespondentUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    RespondentToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyResponses_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestionOptions_SurveyQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "SurveyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResponseId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    OptionId = table.Column<int>(type: "integer", nullable: true),
                    RatingValue = table.Column<int>(type: "integer", nullable: true),
                    TextValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyAnswers_SurveyResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "SurveyResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswers_QuestionId",
                table: "SurveyAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyAnswers_ResponseId_QuestionId",
                table: "SurveyAnswers",
                columns: new[] { "ResponseId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestionOptions_QuestionId_SortOrder",
                table: "SurveyQuestionOptions",
                columns: new[] { "QuestionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_SurveyId_SortOrder",
                table: "SurveyQuestions",
                columns: new[] { "SurveyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyId_RespondentToken",
                table: "SurveyResponses",
                columns: new[] { "SurveyId", "RespondentToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_Scope_CourseId",
                table: "Surveys",
                columns: new[] { "Scope", "CourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurveyAnswers");

            migrationBuilder.DropTable(
                name: "SurveyQuestionOptions");

            migrationBuilder.DropTable(
                name: "SurveyResponses");

            migrationBuilder.DropTable(
                name: "SurveyQuestions");

            migrationBuilder.DropTable(
                name: "Surveys");
        }
    }
}
