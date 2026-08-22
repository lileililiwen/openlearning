using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;

namespace OpenLearning.Web.Pages.StudyTools;

[Authorize]
public class NotesModel : PageModel
{
    private readonly LearnerNoteService _noteService;

    public NotesModel(LearnerNoteService noteService)
    {
        _noteService = noteService;
    }

    public List<LearnerNote> Notes { get; set; } = new();

    [BindProperty]
    public NoteInput Input { get; set; } = new(string.Empty, NoteContextType.Course, 0, null, null);

    [BindProperty]
    public int? EditNoteId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? Error { get; set; }

    public NoteContextType? FilterContextType { get; set; }

    public string? FilterTag { get; set; }

    public string? FilterSearch { get; set; }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    public async Task<IActionResult> OnGetAsync(
        NoteContextType? contextType = null,
        int? contextId = null,
        int? mediaOffset = null,
        string? tag = null,
        string? search = null,
        bool export = false)
    {
        if (export)
        {
            var entries = await _noteService.ExportAsync(UserId);
            var csv = LearnerNoteService.RenderExportCsv(entries);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "notes-export.csv");
        }

        FilterContextType = contextType;
        FilterTag = tag;
        FilterSearch = search;

        Notes = await _noteService.ListAsync(UserId, contextType, contextId, tag, search);

        if (contextType.HasValue)
        {
            Input = Input with { ContextType = contextType.Value };
        }

        if (contextId.HasValue)
        {
            Input = Input with { ContextId = contextId.Value };
        }

        if (mediaOffset.HasValue)
        {
            Input = Input with { MediaOffsetSeconds = mediaOffset.Value };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var (_, error) = await _noteService.CreateAsync(UserId, Input);
        if (error is not null)
        {
            Error = error;
            return RedirectToPage();
        }

        StatusMessage = "Note created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (EditNoteId is null)
        {
            Error = "No note selected for editing.";
            return RedirectToPage();
        }

        var (ok, error) = await _noteService.UpdateAsync(UserId, EditNoteId.Value, Input);
        if (!ok)
        {
            Error = error ?? "Failed to update note.";
            return RedirectToPage();
        }

        StatusMessage = "Note updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int noteId)
    {
        var ok = await _noteService.DeleteAsync(UserId, noteId);
        StatusMessage = ok ? "Note deleted." : "Failed to delete note.";
        return RedirectToPage();
    }
}
