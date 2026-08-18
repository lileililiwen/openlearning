using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Services;
using OpenLearning.Enrollment.Services;
using OpenLearning.Notifications.Models;

namespace OpenLearning.Notifications.Services;

public class AnnouncementService
{
    private readonly DbContext _db;
    private readonly CourseService _courses;
    private readonly EnrollmentService _enrollments;
    private readonly NotificationService _notifications;

    public AnnouncementService(
        DbContext db,
        CourseService courses,
        EnrollmentService enrollments,
        NotificationService notifications)
    {
        _db = db;
        _courses = courses;
        _enrollments = enrollments;
        _notifications = notifications;
    }

    /// <summary>
    /// Posts a course announcement (owner-only) and notifies every enrolled
    /// student with a link to the course. Non-owners are denied.
    /// </summary>
    public async Task<(bool Ok, string? Error)> PostAsync(int courseId, string authorId, string body)
    {
        if (!await _courses.IsOwnerAsync(courseId, authorId))
        {
            return (false, "Only the course owner can post announcements.");
        }

        var trimmed = body.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 4000)
        {
            return (false, "Announcement must be between 1 and 4000 characters.");
        }

        var announcement = new CourseAnnouncement
        {
            CourseId = courseId,
            AuthorId = authorId,
            Body = trimmed,
        };
        _db.Set<CourseAnnouncement>().Add(announcement);
        await _db.SaveChangesAsync();

        var course = await _courses.GetByIdAsync(courseId);
        var courseTitle = course?.Title ?? string.Empty;
        var (enrollments, _) = await _enrollments.GetEnrollmentsForRosterAsync(courseId);
        var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();

        await _notifications.CreateForManyAsync(
            studentIds,
            NotificationType.Announcement,
            $"New announcement in {courseTitle}",
            trimmed,
            $"/Courses/Details?id={courseId}");

        return (true, null);
    }

    public Task<List<CourseAnnouncement>> ListForCourseAsync(int courseId, int count = 20)
    {
        return _db.Set<CourseAnnouncement>().AsNoTracking()
                .Where(a => a.CourseId == courseId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
    }

    public Task<CourseAnnouncement?> GetByIdAsync(int id)
    {
        return _db.Set<CourseAnnouncement>().AsNoTracking()
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);
    }
}
