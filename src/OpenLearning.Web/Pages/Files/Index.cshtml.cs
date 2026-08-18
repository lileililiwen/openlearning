using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;

namespace OpenLearning.Web.Pages.Files;

[Authorize]
public class IndexModel : PageModel
{
    private readonly StorageService _storage;

    public IndexModel(StorageService storage)
    {
        _storage = storage;
    }

    [BindProperty]
    public FilePurpose Purpose { get; set; } = FilePurpose.Avatar;

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public string? Message { get; set; }

    public string? MessageType { get; set; }

    public string? UploadedKey { get; set; }

    public int UploadedId { get; set; }

    public FilePurpose UploadedPurpose { get; set; }

    public void OnGet()
    {
        // GET renders the upload form; files are uploaded on POST.
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Upload is null || Upload.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose a file to upload.");
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await using var stream = Upload.OpenReadStream();
        var (file, error) = await _storage.UploadAsync(
            userId, Purpose, Upload.FileName, Upload.ContentType, stream);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        Message = "Uploaded.";
        MessageType = "success";
        UploadedKey = file!.Key;
        UploadedId = file.Id;
        UploadedPurpose = file.Purpose;
        return Page();
    }
}
