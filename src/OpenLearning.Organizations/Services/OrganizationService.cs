using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Organizations.Models;

namespace OpenLearning.Organizations.Services;

public sealed class OrganizationService(DbContext db, IOrganizationContext context)
{
    public Task<List<Organization>> ListAsync()
    {
        return db.Set<Organization>().AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public Task<List<OrganizationMembership>> MembershipsForUserAsync(string userId)
    {
        return db.Set<OrganizationMembership>().AsNoTracking().Include(x => x.Organization).Where(x => x.UserId == userId && x.Status == MembershipStatus.Active).OrderBy(x => x.Organization!.Name).ToListAsync();
    }

    public async Task<Organization> CreateAsync(string name, string slug, string actorId)
    { var item = new Organization { Name = name.Trim(), Slug = slug.Trim().ToLowerInvariant(), Status = OrganizationStatus.Active }; db.Add(item); await db.SaveChangesAsync(); db.Add(new OrganizationMembership { OrganizationId = item.Id, UserId = actorId, Role = OrganizationRole.OrganizationAdmin, Status = MembershipStatus.Active }); await AuditAsync(item.Id, actorId, "organization.created", item.Name, false); await db.SaveChangesAsync(); return item; }
    public async Task SetStatusAsync(int id, OrganizationStatus status, string actorId)
    { var item = await db.Set<Organization>().FindAsync(id) ?? throw new InvalidOperationException("Organization not found."); item.Status = status; await AuditAsync(id, actorId, status == OrganizationStatus.Suspended ? "organization.suspended" : "organization.reactivated", item.Name, save: false); await db.SaveChangesAsync(); }
    public async Task ConfigurePlatformAsync(int id, string name, string color, int maxDepth, string actorId)
    {
        if (maxDepth is < 1 or > 20)
        { throw new ArgumentOutOfRangeException(nameof(maxDepth)); }
        var item = await db.Set<Organization>().FindAsync(id) ?? throw new InvalidOperationException("Organization not found.");
        item.Name = name.Trim();
        item.PrimaryColor = color.Trim();
        item.MaximumDepartmentDepth = maxDepth;
        await AuditAsync(id, actorId, "organization.configured", item.Name, false);
        await db.SaveChangesAsync();
    }

    public async Task<Department> AddDepartmentAsync(string name, int? parentId, string actorId)
    { var active = await RequireAdminAsync(); if (parentId is not null) { await ValidateParentAsync(active.Id, 0, parentId); } var item = new Department { OrganizationId = active.Id, Name = name.Trim(), ParentId = parentId }; db.Add(item); await AuditAsync(active.Id, actorId, "department.created", item.Name, false); await db.SaveChangesAsync(); return item; }
    public async Task MoveDepartmentAsync(int id, int? parentId, string actorId)
    { var active = await RequireAdminAsync(); var item = await db.Set<Department>().SingleOrDefaultAsync(x => x.Id == id && x.OrganizationId == active.Id) ?? throw new InvalidOperationException("Department not found."); await ValidateParentAsync(active.Id, id, parentId); item.ParentId = parentId; await AuditAsync(active.Id, actorId, "department.moved", id.ToString(CultureInfo.InvariantCulture), false); await db.SaveChangesAsync(); }

    public async Task<OrganizationMembership> AddMembershipAsync(string userId, OrganizationRole role, string actorId)
    { var active = await RequireAdminAsync(); var item = await db.Set<OrganizationMembership>().SingleOrDefaultAsync(x => x.OrganizationId == active.Id && x.UserId == userId); if (item is null) { item = new OrganizationMembership { OrganizationId = active.Id, UserId = userId, Role = role, Status = MembershipStatus.Active }; db.Add(item); } else { item.Role = role; item.Status = MembershipStatus.Active; } await AuditAsync(active.Id, actorId, "membership.upserted", userId, false); await db.SaveChangesAsync(); return item; }
    public async Task SuspendMembershipAsync(int membershipId, string actorId)
    { var active = await RequireAdminAsync(); var item = await db.Set<OrganizationMembership>().SingleOrDefaultAsync(x => x.Id == membershipId && x.OrganizationId == active.Id) ?? throw new InvalidOperationException("Membership not found."); item.Status = MembershipStatus.Suspended; await AuditAsync(active.Id, actorId, "membership.suspended", item.UserId, false); await db.SaveChangesAsync(); }
    public async Task<string> InviteAsync(string email, OrganizationRole role, string actorId)
    { var active = await RequireAdminAsync(); var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); db.Add(new OrganizationInvitation { OrganizationId = active.Id, Email = email.Trim().ToLowerInvariant(), Role = role, TokenHash = Hash(token), ExpiresAt = DateTime.UtcNow.AddDays(7) }); await AuditAsync(active.Id, actorId, "invitation.created", email, false); await db.SaveChangesAsync(); return token; }
    public async Task<bool> AcceptInvitationAsync(string token, string userId, string email)
    { var hash = Hash(token); var invitation = await db.Set<OrganizationInvitation>().SingleOrDefaultAsync(x => x.TokenHash == hash && x.AcceptedAt == null && x.ExpiresAt > DateTime.UtcNow); if (invitation is null || !string.Equals(invitation.Email, email, StringComparison.OrdinalIgnoreCase)) { return false; } var existing = await db.Set<OrganizationMembership>().SingleOrDefaultAsync(x => x.OrganizationId == invitation.OrganizationId && x.UserId == userId); if (existing is null) { db.Add(new OrganizationMembership { OrganizationId = invitation.OrganizationId, UserId = userId, Role = invitation.Role, Status = MembershipStatus.Active }); } invitation.AcceptedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return true; }

    public async Task AssignCourseAsync(int courseId, string actorId)
    { var active = await RequireAdminAsync(); if (!await db.Set<OrganizationCourse>().AnyAsync(x => x.OrganizationId == active.Id && x.CourseId == courseId)) { db.Add(new OrganizationCourse { OrganizationId = active.Id, CourseId = courseId }); } await AuditAsync(active.Id, actorId, "course.assigned", courseId.ToString(CultureInfo.InvariantCulture), false); await db.SaveChangesAsync(); }
    public async Task<List<OrganizationCourse>> CoursesAsync() { var active = await RequireMemberAsync(); return await db.Set<OrganizationCourse>().AsNoTracking().Include(x => x.Course).Where(x => x.OrganizationId == active.Id).ToListAsync(); }
    public async Task<List<Department>> DepartmentsAsync() { var active = await RequireMemberAsync(); return await db.Set<Department>().AsNoTracking().Where(x => x.OrganizationId == active.Id).OrderBy(x => x.Name).ToListAsync(); }
    public async Task<List<OrganizationMembership>> MembershipsAsync() { var active = await RequireAdminAsync(); return await db.Set<OrganizationMembership>().AsNoTracking().Include(x => x.User).Where(x => x.OrganizationId == active.Id).ToListAsync(); }
    public async Task<List<OrganizationAudit>> AuditsAsync(int organizationId)
    {
        return await db.Set<OrganizationAudit>().AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<T>> RunForEachActiveAsync<T>(Func<int, Task<T>> operation) { var ids = await db.Set<Organization>().Where(x => x.Status == OrganizationStatus.Active).Select(x => x.Id).ToListAsync(); var output = new List<T>(); foreach (var id in ids) { output.Add(await operation(id)); } return output; }

    private async Task<ActiveOrganization> RequireMemberAsync()
    {
        return await context.GetActiveAsync() ?? throw new UnauthorizedAccessException("An active organization membership is required.");
    }

    private async Task<ActiveOrganization> RequireAdminAsync() { var active = await RequireMemberAsync(); if (active.Role != OrganizationRole.OrganizationAdmin) { throw new UnauthorizedAccessException("Organization administrator access is required."); } return active; }
    private async Task ValidateParentAsync(int organizationId, int movingId, int? parentId)
    {
        if (parentId is null)
        {
            return;
        }

        var departments = await db.Set<Department>()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new { x.Id, x.ParentId })
            .ToListAsync();
        if (!departments.Any(x => x.Id == parentId))
        {
            throw new InvalidOperationException("Parent department not found.");
        }

        var seen = new HashSet<int> { movingId };
        var cursor = parentId;
        var depth = 1;
        var max = await db.Set<Organization>().Where(x => x.Id == organizationId)
            .Select(x => x.MaximumDepartmentDepth).SingleAsync();
        while (cursor is not null)
        {
            if (!seen.Add(cursor.Value))
            {
                throw new InvalidOperationException("Department move would create a cycle.");
            }

            cursor = departments.First(x => x.Id == cursor.Value).ParentId;
            depth++;
        }

        if (depth > max)
        {
            throw new InvalidOperationException($"Department depth cannot exceed {max}.");
        }
    }
    private async Task AuditAsync(int organizationId, string actorId, string action, string details, bool save = true) { db.Add(new OrganizationAudit { OrganizationId = organizationId, ActorId = actorId, Action = action, Details = details }); if (save) { await db.SaveChangesAsync(); } }
    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
