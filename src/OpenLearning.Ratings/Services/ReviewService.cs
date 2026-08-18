using Microsoft.EntityFrameworkCore;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Ratings.Models;

namespace OpenLearning.Ratings.Services;

/// <summary>Average rating (0 when no reviews) and total review count.</summary>
public sealed record RatingAggregate(double Average, int Count);

/// <summary>One review row with author display info for owner/admin views.</summary>
public sealed record ReviewWithAuthor(
    int Id,
    string UserId,
    string AuthorName,
    string? AuthorEmail,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

public class ReviewService
{
    private readonly DbContext _db;
    private readonly EnrollmentService _enrollments;

    public ReviewService(DbContext db, EnrollmentService enrollments)
    {
        _db = db;
        _enrollments = enrollments;
    }

    /// <summary>
    /// Enrolled students can submit (or replace) their review. Re-submitting
    /// overwrites the prior row instead of creating a new one. Only active
    /// enrollments may rate, mirroring the purchase/learning requirement.
    /// </summary>
    public async Task<(bool Ok, string? Error)> SubmitAsync(
        string userId, int courseId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
        {
            return (false, "Rating must be between 1 and 5.");
        }

        var course = await _db.Set<Course>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return (false, "Course not found.");
        }

        if (!await _enrollments.IsEnrolledAsync(userId, courseId))
        {
            return (false, "You must be enrolled in this course to leave a review.");
        }

        var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmedComment is { Length: > 2000 })
        {
            return (false, "Comment must be 2000 characters or fewer.");
        }

        var existing = await _db.Set<Review>()
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);
        if (existing is not null)
        {
            existing.Rating = rating;
            existing.Comment = trimmedComment;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.Set<Review>().Add(new Review
            {
                CourseId = courseId,
                UserId = userId,
                Rating = rating,
                Comment = trimmedComment,
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Average + count for a single course.</summary>
    public async Task<RatingAggregate> GetRatingAsync(int courseId)
    {
        var grouped = await _db.Set<Review>().AsNoTracking()
            .Where(r => r.CourseId == courseId)
            .GroupBy(_ => 1)
            .Select(g => new { Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync();
        return grouped is null
            ? new RatingAggregate(0d, 0)
            : new RatingAggregate(grouped.Average, grouped.Count);
    }

    /// <summary>Average + count for a set of course ids. Empty input → empty map.</summary>
    public async Task<Dictionary<int, RatingAggregate>> GetRatingsAsync(IEnumerable<int> courseIds)
    {
        var ids = courseIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, RatingAggregate>();
        }

        var grouped = await _db.Set<Review>().AsNoTracking()
            .Where(r => ids.Contains(r.CourseId))
            .GroupBy(r => r.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                Average = g.Average(r => (double)r.Rating),
                Count = g.Count(),
            })
            .ToListAsync();
        return grouped.ToDictionary(g => g.CourseId, g => new RatingAggregate(g.Average, g.Count));
    }

    /// <summary>All reviews for a course, newest first, joined with author info.</summary>
    public async Task<List<ReviewWithAuthor>> GetReviewsForCourseAsync(int courseId)
    {
        return await _db.Set<Review>().AsNoTracking()
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewWithAuthor(
                r.Id,
                r.UserId,
                r.User!.DisplayName,
                r.User!.Email,
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToListAsync();
    }

    /// <summary>The current user's review (if any) for a course — to pre-fill the form.</summary>
    public Task<Review?> GetUserReviewAsync(string userId, int courseId)
        => _db.Set<Review>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);

    /// <summary>Admin-only moderation: delete a review by id.</summary>
    public async Task<bool> DeleteAsync(int reviewId)
    {
        var review = await _db.Set<Review>().FindAsync(reviewId);
        if (review is null)
        {
            return false;
        }

        _db.Set<Review>().Remove(review);
        await _db.SaveChangesAsync();
        return true;
    }
}
