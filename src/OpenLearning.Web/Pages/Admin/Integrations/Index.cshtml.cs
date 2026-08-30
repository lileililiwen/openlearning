using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.Integrations.Models;
using OpenLearning.Integrations.Services;

namespace OpenLearning.Web.Pages.Admin.Integrations;

[Authorize(Policy = Policies.RequireAdmin)]
public sealed class IntegrationsModel : PageModel
{
    private readonly IntegrationOperationsService _integrations;

    public IntegrationsModel(IntegrationOperationsService integrations)
    {
        _integrations = integrations;
    }

    public List<IntegrationRegistration> Registrations { get; private set; } = new();

    public List<IntegrationDelivery> Deliveries { get; private set; } = new();

    public List<IntegrationAuditRecord> Audit { get; private set; } = new();

    private const string _tenant = "platform";

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        Registrations = await _integrations.GetIntegrationsAsync(_tenant);
        Deliveries = await _integrations.GetDeliveriesAsync(_tenant);
        Audit = await _integrations.GetAuditAsync(_tenant);
        return Page();
    }

    public async Task<IActionResult> OnPostDeclareAsync(string name, string type, string? endpoint, int contractVersion, string? capabilities)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        var actor = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "admin";
        var capabilitiesJson = CapabilitiesToJson(capabilities);
        var result = await _integrations.DeclareIntegrationAsync(_tenant, actor, new DeclareIntegrationRequest
        {
            Name = name,
            Type = type,
            ContractVersion = contractVersion,
            EndpointReference = endpoint,
            CapabilitiesJson = capabilitiesJson,
        });

        if (!result.Succeeded)
        {
            Flash(string.Join(" ", result.Errors.Select(e => e.Message)), false);
            return RedirectToPage();
        }

        Flash("Integration declared.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetEnabledAsync(int registrationId, bool enabled)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        var actor = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "admin";
        await _integrations.SetEnabledAsync(_tenant, actor, registrationId, enabled);
        Flash(enabled ? "Integration enabled." : "Integration disabled.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReplayAsync(int deliveryId)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        var actor = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "admin";
        var result = await _integrations.ReplayDeadLetterAsync(_tenant, actor, deliveryId);
        Flash(result.Status == IntegrationDeliveryStatus.Delivered
            ? "Dead-letter replayed successfully."
            : $"Replay finished with status {result.Status}.");
        return RedirectToPage();
    }

    private static string CapabilitiesToJson(string? capabilities)
    {
        if (string.IsNullOrWhiteSpace(capabilities))
        {
            return "[]";
        }

        var items = capabilities.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        return System.Text.Json.JsonSerializer.Serialize(items);
    }

    private void Flash(string message, bool ok = true)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = ok ? "success" : "danger";
    }
}
