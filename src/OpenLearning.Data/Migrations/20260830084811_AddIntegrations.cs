using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationAuditRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeliveryId = table.Column<int>(type: "integer", nullable: true),
                    RegistrationId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Disposition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RedactedPayload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationAuditRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationDelivery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegistrationId = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadReference = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastErrorCategory = table.Column<int>(type: "integer", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationDelivery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationRegistration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false),
                    CapabilitiesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EndpointReference = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SecretReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationRegistration", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAuditRecord_DeliveryId",
                table: "IntegrationAuditRecord",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAuditRecord_RegistrationId",
                table: "IntegrationAuditRecord",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAuditRecord_TenantId",
                table: "IntegrationAuditRecord",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDelivery_RegistrationId",
                table: "IntegrationDelivery",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDelivery_Status",
                table: "IntegrationDelivery",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDelivery_TenantId",
                table: "IntegrationDelivery",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDelivery_TenantId_IdempotencyKey",
                table: "IntegrationDelivery",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRegistration_TenantId",
                table: "IntegrationRegistration",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRegistration_TenantId_Type",
                table: "IntegrationRegistration",
                columns: new[] { "TenantId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationAuditRecord");

            migrationBuilder.DropTable(
                name: "IntegrationDelivery");

            migrationBuilder.DropTable(
                name: "IntegrationRegistration");
        }
    }
}
