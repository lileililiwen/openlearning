using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentAggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefreshRunId = table.Column<long>(type: "bigint", nullable: false),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Completions = table.Column<int>(type: "integer", nullable: false),
                    AverageScore = table.Column<double>(type: "double precision", nullable: false),
                    PassRate = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CohortRetentionAggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefreshRunId = table.Column<long>(type: "bigint", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    ClassGroupId = table.Column<int>(type: "integer", nullable: false),
                    PeriodIndex = table.Column<int>(type: "integer", nullable: false),
                    Retained = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortRetentionAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseFunnelAggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefreshRunId = table.Column<long>(type: "bigint", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Eligible = table.Column<int>(type: "integer", nullable: false),
                    Enrolled = table.Column<int>(type: "integer", nullable: false),
                    Started = table.Column<int>(type: "integer", nullable: false),
                    Completed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseFunnelAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EngagementAggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefreshRunId = table.Column<long>(type: "bigint", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ActiveLearners = table.Column<int>(type: "integer", nullable: false),
                    ActiveSeconds = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngagementAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequesterId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FiltersJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    LessonId = table.Column<int>(type: "integer", nullable: true),
                    AssessmentId = table.Column<int>(type: "integer", nullable: true),
                    ClassGroupId = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    PropertiesJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ValidationOutcome = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AggregateDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    CohortThreshold = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkloadAggregates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefreshRunId = table.Column<long>(type: "bigint", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TeachingHours = table.Column<double>(type: "double precision", nullable: false),
                    GradingWorkload = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkloadAggregates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAggregates_RefreshRunId_AssessmentId_Date",
                table: "AssessmentAggregates",
                columns: new[] { "RefreshRunId", "AssessmentId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CohortRetentionAggregates_RefreshRunId_CourseId_ClassGroupId",
                table: "CohortRetentionAggregates",
                columns: new[] { "RefreshRunId", "CourseId", "ClassGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseFunnelAggregates_RefreshRunId_CourseId_Date",
                table: "CourseFunnelAggregates",
                columns: new[] { "RefreshRunId", "CourseId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_EngagementAggregates_RefreshRunId_CourseId_Date",
                table: "EngagementAggregates",
                columns: new[] { "RefreshRunId", "CourseId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportAudits_RequesterId_ExportedAt",
                table: "ExportAudits",
                columns: new[] { "RequesterId", "ExportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvents_CourseId_OccurredAt",
                table: "LearningEvents",
                columns: new[] { "CourseId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvents_EventId",
                table: "LearningEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvents_EventType_OccurredAt",
                table: "LearningEvents",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvents_ReceivedAt",
                table: "LearningEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshRuns_Scope_Status",
                table: "RefreshRuns",
                columns: new[] { "Scope", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionPolicies_Key",
                table: "RetentionPolicies",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadAggregates_RefreshRunId_CourseId_Date",
                table: "WorkloadAggregates",
                columns: new[] { "RefreshRunId", "CourseId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentAggregates");

            migrationBuilder.DropTable(
                name: "CohortRetentionAggregates");

            migrationBuilder.DropTable(
                name: "CourseFunnelAggregates");

            migrationBuilder.DropTable(
                name: "EngagementAggregates");

            migrationBuilder.DropTable(
                name: "ExportAudits");

            migrationBuilder.DropTable(
                name: "LearningEvents");

            migrationBuilder.DropTable(
                name: "RefreshRuns");

            migrationBuilder.DropTable(
                name: "RetentionPolicies");

            migrationBuilder.DropTable(
                name: "WorkloadAggregates");
        }
    }
}
