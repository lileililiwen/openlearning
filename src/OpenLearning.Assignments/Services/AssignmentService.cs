using Microsoft.EntityFrameworkCore;
using OpenLearning.Assignments.Models;
using OpenLearning.Notifications.Services;

namespace OpenLearning.Assignments.Services;

/// <summary>
/// Assignment CRUD (owner-gated), student submission with resubmit rules, and
/// instructor grading. One submission per student per assignment.
/// </summary>
public class AssignmentService
{
    private readonly DbContext _db;
    private readonly NotificationService _notifications;

    public AssignmentService(DbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    // ===== Owner-gated management =====

    public Task<List<Assignment>> GetForCourseAsync(int courseId)
    {
        return _db.Set<Assignment>().AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.DueAt)
            .ThenBy(a => a.Id)
            .ToListAsync();
    }

    public Task<Assignment?> GetByIdAsync(int id)
    {
        return _db.Set<Assignment>().AsNoTracking()
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public Task<bool> IsOwnerAsync(int id, string authorId)
    {
        return _db.Set<Assignment>().AnyAsync(a => a.Id == id && a.AuthorId == authorId);
    }

    public async Task<(bool Ok, string? Error)> CreateAsync(
        int courseId, string authorId, string title, string instructions, DateTime? dueAt, bool allowResubmitAfterGrading)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length is 0 or > 200)
        {
            return (false, "Assignment title is required (200 characters or fewer).");
        }

        if (string.IsNullOrWhiteSpace(instructions))
        {
            return (false, "Assignment instructions are required.");
        }

        _db.Set<Assignment>().Add(new Assignment
        {
            CourseId = courseId,
            AuthorId = authorId,
            Title = trimmedTitle,
            Instructions = instructions.Trim(),
            DueAt = Normalize(dueAt),
            AllowResubmitAfterGrading = allowResubmitAfterGrading,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(
        int id, string authorId, string title, string instructions, DateTime? dueAt, bool allowResubmitAfterGrading)
    {
        var assignment = await _db.Set<Assignment>()
            .FirstOrDefaultAsync(a => a.Id == id && a.AuthorId == authorId);
        if (assignment is null)
        {
            return (false, "Assignment not found.");
        }

        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length is 0 or > 200)
        {
            return (false, "Assignment title is required (200 characters or fewer).");
        }

        if (string.IsNullOrWhiteSpace(instructions))
        {
            return (false, "Assignment instructions are required.");
        }

        assignment.Title = trimmedTitle;
        assignment.Instructions = instructions.Trim();
        assignment.DueAt = Normalize(dueAt);
        assignment.AllowResubmitAfterGrading = allowResubmitAfterGrading;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(int id, string authorId)
    {
        var assignment = await _db.Set<Assignment>()
            .FirstOrDefaultAsync(a => a.Id == id && a.AuthorId == authorId);
        if (assignment is null)
        {
            return (false, "Assignment not found.");
        }

        _db.Set<Assignment>().Remove(assignment);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Submissions =====

    public async Task<AssignmentSubmission?> GetSubmissionAsync(int assignmentId, string studentId)
    {
        return await _db.Set<AssignmentSubmission>().AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId && s.StudentId == studentId)
            .FirstOrDefaultAsync();
    }

    public Task<AssignmentSubmission?> GetSubmissionByIdAsync(int submissionId)
    {
        return _db.Set<AssignmentSubmission>().AsNoTracking()
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
    }

    public Task<List<AssignmentSubmission>> GetSubmissionsAsync(int assignmentId)
    {
        return _db.Set<AssignmentSubmission>().AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId)
            .OrderBy(s => s.SubmittedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Submits (or resubmits) for a student. Resubmission before grading always
    /// replaces; after grading it is allowed only when the flag is set.
    /// </summary>
    public async Task<(bool Ok, string? Error)> SubmitAsync(
        int assignmentId, string studentId, string text, string? fileUrl)
    {
        var assignment = await _db.Set<Assignment>().FindAsync(assignmentId);
        if (assignment is null)
        {
            return (false, "Assignment not found.");
        }

        if (assignment.DueAt is DateTime due && DateTime.UtcNow > due)
        {
            return (false, "This assignment is past its due date.");
        }

        var existing = await _db.Set<AssignmentSubmission>()
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

        if (existing is not null && existing.GradedAt is not null && !assignment.AllowResubmitAfterGrading)
        {
            return (false, "This assignment has been graded and resubmission is not allowed.");
        }

        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(fileUrl))
        {
            return (false, "Submit either text or a file (or both).");
        }

        if (existing is null)
        {
            _db.Set<AssignmentSubmission>().Add(new AssignmentSubmission
            {
                AssignmentId = assignmentId,
                StudentId = studentId,
                Text = text?.Trim() ?? string.Empty,
                FileUrl = fileUrl,
                SubmittedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Text = text?.Trim() ?? string.Empty;
            existing.FileUrl = fileUrl;
            existing.SubmittedAt = DateTime.UtcNow;
            // A resubmission resets any previous grading.
            existing.Score = null;
            existing.Feedback = null;
            existing.GradedAt = null;
            existing.GradedBy = null;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> GradeAsync(
        int submissionId, string graderId, int? score, string? feedback)
    {
        var submission = await _db.Set<AssignmentSubmission>().FindAsync(submissionId);
        if (submission is null)
        {
            return (false, "Submission not found.");
        }

        if (score is null)
        {
            return (false, "A score is required.");
        }

        if (score < 0 || score > 100)
        {
            return (false, "Score must be between 0 and 100.");
        }

        submission.Score = score;
        submission.Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        submission.GradedAt = DateTime.UtcNow;
        submission.GradedBy = graderId;
        await _db.SaveChangesAsync();

        // Emit assignment.graded exactly once per grade; a re-grade must not re-notify.
        if (submission.NotifiedAt is null)
        {
            var assignment = await _db.Set<Assignment>().AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == submission.AssignmentId);
            await _notifications.SendAsync(
                NotificationService.EventKeys.AssignmentGraded,
                submission.StudentId,
                new Dictionary<string, string>
                {
                    ["assignmentTitle"] = assignment?.Title ?? string.Empty,
                    ["score"] = score.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                },
                $"/Courses/Assignments/Detail?id={submission.AssignmentId}");
            submission.NotifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return (true, null);
    }

    /// <summary>Count of submissions still awaiting grading for an assignment set.</summary>
    public async Task<int> GetUngradedCountAsync(IEnumerable<int> assignmentIds)
    {
        var ids = assignmentIds.ToList();
        if (ids.Count == 0)
        {
            return 0;
        }

        return await _db.Set<AssignmentSubmission>()
            .CountAsync(s => ids.Contains(s.AssignmentId) && s.GradedAt == null);
    }

    private static DateTime? Normalize(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();
    }

    /// <summary>Assignments due within the given window (for the due-reminder job).</summary>
    public Task<List<Assignment>> ListDueWithinAsync(DateTime now, TimeSpan window)
    {
        var horizon = now + window;
        return _db.Set<Assignment>().AsNoTracking()
            .Where(a => a.DueAt != null && a.DueAt.Value > now && a.DueAt.Value <= horizon)
            .OrderBy(a => a.DueAt)
            .ToListAsync();
    }

    /// <summary>Assignments whose due date passed but have not yet fired the due-missed notification.</summary>
    public Task<List<Assignment>> ListPastDueUnnotifiedAsync(DateTime now)
    {
        return _db.Set<Assignment>().AsNoTracking()
            .Where(a => a.DueAt != null && a.DueAt.Value < now && a.DueMissedNotifiedAt == null)
            .OrderBy(a => a.DueAt)
            .ToListAsync();
    }

    /// <summary>Marks an assignment as due-missed-notified (idempotency guard for the job).</summary>
    public async Task MarkDueMissedNotifiedAsync(int assignmentId)
    {
        var assignment = await _db.Set<Assignment>().FindAsync(assignmentId);
        if (assignment is null || assignment.DueMissedNotifiedAt is not null)
        {
            return;
        }

        assignment.DueMissedNotifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>Submitting students for an assignment (used to exclude them from reminders).</summary>
    public Task<List<string>> GetSubmittingStudentIdsAsync(int assignmentId)
    {
        return _db.Set<AssignmentSubmission>().AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId)
            .Select(s => s.StudentId)
            .Distinct()
            .ToListAsync();
    }
}
