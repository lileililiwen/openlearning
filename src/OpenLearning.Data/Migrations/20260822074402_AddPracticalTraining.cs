using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticalTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticalCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApprovedHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalCompletions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PracticalEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    EvaluatorKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PracticalEvidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    StoredFileId = table.Column<int>(type: "integer", nullable: false),
                    LearnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalEvidence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PracticalHosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalHosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PracticalIncidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PracticalPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    MinimumHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PracticalPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PracticalProgramId = table.Column<int>(type: "integer", nullable: false),
                    HostOrganizationId = table.Column<int>(type: "integer", nullable: false),
                    LearnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CoordinatorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SupervisorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupervisorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalPlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticalPlacements_PracticalHosts_HostOrganizationId",
                        column: x => x.HostOrganizationId,
                        principalTable: "PracticalHosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticalPlacements_PracticalPrograms_PracticalProgramId",
                        column: x => x.PracticalProgramId,
                        principalTable: "PracticalPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PracticalProgramCompetencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PracticalProgramId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalProgramCompetencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticalProgramCompetencies_PracticalPrograms_PracticalPro~",
                        column: x => x.PracticalProgramId,
                        principalTable: "PracticalPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticalHourLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    AmendsLogId = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalHourLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticalHourLogs_PracticalHourLogs_AmendsLogId",
                        column: x => x.AmendsLogId,
                        principalTable: "PracticalHourLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticalHourLogs_PracticalPlacements_PlacementId",
                        column: x => x.PlacementId,
                        principalTable: "PracticalPlacements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticalSupervisorInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalSupervisorInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticalSupervisorInvitations_PracticalPlacements_Placemen~",
                        column: x => x.PlacementId,
                        principalTable: "PracticalPlacements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticalPlacementCompetencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlacementId = table.Column<int>(type: "integer", nullable: false),
                    ProgramCompetencyId = table.Column<int>(type: "integer", nullable: false),
                    IsAchieved = table.Column<bool>(type: "boolean", nullable: false),
                    Evaluation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalPlacementCompetencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticalPlacementCompetencies_PracticalPlacements_Placemen~",
                        column: x => x.PlacementId,
                        principalTable: "PracticalPlacements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticalPlacementCompetencies_PracticalProgramCompetencies~",
                        column: x => x.ProgramCompetencyId,
                        principalTable: "PracticalProgramCompetencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticalCompletions_ConfirmationKey",
                table: "PracticalCompletions",
                column: "ConfirmationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticalCompletions_PlacementId",
                table: "PracticalCompletions",
                column: "PlacementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticalEvaluations_PlacementId_EvaluatorKind",
                table: "PracticalEvaluations",
                columns: new[] { "PlacementId", "EvaluatorKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticalHourLogs_AmendsLogId",
                table: "PracticalHourLogs",
                column: "AmendsLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalHourLogs_PlacementId_StartedAt_EndedAt",
                table: "PracticalHourLogs",
                columns: new[] { "PlacementId", "StartedAt", "EndedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticalPlacementCompetencies_PlacementId_ProgramCompetenc~",
                table: "PracticalPlacementCompetencies",
                columns: new[] { "PlacementId", "ProgramCompetencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticalPlacementCompetencies_ProgramCompetencyId",
                table: "PracticalPlacementCompetencies",
                column: "ProgramCompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalPlacements_HostOrganizationId",
                table: "PracticalPlacements",
                column: "HostOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalPlacements_LearnerId",
                table: "PracticalPlacements",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalPlacements_PracticalProgramId",
                table: "PracticalPlacements",
                column: "PracticalProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalProgramCompetencies_PracticalProgramId",
                table: "PracticalProgramCompetencies",
                column: "PracticalProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalPrograms_Title_Version",
                table: "PracticalPrograms",
                columns: new[] { "Title", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticalSupervisorInvitations_PlacementId",
                table: "PracticalSupervisorInvitations",
                column: "PlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalSupervisorInvitations_TokenHash",
                table: "PracticalSupervisorInvitations",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticalCompletions");

            migrationBuilder.DropTable(
                name: "PracticalEvaluations");

            migrationBuilder.DropTable(
                name: "PracticalEvidence");

            migrationBuilder.DropTable(
                name: "PracticalHourLogs");

            migrationBuilder.DropTable(
                name: "PracticalIncidents");

            migrationBuilder.DropTable(
                name: "PracticalPlacementCompetencies");

            migrationBuilder.DropTable(
                name: "PracticalSupervisorInvitations");

            migrationBuilder.DropTable(
                name: "PracticalProgramCompetencies");

            migrationBuilder.DropTable(
                name: "PracticalPlacements");

            migrationBuilder.DropTable(
                name: "PracticalHosts");

            migrationBuilder.DropTable(
                name: "PracticalPrograms");
        }
    }
}
