using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveBookingCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BookingClosesAt",
                table: "LiveSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BookingOpensAt",
                table: "LiveSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDeadline",
                table: "LiveSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "LiveSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBookingEnabled",
                table: "LiveSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LiveBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveBookings_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveBookings_LiveSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LiveSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveCalendarTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveCalendarTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveCalendarTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveWaitlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PromotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveWaitlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveWaitlists_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveWaitlists_LiveSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LiveSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveBookings_SessionId_StudentId",
                table: "LiveBookings",
                columns: new[] { "SessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveBookings_StudentId",
                table: "LiveBookings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveCalendarTokens_TokenHash",
                table: "LiveCalendarTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_LiveCalendarTokens_UserId",
                table: "LiveCalendarTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveWaitlists_SessionId_Position",
                table: "LiveWaitlists",
                columns: new[] { "SessionId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveWaitlists_SessionId_StudentId",
                table: "LiveWaitlists",
                columns: new[] { "SessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveWaitlists_StudentId",
                table: "LiveWaitlists",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveBookings");

            migrationBuilder.DropTable(
                name: "LiveCalendarTokens");

            migrationBuilder.DropTable(
                name: "LiveWaitlists");

            migrationBuilder.DropColumn(
                name: "BookingClosesAt",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "BookingOpensAt",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "CancellationDeadline",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "LiveSessions");

            migrationBuilder.DropColumn(
                name: "IsBookingEnabled",
                table: "LiveSessions");
        }
    }
}
