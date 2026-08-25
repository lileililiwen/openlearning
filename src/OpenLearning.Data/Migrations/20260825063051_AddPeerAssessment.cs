using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPeerAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeerAllocationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigId = table.Column<int>(type: "integer", nullable: false),
                    RunNumber = table.Column<int>(type: "integer", nullable: false),
                    ParticipantCount = table.Column<int>(type: "integer", nullable: false),
                    ReviewsEach = table.Column<int>(type: "integer", nullable: false),
                    ShortfallCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerAllocationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeerReviewAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigId = table.Column<int>(type: "integer", nullable: false),
                    AssessorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RevieweeSubmissionId = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerReviewAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeerReviewConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    ReviewsPerStudent = table.Column<int>(type: "integer", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    Strategy = table.Column<int>(type: "integer", nullable: false),
                    InstructorWeightPercent = table.Column<int>(type: "integer", nullable: false),
                    ReviewOpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResultsReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerReviewConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeerReviewResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ComputedScore = table.Column<int>(type: "integer", nullable: true),
                    Basis = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverrideScore = table.Column<int>(type: "integer", nullable: true),
                    OverrideBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    OverrideAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerReviewResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeerAllocationPairs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunId = table.Column<int>(type: "integer", nullable: false),
                    ConfigId = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RevieweeSubmissionId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerAllocationPairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeerAllocationPairs_PeerAllocationRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "PeerAllocationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeerAssessmentAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    PromptSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerAssessmentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeerAssessmentAnswers_PeerReviewAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "PeerReviewAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeerReviewRubricQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerReviewRubricQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeerReviewRubricQuestions_PeerReviewConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "PeerReviewConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeerAllocationPairs_ConfigId_ReviewerId_IsActive",
                table: "PeerAllocationPairs",
                columns: new[] { "ConfigId", "ReviewerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PeerAllocationPairs_RevieweeSubmissionId",
                table: "PeerAllocationPairs",
                column: "RevieweeSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerAllocationPairs_RunId_ReviewerId_RevieweeSubmissionId",
                table: "PeerAllocationPairs",
                columns: new[] { "RunId", "ReviewerId", "RevieweeSubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerAllocationRuns_ConfigId_RunNumber",
                table: "PeerAllocationRuns",
                columns: new[] { "ConfigId", "RunNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerAssessmentAnswers_AssessmentId_QuestionId",
                table: "PeerAssessmentAnswers",
                columns: new[] { "AssessmentId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviewAssessments_ConfigId_AssessorId_RevieweeSubmissio~",
                table: "PeerReviewAssessments",
                columns: new[] { "ConfigId", "AssessorId", "RevieweeSubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviewAssessments_RevieweeSubmissionId",
                table: "PeerReviewAssessments",
                column: "RevieweeSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviewConfigs_AssignmentId",
                table: "PeerReviewConfigs",
                column: "AssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviewConfigs_CourseId",
                table: "PeerReviewConfigs",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviewResults_ConfigId_StudentId",
                table: "PeerReviewResults",
                columns: new[] { "ConfigId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviewRubricQuestions_ConfigId_SortOrder",
                table: "PeerReviewRubricQuestions",
                columns: new[] { "ConfigId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeerAllocationPairs");

            migrationBuilder.DropTable(
                name: "PeerAssessmentAnswers");

            migrationBuilder.DropTable(
                name: "PeerReviewResults");

            migrationBuilder.DropTable(
                name: "PeerReviewRubricQuestions");

            migrationBuilder.DropTable(
                name: "PeerAllocationRuns");

            migrationBuilder.DropTable(
                name: "PeerReviewAssessments");

            migrationBuilder.DropTable(
                name: "PeerReviewConfigs");
        }
    }
}
