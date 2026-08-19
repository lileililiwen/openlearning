using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Assessments.Models;
using OpenLearning.Assessments.Services;
using OpenLearning.Auth.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Notifications.Models;
using OpenLearning.Notifications.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;

namespace OpenLearning.Web.Pages.Courses.Quizzes;

public class TakeModel : PageModel
{
    private readonly AttemptService _attempts;
    private readonly EnrollmentService _enrollments;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notifications;
    private readonly StorageService _storage;

    public TakeModel(
        AttemptService attempts,
        EnrollmentService enrollments,
        UserManager<ApplicationUser> userManager,
        NotificationService notifications,
        StorageService storage)
    {
        _attempts = attempts;
        _enrollments = enrollments;
        _userManager = userManager;
        _notifications = notifications;
        _storage = storage;
    }

    public Quiz? Quiz { get; set; }

    public List<QuizAttempt> Attempts { get; set; } = new();

    [BindProperty]
    public int QuizId { get; set; }

    /// <summary>Radios for single-choice and true/false questions.</summary>
    [BindProperty]
    public Dictionary<int, int> Answers { get; set; } = new();

    /// <summary>Checkbox groups for multiple-choice questions.</summary>
    [BindProperty]
    public Dictionary<int, string[]> Multiple { get; set; } = new();

    /// <summary>Free text for fill-in-the-blank and short-answer questions.</summary>
    [BindProperty]
    public Dictionary<int, string> TextAnswers { get; set; } = new();

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

        var quiz = await _attempts.GetQuizForTakeAsync(id);
        if (quiz is null)
        {
            return NotFound();
        }

        if (!await _enrollments.IsEnrolledAsync(userId, quiz.CourseId))
        {
            return Forbid();
        }

        Quiz = quiz;
        QuizId = id;
        Attempts = await _attempts.GetAttemptsForStudentAsync(userId, id);
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

        if (!ModelState.IsValid)
        {
            Quiz = await _attempts.GetQuizForTakeAsync(QuizId);
            return Page();
        }

        var quiz = await _attempts.GetQuizForTakeAsync(QuizId);
        if (quiz is null)
        {
            return NotFound();
        }

        var answerInputs = new Dictionary<int, AttemptService.QuizAnswerInput>();
        foreach (var question in quiz.Questions.OrderBy(q => q.OrderIndex))
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
                            Quiz = quiz;
                            return Page();
                        }

                        fileUrl = $"/files/{stored!.Key}";
                    }

                    answerInputs[question.Id] = new AttemptService.QuizAnswerInput(null, null, null, fileUrl);
                    break;
            }
        }

        var (attemptId, error) = await _attempts.SubmitAsync(userId, QuizId, answerInputs);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            Quiz = quiz;
            Attempts = await _attempts.GetAttemptsForStudentAsync(userId, QuizId);
            return Page();
        }

        if (attemptId is null)
        {
            ModelState.AddModelError(string.Empty, "Could not save your attempt.");
            Quiz = quiz;
            Attempts = await _attempts.GetAttemptsForStudentAsync(userId, QuizId);
            return Page();
        }

        var attempt = await _attempts.GetAttemptAsync(attemptId.Value, userId);
        var submittedQuiz = attempt?.Quiz;
        if (submittedQuiz is not null)
        {
            var percent = attempt!.MaxScore > 0 ? (int)Math.Round(attempt.Score * 100.0 / attempt.MaxScore) : 0;
            await _notifications.CreateAsync(
                userId,
                NotificationType.Quiz,
                $"Quiz submitted: {submittedQuiz.Title}",
                $"Your score is {percent}%. View the result below.",
                $"/Courses/Quizzes/Result?id={attemptId}",
                new Dictionary<string, string>
                {
                    ["QuizTitle"] = submittedQuiz.Title,
                    ["Score"] = percent.ToString(CultureInfo.InvariantCulture),
                });
        }

        return RedirectToPage("/Courses/Quizzes/Result", new { id = attemptId });
    }
}
