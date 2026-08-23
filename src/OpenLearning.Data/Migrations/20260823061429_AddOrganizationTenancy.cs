using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenLearning.Data.Migrations;

public partial class AddOrganizationTenancy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("OrganizationAudits", table => new { Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), OrganizationId = table.Column<int>("integer"), ActorId = table.Column<string>("text"), Action = table.Column<string>("character varying(100)", maxLength: 100), Details = table.Column<string>("character varying(2000)", maxLength: 2000), CreatedAt = table.Column<DateTime>("timestamp with time zone") }, constraints: table => table.PrimaryKey("PK_OrganizationAudits", x => x.Id));
        migrationBuilder.CreateTable("OrganizationInvitations", table => new { Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), OrganizationId = table.Column<int>("integer"), Email = table.Column<string>("character varying(256)", maxLength: 256), Role = table.Column<int>("integer"), TokenHash = table.Column<string>("character varying(64)", maxLength: 64), ExpiresAt = table.Column<DateTime>("timestamp with time zone"), AcceptedAt = table.Column<DateTime>("timestamp with time zone", nullable: true) }, constraints: table => table.PrimaryKey("PK_OrganizationInvitations", x => x.Id));
        migrationBuilder.CreateTable("Organizations", table => new { Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), Name = table.Column<string>("character varying(200)", maxLength: 200), Slug = table.Column<string>("character varying(100)", maxLength: 100), Status = table.Column<int>("integer"), PrimaryColor = table.Column<string>("character varying(20)", maxLength: 20), MaximumDepartmentDepth = table.Column<int>("integer"), CreatedAt = table.Column<DateTime>("timestamp with time zone") }, constraints: table => table.PrimaryKey("PK_Organizations", x => x.Id));
        migrationBuilder.CreateTable("Departments", table => new { Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), OrganizationId = table.Column<int>("integer"), ParentId = table.Column<int>("integer", nullable: true), Name = table.Column<string>("character varying(200)", maxLength: 200) }, constraints: table => { table.PrimaryKey("PK_Departments", x => x.Id); table.ForeignKey("FK_Departments_Departments_ParentId", x => x.ParentId, "Departments", "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_Departments_Organizations_OrganizationId", x => x.OrganizationId, "Organizations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable("OrganizationMemberships", table => new { Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), OrganizationId = table.Column<int>("integer"), UserId = table.Column<string>("text"), Role = table.Column<int>("integer"), Status = table.Column<int>("integer"), JoinedAt = table.Column<DateTime>("timestamp with time zone") }, constraints: table => { table.PrimaryKey("PK_OrganizationMemberships", x => x.Id); table.ForeignKey("FK_OrganizationMemberships_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_OrganizationMemberships_Organizations_OrganizationId", x => x.OrganizationId, "Organizations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable("OrganizationCourses", table => new { Id = table.Column<int>("integer").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn), OrganizationId = table.Column<int>("integer"), CourseId = table.Column<int>("integer") }, constraints: table => { table.PrimaryKey("PK_OrganizationCourses", x => x.Id); table.ForeignKey("FK_OrganizationCourses_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex("IX_Departments_OrganizationId_Name", "Departments", new[] { "OrganizationId", "Name" });
        migrationBuilder.CreateIndex("IX_Departments_ParentId", "Departments", "ParentId");
        migrationBuilder.CreateIndex("IX_OrganizationAudits_OrganizationId_CreatedAt", "OrganizationAudits", new[] { "OrganizationId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_OrganizationCourses_CourseId", "OrganizationCourses", "CourseId");
        migrationBuilder.CreateIndex("IX_OrganizationCourses_OrganizationId_CourseId", "OrganizationCourses", new[] { "OrganizationId", "CourseId" }, unique: true);
        migrationBuilder.CreateIndex("IX_OrganizationInvitations_OrganizationId_Email", "OrganizationInvitations", new[] { "OrganizationId", "Email" });
        migrationBuilder.CreateIndex("IX_OrganizationMemberships_OrganizationId_UserId", "OrganizationMemberships", new[] { "OrganizationId", "UserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_OrganizationMemberships_UserId", "OrganizationMemberships", "UserId");
        migrationBuilder.CreateIndex("IX_Organizations_Slug", "Organizations", "Slug", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Departments");
        migrationBuilder.DropTable("OrganizationAudits");
        migrationBuilder.DropTable("OrganizationCourses");
        migrationBuilder.DropTable("OrganizationInvitations");
        migrationBuilder.DropTable("OrganizationMemberships");
        migrationBuilder.DropTable("Organizations");
    }
}
