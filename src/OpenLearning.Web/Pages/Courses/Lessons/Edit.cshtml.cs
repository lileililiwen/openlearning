using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.CourseManagement.Models;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Scorm.Models;
using OpenLearning.Scorm.Services;

namespace OpenLearning.Web.Pages.Courses.Lessons;

[Authorize(Policy = Policies.RequireInstructor)]
public class EditModel : PageModel
{
    private readonly LessonService _lessons;
    private readonly ScormService _scorm;
    private readonly IWebHostEnvironment _environment;

    public EditModel(LessonService lessons, ScormService scorm, IWebHostEnvironment environment)
    {
        _lessons = lessons;
        _scorm = scorm;
        _environment = environment;
    }

    public Lesson? Lesson { get; set; }

    public ScormPackage? ScormPackage { get; set; }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? ScormFile { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Video URL")]
        [StringLength(1000)]
        public string? VideoUrl { get; set; }

        [Display(Name = "Poster URL (optional)")]
        [StringLength(1000)]
        public string? VideoPosterUrl { get; set; }

        [Display(Name = "Subtitles URL (.vtt, optional)")]
        [StringLength(1000)]
        public string? SubtitleUrl { get; set; }

        [Display(Name = "Preview lesson (visible to non-enrolled visitors of a published course)")]
        public bool IsPreview { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var lesson = await _lessons.GetByIdAsync(id);
        if (lesson is null)
        {
            return NotFound();
        }

        if (lesson.Module?.Course is null || lesson.Module.Course.InstructorId != userId)
        {
            return Forbid();
        }

        Lesson = lesson;
        Id = id;
        Input.Title = lesson.Title;
        Input.Content = lesson.Content;
        Input.VideoUrl = lesson.VideoUrl;
        Input.VideoPosterUrl = lesson.VideoPosterUrl;
        Input.SubtitleUrl = lesson.SubtitleUrl;
        Input.IsPreview = lesson.IsPreview;
        ScormPackage = await _scorm.GetForLessonAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            Lesson = await _lessons.GetByIdAsync(Id);
            ScormPackage = await _scorm.GetForLessonAsync(Id);
            return Page();
        }

        var updated = await _lessons.UpdateAsync(
            Id, userId, Input.Title, Input.Content, Input.VideoUrl, Input.VideoPosterUrl, Input.SubtitleUrl, Input.IsPreview);
        if (!updated)
        {
            return Forbid();
        }

        var courseId = (await _lessons.GetByIdAsync(Id))!.Module!.CourseId;
        return RedirectToPage("/Courses/Edit", new { id = courseId });
    }

    public async Task<IActionResult> OnPostUploadScormAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (ScormFile is null || ScormFile.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Choose a SCORM .zip file to upload.");
            Lesson = await _lessons.GetByIdAsync(id);
            ScormPackage = await _scorm.GetForLessonAsync(id);
            return Page();
        }

        await using var stream = ScormFile.OpenReadStream();
        var (_, error) = await _scorm.UploadAsync(
            id, userId, _environment.WebRootPath, stream, ScormFile.FileName);

        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        Lesson = await _lessons.GetByIdAsync(id);
        ScormPackage = await _scorm.GetForLessonAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveScormAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var package = await _scorm.GetForLessonAsync(id);
        if (package is not null)
        {
            await _scorm.RemoveAsync(package.Id, userId, _environment.WebRootPath);
        }

        return RedirectToPage(new { id });
    }
}
