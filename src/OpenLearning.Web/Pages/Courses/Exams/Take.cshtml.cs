using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Exams.Models;
using OpenLearning.Exams.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;

namespace OpenLearning.Web.Pages.Courses.Exams;

public class TakeModel : PageModel
{
    private readonly ExamService _exams;
    private readonly EnrollmentService _enrollments;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StorageService _storage;

    public TakeModel(
        ExamService exams,
        EnrollmentService enrollments,
        UserManager<ApplicationUser> userManager,
        StorageService storage)
    {
        _exams = exams;
        _enrollments = enrollments;
        _userManager = userManager;
        _storage = storage;
    }

    public Exam? Exam { get; set; }

    public ExamAttempt? Attempt { get; set; }

    /// <summary>Denial reason (window closed, attempt limit reached, etc.).</summary>
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public int ExamId { get; set; }

    [BindProperty]
    public int AttemptId { get; set; }

    /// <summary>
    /// Radios for single-choice and true/false questions. The explicit Name keeps the
    /// model prefix fixed even when the form has no "Answers[...]" keys; without it the
    /// binder falls back to an empty prefix and tries to parse every form field name
    /// (including "__RequestVerificationToken") as an int key (dotnet/aspnetcore#16663).
    /// </summary>
    [BindProperty(Name = "Answers")]
    public Dictionary<int, int> Answers { get; set; } = new();

    /// <summary>Checkbox groups for multiple-choice questions.</summary>
    [BindProperty(Name = "Multiple")]
    public Dictionary<int, string[]> Multiple { get; set; } = new();

    /// <summary>Free text for fill-in-the-blank and short-answer questions.</summary>
    [BindProperty(Name = "TextAnswers")]
    public Dictionary<int, string> TextAnswers { get; set; } = new();

    /// <summary>Times the student left the exam page; recorded on submit.</summary>
    [BindProperty]
    public int ScreenSwitchCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user?.IsSuspended == true)
        {
            return Forbid();
        }

        var exam = await _exams.GetForTakeAsync(id);
        if (exam is null)
        {
            return NotFound();
        }

        if (!await _enrollments.IsEnrolledAsync(userId, exam.CourseId))
        {
            return Forbid();
        }

        var (attempt, error) = await _exams.StartAsync(id, userId);
        Exam = exam;
        ExamId = id;
        Attempt = attempt;
        AttemptId = attempt?.Id ?? 0;
        ErrorMessage = error;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var user = await _userManager.GetUserAsync(User);
        if (user?.IsSuspended == true)
        {
            return Forbid();
        }

        var exam = await _exams.GetForTakeAsync(ExamId);
        if (exam is null)
        {
            return NotFound();
        }

        var answerInputs = new Dictionary<int, AttemptService.QuizAnswerInput>();
        foreach (var question in exam.Questions.OrderBy(q => q.OrderIndex))
        {
            switch (question.QuestionType)
            {
                case QuestionType.SingleChoice:
                case QuestionType.TrueFalse:
                    answerInputs[question.Id] = new AttemptService.QuizAnswerInput(
                        Answers.GetValueOrDefault(question.Id), null, null, null);
                    break;
                case QuestionType.MultipleChoice:
                    var selected = Multiple.GetValueOrDefault(question.Id) ?? Array.Empty<string>();
                    var ids = selected
                        .Where(s => int.TryParse(s, out _))
                        .Select(int.Parse)
                        .OrderBy(x => x);
                    answerInputs[question.Id] = new AttemptService.QuizAnswerInput(
                        null, string.Join(",", ids), null, null);
                    break;
                case QuestionType.FillBlank:
                case QuestionType.ShortAnswer:
                    answerInputs[question.Id] = new AttemptService.QuizAnswerInput(
                        null, null, TextAnswers.GetValueOrDefault(question.Id), null);
                    break;
                case QuestionType.FileUpload:
                    string? fileUrl = null;
                    var uploadedFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"Files[{question.Id}]");
                    if (uploadedFile is not null && uploadedFile.Length > 0)
                    {
                        var (stored, uploadError) = await _storage.UploadAsync(
                            userId, FilePurpose.Answer, uploadedFile.FileName, uploadedFile.ContentType, uploadedFile.OpenReadStream());
                        if (uploadError is not null)
                        {
                            ModelState.AddModelError(string.Empty, uploadError);
                            Exam = exam;
                            return Page();
                        }

                        fileUrl = $"/files/{stored!.Key}";
                    }

                    answerInputs[question.Id] = new AttemptService.QuizAnswerInput(null, null, null, fileUrl);
                    break;
            }
        }

        var (attemptId, error) = await _exams.SubmitAsync(AttemptId, userId, answerInputs, ScreenSwitchCount);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            Exam = exam;
            return Page();
        }

        if (attemptId is null)
        {
            ModelState.AddModelError(string.Empty, "Could not save your attempt.");
            Exam = exam;
            return Page();
        }

        return RedirectToPage("/Courses/Exams/Result", new { id = attemptId });
    }
}
