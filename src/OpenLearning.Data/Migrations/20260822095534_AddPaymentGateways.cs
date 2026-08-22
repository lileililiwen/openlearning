using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentGateways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentAttempt",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProviderIntentId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SucceededAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentIntents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOutbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliationIssue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Retryable = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliationIssue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRefund",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentIntentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProviderRefundId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefund", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderEvent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SignatureValid = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderEvent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempt_PaymentIntentId",
                table: "PaymentAttempt",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_OrderId",
                table: "PaymentIntents",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentIntents_Provider_ProviderIntentId",
                table: "PaymentIntents",
                columns: new[] { "Provider", "ProviderIntentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOutbox_PaymentIntentId_Kind",
                table: "PaymentOutbox",
                columns: new[] { "PaymentIntentId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliationIssue_PaymentIntentId",
                table: "PaymentReconciliationIssue",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefund_PaymentIntentId",
                table: "PaymentRefund",
                column: "PaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderEvent_Provider_ProviderEventId",
                table: "ProviderEvent",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAttempt");

            migrationBuilder.DropTable(
                name: "PaymentIntents");

            migrationBuilder.DropTable(
                name: "PaymentOutbox");

            migrationBuilder.DropTable(
                name: "PaymentReconciliationIssue");

            migrationBuilder.DropTable(
                name: "PaymentRefund");

            migrationBuilder.DropTable(
                name: "ProviderEvent");
        }
    }
}
