using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExamIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrityAccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IncidentId = table.Column<int>(type: "integer", nullable: true),
                    SessionId = table.Column<int>(type: "integer", nullable: true),
                    ReviewerId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrityAccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrityIncidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    ExamId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    ContributingRules = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PolicyVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrityIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrityIncidents_ExamAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrityPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExamId = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RiskThreshold = table.Column<int>(type: "integer", nullable: false),
                    HeartbeatGapWeight = table.Column<int>(type: "integer", nullable: false),
                    VisibilityHiddenWeight = table.Column<int>(type: "integer", nullable: false),
                    TabSwitchWeight = table.Column<int>(type: "integer", nullable: false),
                    CopyAttemptWeight = table.Column<int>(type: "integer", nullable: false),
                    PasteAttemptWeight = table.Column<int>(type: "integer", nullable: false),
                    ConnectivityLossWeight = table.Column<int>(type: "integer", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrityPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrityPolicies_Exam_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegritySessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    Nonce = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Signature = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastSequence = table.Column<long>(type: "bigint", nullable: false),
                    LastEventAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegritySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegritySessions_ExamAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnerAccommodations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExamId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    AttemptId = table.Column<int>(type: "integer", nullable: true),
                    ExtraMinutes = table.Column<int>(type: "integer", nullable: false),
                    AllowedBreaks = table.Column<int>(type: "integer", nullable: false),
                    RelaxedVisibilityThreshold = table.Column<int>(type: "integer", nullable: false),
                    RelaxedCopyPasteThreshold = table.Column<int>(type: "integer", nullable: false),
                    GrantedById = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerAccommodations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerAccommodations_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearnerAccommodations_ExamAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LearnerAccommodations_Exam_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrityAppeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IncidentId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<string>(type: "text", nullable: true),
                    ReviewerNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrityAppeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrityAppeals_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IntegrityAppeals_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrityAppeals_IntegrityIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "IntegrityIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrityDispositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IncidentId = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrityDispositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrityDispositions_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IntegrityDispositions_IntegrityIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "IntegrityIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrityEvidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrityEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrityEvidence_IntegritySessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "IntegritySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityAccessLogs_AccessedAt",
                table: "IntegrityAccessLogs",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityAccessLogs_IncidentId",
                table: "IntegrityAccessLogs",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityAccessLogs_ReviewerId",
                table: "IntegrityAccessLogs",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityAppeals_IncidentId",
                table: "IntegrityAppeals",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityAppeals_ReviewerId",
                table: "IntegrityAppeals",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityAppeals_StudentId",
                table: "IntegrityAppeals",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityDispositions_IncidentId",
                table: "IntegrityDispositions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityDispositions_ReviewerId",
                table: "IntegrityDispositions",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityEvidence_AttemptId",
                table: "IntegrityEvidence",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityEvidence_ReceivedAt",
                table: "IntegrityEvidence",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityEvidence_SessionId_BatchId",
                table: "IntegrityEvidence",
                columns: new[] { "SessionId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityIncidents_AttemptId",
                table: "IntegrityIncidents",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityIncidents_CourseId",
                table: "IntegrityIncidents",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityIncidents_ExamId",
                table: "IntegrityIncidents",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityIncidents_Status",
                table: "IntegrityIncidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityIncidents_StudentId",
                table: "IntegrityIncidents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrityPolicies_ExamId_IsActive",
                table: "IntegrityPolicies",
                columns: new[] { "ExamId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegritySessions_AttemptId",
                table: "IntegritySessions",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegritySessions_AttemptId_Status",
                table: "IntegritySessions",
                columns: new[] { "AttemptId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerAccommodations_AttemptId",
                table: "LearnerAccommodations",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerAccommodations_ExamId_StudentId",
                table: "LearnerAccommodations",
                columns: new[] { "ExamId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerAccommodations_StudentId",
                table: "LearnerAccommodations",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrityAccessLogs");

            migrationBuilder.DropTable(
                name: "IntegrityAppeals");

            migrationBuilder.DropTable(
                name: "IntegrityDispositions");

            migrationBuilder.DropTable(
                name: "IntegrityEvidence");

            migrationBuilder.DropTable(
                name: "IntegrityPolicies");

            migrationBuilder.DropTable(
                name: "LearnerAccommodations");

            migrationBuilder.DropTable(
                name: "IntegrityIncidents");

            migrationBuilder.DropTable(
                name: "IntegritySessions");
        }
    }
}
