using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankTopic",
                table: "Questions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBank",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_IsBank_ArchivedAt",
                table: "Questions",
                columns: new[] { "IsBank", "ArchivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_IsBank_ArchivedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "BankTopic",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "IsBank",
                table: "Questions");
        }
    }
}
