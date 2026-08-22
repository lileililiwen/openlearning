using System;

// Gamification rewards remain auditable and source-idempotent — corrections are additive.
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGamification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GamificationBadges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CriteriaVersion = table.Column<int>(type: "integer", nullable: false),
                    RequiredPoints = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationBadges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamificationChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetPoints = table.Column<int>(type: "integer", nullable: false),
                    ScopeKind = table.Column<int>(type: "integer", nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamificationLeaderboardModeration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ScopeKind = table.Column<int>(type: "integer", nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationLeaderboardModeration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamificationLeaderboardPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayAlias = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationLeaderboardPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamificationPointRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    DailyCap = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationPointRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamificationBadgeAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BadgeDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    BadgeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CriteriaVersion = table.Column<int>(type: "integer", nullable: false),
                    Evidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationBadgeAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamificationBadgeAwards_GamificationBadges_BadgeDefinitionId",
                        column: x => x.BadgeDefinitionId,
                        principalTable: "GamificationBadges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GamificationPointEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PointRuleId = table.Column<int>(type: "integer", nullable: false),
                    RuleVersion = table.Column<int>(type: "integer", nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestedPoints = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    WasCapped = table.Column<bool>(type: "boolean", nullable: false),
                    CorrectsEntryId = table.Column<int>(type: "integer", nullable: true),
                    ScopeKind = table.Column<int>(type: "integer", nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamificationPointEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamificationPointEntries_GamificationPointEntries_CorrectsE~",
                        column: x => x.CorrectsEntryId,
                        principalTable: "GamificationPointEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GamificationPointEntries_GamificationPointRules_PointRuleId",
                        column: x => x.PointRuleId,
                        principalTable: "GamificationPointRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GamificationBadgeAwards_BadgeDefinitionId",
                table: "GamificationBadgeAwards",
                column: "BadgeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GamificationBadgeAwards_BadgeKey_UserId",
                table: "GamificationBadgeAwards",
                columns: new[] { "BadgeKey", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamificationBadges_Key_CriteriaVersion",
                table: "GamificationBadges",
                columns: new[] { "Key", "CriteriaVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamificationChallenges_ScopeKind_ScopeId_StartsAt_EndsAt",
                table: "GamificationChallenges",
                columns: new[] { "ScopeKind", "ScopeId", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GamificationLeaderboardModeration_UserId_ScopeKind_ScopeId",
                table: "GamificationLeaderboardModeration",
                columns: new[] { "UserId", "ScopeKind", "ScopeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamificationLeaderboardPreferences_UserId",
                table: "GamificationLeaderboardPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamificationPointEntries_CorrectsEntryId",
                table: "GamificationPointEntries",
                column: "CorrectsEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_GamificationPointEntries_PointRuleId",
                table: "GamificationPointEntries",
                column: "PointRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_GamificationPointEntries_ScopeKind_ScopeId_CreatedAt",
                table: "GamificationPointEntries",
                columns: new[] { "ScopeKind", "ScopeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GamificationPointEntries_SourceKey",
                table: "GamificationPointEntries",
                column: "SourceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamificationPointEntries_UserId_CreatedAt",
                table: "GamificationPointEntries",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GamificationPointRules_EventType_Version",
                table: "GamificationPointRules",
                columns: new[] { "EventType", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GamificationBadgeAwards");

            migrationBuilder.DropTable(
                name: "GamificationChallenges");

            migrationBuilder.DropTable(
                name: "GamificationLeaderboardModeration");

            migrationBuilder.DropTable(
                name: "GamificationLeaderboardPreferences");

            migrationBuilder.DropTable(
                name: "GamificationPointEntries");

            migrationBuilder.DropTable(
                name: "GamificationBadges");

            migrationBuilder.DropTable(
                name: "GamificationPointRules");
        }
    }
}
