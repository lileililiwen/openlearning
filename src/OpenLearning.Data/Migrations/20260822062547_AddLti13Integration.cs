using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLti13Integration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LtiAuditEvent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegistrationId = table.Column<int>(type: "integer", nullable: true),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    Detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiAuditEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiProtocolToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegistrationId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValueHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiProtocolToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    JwksUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TokenEndpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Capabilities = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiResourceLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContextMappingId = table.Column<int>(type: "integer", nullable: false),
                    ResourceLinkId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiResourceLink", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiScoreOperation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LineItemId = table.Column<int>(type: "integer", nullable: false),
                    OperationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiScoreOperation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiSigningKey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KeyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrivateKeyPem = table.Column<string>(type: "text", nullable: false),
                    PublicKeyPem = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiSigningKey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiSubject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeploymentId = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastLaunchAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiSubject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiDeployment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegistrationId = table.Column<int>(type: "integer", nullable: false),
                    DeploymentId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiDeployment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiDeployment_LtiRegistrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "LtiRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LtiContextMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeploymentId = table.Column<int>(type: "integer", nullable: false),
                    ExternalContextId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiContextMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiContextMapping_LtiDeployment_DeploymentId",
                        column: x => x.DeploymentId,
                        principalTable: "LtiDeployment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LtiLineItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContextMappingId = table.Column<int>(type: "integer", nullable: false),
                    ExternalLineItemId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AssignmentId = table.Column<int>(type: "integer", nullable: true),
                    MaximumScore = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiLineItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtiLineItem_LtiContextMapping_ContextMappingId",
                        column: x => x.ContextMappingId,
                        principalTable: "LtiContextMapping",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LtiAuditEvent_CreatedAt",
                table: "LtiAuditEvent",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LtiContextMapping_DeploymentId_ExternalContextId",
                table: "LtiContextMapping",
                columns: new[] { "DeploymentId", "ExternalContextId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiDeployment_RegistrationId_DeploymentId",
                table: "LtiDeployment",
                columns: new[] { "RegistrationId", "DeploymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiLineItem_ContextMappingId_ExternalLineItemId",
                table: "LtiLineItem",
                columns: new[] { "ContextMappingId", "ExternalLineItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiProtocolToken_RegistrationId_Kind_ValueHash",
                table: "LtiProtocolToken",
                columns: new[] { "RegistrationId", "Kind", "ValueHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiRegistrations_Issuer_ClientId",
                table: "LtiRegistrations",
                columns: new[] { "Issuer", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiResourceLink_ContextMappingId_ResourceLinkId",
                table: "LtiResourceLink",
                columns: new[] { "ContextMappingId", "ResourceLinkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiScoreOperation_LineItemId_OperationId",
                table: "LtiScoreOperation",
                columns: new[] { "LineItemId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiSigningKey_KeyId",
                table: "LtiSigningKey",
                column: "KeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiSubject_DeploymentId_Subject",
                table: "LtiSubject",
                columns: new[] { "DeploymentId", "Subject" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LtiAuditEvent");

            migrationBuilder.DropTable(
                name: "LtiLineItem");

            migrationBuilder.DropTable(
                name: "LtiProtocolToken");

            migrationBuilder.DropTable(
                name: "LtiResourceLink");

            migrationBuilder.DropTable(
                name: "LtiScoreOperation");

            migrationBuilder.DropTable(
                name: "LtiSigningKey");

            migrationBuilder.DropTable(
                name: "LtiSubject");

            migrationBuilder.DropTable(
                name: "LtiContextMapping");

            migrationBuilder.DropTable(
                name: "LtiDeployment");

            migrationBuilder.DropTable(
                name: "LtiRegistrations");
        }
    }
}
