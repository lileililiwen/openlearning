
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;

namespace OpenLearning.Web.Pages.Courses.Exams.Integrity;

/// <summary>Client-submitted integrity event batch.</summary>
public sealed class IntegritySignalModel
{
    public int SessionId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public List<IntegritySignalEvent> Events { get; set; } = new();
}

public sealed class IntegritySignalEvent
{
    public long Sequence { get; set; }
    public IntegrityEventType Type { get; set; }
    public DateTime ClientTimestamp { get; set; }
    public string? Payload { get; set; }
}

/// <summary>
/// JSON endpoint the student's browser calls to stream allowlisted integrity
/// signals. Authorization is the signed session token; no camera/mic/biometrics.
/// CSRF protection stays on; the client includes the page's anti-forgery token.
/// </summary>
public class SignalModel : PageModel
{
    private readonly ExamIntegrityService _integrity;

    public SignalModel(ExamIntegrityService integrity)
    {
        _integrity = integrity;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        IntegritySignalModel? model;
        try
        {
            model = JsonSerializer.Deserialize<IntegritySignalModel>(json);
        }
        catch (JsonException)
        {
            model = null;
        }

        if (model is null)
        {
            return BadRequest(new { error = "Model binding failed." });
        }

        if (!_integrity.ValidateToken(model.SessionId, model.Token))
        {
            return BadRequest(new { error = "Invalid session token." });
        }

        var inputs = model.Events
            .Select(e => new EvidenceInput(e.Sequence, e.Type, e.ClientTimestamp, e.Payload))
            .ToList();
        var result = await _integrity.IngestAsync(model.SessionId, model.Token, model.BatchId, inputs);
        if (!result.Accepted)
        {
            return BadRequest(new { error = result.Error, lastSequence = result.LastSequence });
        }

        return new JsonResult(new
        {
            accepted = true,
            lastSequence = result.LastSequence,
            replayed = result.Replayed,
        });
    }
}
