using System.Threading;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Data;
using OpenLearning.Outcomes.Services;

namespace OpenLearning.Web.Pages.Courses.Outcomes;

/// <summary>
/// Reads recorded quiz attempts for a learner in a course and adapts them to the
/// module-neutral <see cref="ActivityResult"/> shape. Lives in the Web composition
/// root so the Outcomes module never depends on Assessments.
/// </summary>
public sealed class DbOutcomeActivitySource : IOutcomeActivitySource
{
    private readonly ApplicationDbContext _db;

    public DbOutcomeActivitySource(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ActivityResult>> GetResultsAsync(int courseId, string learnerId, CancellationToken cancellationToken = default)
    {
        var quizIds = await _db.Set<Quiz>()
            .Where(q => q.CourseId == courseId)
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        if (quizIds.Count == 0)
        {
            return Array.Empty<ActivityResult>();
        }

        var attempts = await _db.Set<QuizAttempt>()
            .Where(a => a.StudentId == learnerId && quizIds.Contains(a.QuizId))
            .ToListAsync(cancellationToken);

        var results = new List<ActivityResult>();
        foreach (var group in attempts.GroupBy(a => a.QuizId))
        {
            var best = 0.0;
            var lastAttempt = DateTime.MinValue;
            foreach (var attempt in group)
            {
                var fraction = attempt.MaxScore > 0 ? (double)attempt.Score / attempt.MaxScore : 0.0;
                if (fraction > best)
                {
                    best = fraction;
                }

                if (attempt.CompletedAt > lastAttempt)
                {
                    lastAttempt = attempt.CompletedAt;
                }
            }

            results.Add(new ActivityResult
            {
                ActivityId = group.Key,
                ActivityType = "Quiz",
                ScoreFraction = best,
                Attempts = group.Count(),
                LastAttemptAt = lastAttempt == DateTime.MinValue ? null : lastAttempt,
            });
        }

        return results;
    }
}
