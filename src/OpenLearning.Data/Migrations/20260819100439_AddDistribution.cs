using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffiliateLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistributorUserId = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateLink_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attribution",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    AffiliateClickId = table.Column<int>(type: "integer", nullable: false),
                    DistributorUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attribution", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommissionEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistributorUserId = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PayoutRequestId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DistributorProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributorProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributorProfile_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistributorSettlementStatement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistributorUserId = table.Column<string>(type: "text", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributorSettlementStatement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayoutRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistributorUserId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutRequest_AspNetUsers_DistributorUserId",
                        column: x => x.DistributorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateClick",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AffiliateLinkId = table.Column<int>(type: "integer", nullable: false),
                    AnonymousId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    HashedIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ClickedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateClick", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateClick_AffiliateLink_AffiliateLinkId",
                        column: x => x.AffiliateLinkId,
                        principalTable: "AffiliateLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateClick_AffiliateLinkId_ClickedAt",
                table: "AffiliateClick",
                columns: new[] { "AffiliateLinkId", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateLink_CourseId",
                table: "AffiliateLink",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateLink_DistributorUserId_CourseId",
                table: "AffiliateLink",
                columns: new[] { "DistributorUserId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateLink_Slug",
                table: "AffiliateLink",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attribution_DistributorUserId_CreatedAt",
                table: "Attribution",
                columns: new[] { "DistributorUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attribution_OrderId",
                table: "Attribution",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionEntry_DistributorUserId_Status",
                table: "CommissionEntry",
                columns: new[] { "DistributorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionEntry_OrderId_Status",
                table: "CommissionEntry",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DistributorProfile_UserId",
                table: "DistributorProfile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributorSettlementStatement_DistributorUserId_PeriodStart",
                table: "DistributorSettlementStatement",
                columns: new[] { "DistributorUserId", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRequest_DistributorUserId_Status",
                table: "PayoutRequest",
                columns: new[] { "DistributorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRequest_Status",
                table: "PayoutRequest",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffiliateClick");

            migrationBuilder.DropTable(
                name: "Attribution");

            migrationBuilder.DropTable(
                name: "CommissionEntry");

            migrationBuilder.DropTable(
                name: "DistributorProfile");

            migrationBuilder.DropTable(
                name: "DistributorSettlementStatement");

            migrationBuilder.DropTable(
                name: "PayoutRequest");

            migrationBuilder.DropTable(
                name: "AffiliateLink");
        }
    }
}
