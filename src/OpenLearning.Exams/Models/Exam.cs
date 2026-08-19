using OpenLearning.Assessments.Models;
using OpenLearning.Auth.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Exams.Models;

/// <summary>
/// A formal exam: a timed, attempt-limited assessment attached to a course.
/// Exam questions reuse the assessments <see cref="Question"/> model so the
/// question-types capability (auto-scoring, manual grading) applies unchanged.
/// </summary>
public class Exam
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Instructor who owns the course and manages this exam.</summary>
    public string AuthorId { get; set; } = string.Empty;

    public ApplicationUser? Author { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Official exams count toward credentials; mock exams are practice.</summary>
    public bool IsOfficial { get; set; }

    /// <summary>Allowed time to complete the exam in minutes.</summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>Minimum percent score required to pass.</summary>
    public int PassPercent { get; set; } = 60;

    /// <summary>Allowed number of attempts per student; mock defaults to 3, official to 1.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Optional scheduling window; outside of it the exam cannot be taken.</summary>
    public DateTime? OpensAt { get; set; }

    public DateTime? ClosesAt { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    public ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
}
