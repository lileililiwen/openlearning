using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Lti.Models;
using OpenLearning.Lti.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public sealed class LtiModel : PageModel
{
    private readonly LtiAdminService _lti;
    private readonly DbContext _db;
    public LtiModel(LtiAdminService lti, DbContext db) { _lti = lti; _db = db; }
    public List<LtiRegistration> Registrations { get; private set; } = new();
    public List<Course> Courses { get; private set; } = new();
    public List<LtiAuditEvent> Audit { get; private set; } = new();
    public async Task OnGetAsync() { Registrations = await _lti.ListAsync(); Courses = await _db.Set<Course>().AsNoTracking().OrderBy(x => x.Title).ToListAsync(); Audit = await _lti.AuditAsync(); }
    public async Task<IActionResult> OnPostCreateAsync(string name, string issuer, string clientId, string authorizationEndpoint, string jwksUrl, string? tokenEndpoint, bool deepLinking, bool nrps, bool ags)
    {
        var capabilities = (deepLinking ? LtiCapabilities.DeepLinking : 0) | (nrps ? LtiCapabilities.Nrps : 0) | (ags ? LtiCapabilities.Ags : 0);
        try
        { await _lti.CreateAsync(name, issuer, clientId, authorizationEndpoint, jwksUrl, tokenEndpoint, capabilities); Flash("Registration created."); }
        catch (Exception ex) when (ex is ArgumentException or DbUpdateException) { Flash(ex.Message, false); }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeploymentAsync(int registrationId, string deploymentId) { try { await _lti.AddDeploymentAsync(registrationId, deploymentId); Flash("Deployment added."); } catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or DbUpdateException) { Flash(ex.Message, false); } return RedirectToPage(); }
    public async Task<IActionResult> OnPostMappingAsync(int deploymentId, string contextId, int courseId) { try { await _lti.MapContextAsync(deploymentId, contextId, courseId); Flash("Context mapped."); } catch (Exception ex) when (ex is ArgumentException or DbUpdateException) { Flash(ex.Message, false); } return RedirectToPage(); }
    public async Task<IActionResult> OnPostRevokeAsync(int id) { await _lti.RevokeAsync(id); Flash("Registration revoked."); return RedirectToPage(); }
    public async Task<IActionResult> OnPostRotateKeyAsync() { var key = await _lti.RotateKeyAsync(); Flash($"Signing key rotated: {key.KeyId}"); return RedirectToPage(); }
    private void Flash(string message, bool ok = true) { TempData["Message"] = message; TempData["MessageType"] = ok ? "success" : "danger"; }
}
