using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Organizations.Authorization;
using OpenLearning.Organizations.Models;
using OpenLearning.Organizations.Services;

namespace OpenLearning.Web.Pages.Organizations;

[Authorize(Policy = OrganizationPolicies.Admin)]
public sealed class ManageModel(OrganizationService organizations, IOrganizationContext context, UserManager<ApplicationUser> users, CourseService courses) : PageModel
{
    public ActiveOrganization Active { get; private set; } = null!;
    public List<Department> Departments { get; private set; } = [];
    public List<OrganizationMembership> Memberships { get; private set; } = [];
    public List<OrganizationCourse> AssignedCourses { get; private set; } = [];
    public List<Course> Courses { get; private set; } = [];
    public List<ApplicationUser> Users { get; private set; } = [];
    public async Task OnGetAsync() { Active = (await context.GetActiveAsync())!; Departments = await organizations.DepartmentsAsync(); Memberships = await organizations.MembershipsAsync(); AssignedCourses = await organizations.CoursesAsync(); Courses = await courses.GetAllAsync(); Users = await users.Users.OrderBy(x => x.Email).ToListAsync(); }
    public async Task<IActionResult> OnPostDepartmentAsync(string name, int? parentId) { await organizations.AddDepartmentAsync(name, parentId, Actor()); return RedirectToPage(); }
    public async Task<IActionResult> OnPostMoveAsync(int id, int? parentId) { try { await organizations.MoveDepartmentAsync(id, parentId, Actor()); } catch (InvalidOperationException e) { TempData["Message"] = e.Message; TempData["MessageType"] = "danger"; } return RedirectToPage(); }
    public async Task<IActionResult> OnPostMemberAsync(string userId, OrganizationRole role) { await organizations.AddMembershipAsync(userId, role, Actor()); return RedirectToPage(); }
    public async Task<IActionResult> OnPostSuspendMemberAsync(int id) { await organizations.SuspendMembershipAsync(id, Actor()); return RedirectToPage(); }
    public async Task<IActionResult> OnPostInviteAsync(string email, OrganizationRole role) { var token = await organizations.InviteAsync(email, role, Actor()); TempData["Message"] = $"Invitation created. One-time token: {token}"; return RedirectToPage(); }
    public async Task<IActionResult> OnPostCourseAsync(int courseId) { await organizations.AssignCourseAsync(courseId, Actor()); return RedirectToPage(); }
    private string Actor()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
