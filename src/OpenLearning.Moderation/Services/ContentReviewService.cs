using Microsoft.EntityFrameworkCore;
using OpenLearning.Community.Models;
using OpenLearning.Moderation.Models;
using OpenLearning.Ratings.Models;

namespace OpenLearning.Moderation.Services;

/// <summary>Inline preview of a reported item for the admin report queue.</summary>
public sealed record ReportPreview(
    ReportedContentType ContentType,
    int ContentId,
    string AuthorId,
    string AuthorName,
    string Snippet,
    int? CourseId);

/// <summary>
/// Content moderation: users report reviews/comments/Q&amp;A items; admins
/// resolve reports by removing (hides the content everywhere) or dismissing.
/// </summary>
public class ContentReviewService
{
    private readonly DbContext _db;

    public ContentReviewService(DbContext db)
    {
        _db = db;
    }

    /// <summary>Reports content unless there is already an open report for it.</summary>
    public async Task<(bool Ok, string? Error)> ReportAsync(string userId, ReportedContentType contentType, int contentId, string reason)
    {
        var trimmed = (reason ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return (false, "Please provide a reason for the report.");
        }

        var authorId = await GetAuthorIdAsync(contentType, contentId);
        if (authorId is null)
        {
            return (false, "The content you are reporting no longer exists.");
        }

        if (authorId == userId)
        {
            return (false, "You cannot report your own content.");
        }

        var open = await _db.Set<ContentReport>().AnyAsync(r =>
            r.ContentType == contentType && r.ContentId == contentId && r.Resolution == ReportResolution.Pending);
        if (open)
        {
            return (false, "This item was already reported.");
        }

        _db.Set<ContentReport>().Add(new ContentReport
        {
            ContentType = contentType,
            ContentId = contentId,
            ReportedById = userId,
            Reason = trimmed.Length > 1000 ? trimmed[..1000] : trimmed,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Preview of the reported item, or null when it no longer exists.</summary>
    public async Task<ReportPreview?> GetPreviewAsync(ReportedContentType type, int contentId)
    {
        switch (type)
        {
            case ReportedContentType.Review:
                {
                    var review = await _db.Set<Review>()
                        .Include(x => x.User)
                        .FirstOrDefaultAsync(x => x.Id == contentId);
                    if (review is null)
                    {
                        return null;
                    }

                    var snippet = string.IsNullOrWhiteSpace(review.Comment)
                        ? $"(rating: {review.Rating})"
                        : Truncate(review.Comment);
                    return new ReportPreview(type, review.Id, review.UserId,
                        review.User?.DisplayName ?? review.UserId,
                        snippet,
                        review.CourseId);
                }

            case ReportedContentType.ReviewComment:
                {
                    var comment = await _db.Set<ReviewComment>()
                        .Include(x => x.Author)
                        .Include(x => x.Review)
                        .FirstOrDefaultAsync(x => x.Id == contentId);
                    return comment is null
                        ? null
                        : new ReportPreview(type, comment.Id, comment.AuthorId,
                            comment.Author?.DisplayName ?? comment.AuthorId,
                            Truncate(comment.Body),
                            comment.Review?.CourseId);
                }

            case ReportedContentType.Question:
                {
                    var question = await _db.Set<Question>()
                        .Include(x => x.Author)
                        .FirstOrDefaultAsync(x => x.Id == contentId);
                    return question is null
                        ? null
                        : new ReportPreview(type, question.Id, question.AuthorId,
                            question.Author?.DisplayName ?? question.AuthorId,
                            Truncate($"{question.Title}: {question.Body}"),
                            question.CourseId);
                }

            case ReportedContentType.QuestionReply:
                {
                    var reply = await _db.Set<QuestionReply>()
                        .Include(x => x.Author)
                        .Include(x => x.Question)
                        .FirstOrDefaultAsync(x => x.Id == contentId);
                    return reply is null
                        ? null
                        : new ReportPreview(type, reply.Id, reply.AuthorId,
                            reply.Author?.DisplayName ?? reply.AuthorId,
                            Truncate(reply.Body),
                            reply.Question?.CourseId);
                }

            case ReportedContentType.Post:
                {
                    var post = await _db.Set<Post>()
                        .Include(x => x.Author)
                        .FirstOrDefaultAsync(x => x.Id == contentId);
                    return post is null
                        ? null
                        : new ReportPreview(type, post.Id, post.AuthorId,
                            post.Author?.DisplayName ?? post.AuthorId,
                            Truncate(post.Body),
                            post.CourseId);
                }

            case ReportedContentType.PostReply:
                {
                    var reply = await _db.Set<PostReply>()
                        .Include(x => x.Author)
                        .Include(x => x.Post)
                        .FirstOrDefaultAsync(x => x.Id == contentId);
                    return reply is null
                        ? null
                        : new ReportPreview(type, reply.Id, reply.AuthorId,
                            reply.Author?.DisplayName ?? reply.AuthorId,
                            Truncate(reply.Body),
                            reply.Post?.CourseId);
                }

            default:
                return null;
        }
    }

    public Task<List<ContentReport>> GetPendingAsync()
    {
        return _db.Set<ContentReport>().AsNoTracking()
            .Where(r => r.Resolution == ReportResolution.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Admin resolves a report: remove hides the target, dismiss keeps it.</summary>
    public async Task<(bool Ok, string? Error)> ResolveAsync(int reportId, bool remove, string resolverId)
    {
        var report = await _db.Set<ContentReport>().FirstOrDefaultAsync(r => r.Id == reportId);
        if (report is null)
        {
            return (false, "Report not found.");
        }

        if (report.Resolution != ReportResolution.Pending)
        {
            return (false, "This report was already resolved.");
        }

        if (remove)
        {
            await HideContentAsync(report.ContentType, report.ContentId);
            if (!await IsContentHiddenAsync(report.ContentType, report.ContentId))
            {
                return (false, "Target content no longer exists.");
            }
        }

        report.Resolution = remove ? ReportResolution.Removed : ReportResolution.Dismissed;
        report.ResolvedAt = DateTime.UtcNow;
        report.ResolvedById = resolverId;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> IsContentHiddenAsync(ReportedContentType type, int contentId)
    {
        return type switch
        {
            ReportedContentType.Review => await _db.Set<OpenLearning.Ratings.Models.Review>()
                .AnyAsync(x => x.Id == contentId && x.IsHidden),
            ReportedContentType.ReviewComment => await _db.Set<OpenLearning.Ratings.Models.ReviewComment>()
                .AnyAsync(x => x.Id == contentId && x.IsHidden),
            ReportedContentType.Question => await _db.Set<OpenLearning.Community.Models.Question>()
                .AnyAsync(x => x.Id == contentId && x.IsHidden),
            ReportedContentType.QuestionReply => await _db.Set<OpenLearning.Community.Models.QuestionReply>()
                .AnyAsync(x => x.Id == contentId && x.IsHidden),
            ReportedContentType.Post => await _db.Set<OpenLearning.Community.Models.Post>()
                .AnyAsync(x => x.Id == contentId && x.IsHidden),
            ReportedContentType.PostReply => await _db.Set<OpenLearning.Community.Models.PostReply>()
                .AnyAsync(x => x.Id == contentId && x.IsHidden),
            _ => false,
        };
    }

    private async Task HideContentAsync(ReportedContentType type, int contentId)
    {
        switch (type)
        {
            case ReportedContentType.Review:
                await SetHiddenAsync(_db.Set<OpenLearning.Ratings.Models.Review>(), contentId);
                break;
            case ReportedContentType.ReviewComment:
                await SetHiddenAsync(_db.Set<OpenLearning.Ratings.Models.ReviewComment>(), contentId);
                break;
            case ReportedContentType.Question:
                await SetHiddenAsync(_db.Set<OpenLearning.Community.Models.Question>(), contentId);
                break;
            case ReportedContentType.QuestionReply:
                await SetHiddenAsync(_db.Set<OpenLearning.Community.Models.QuestionReply>(), contentId);
                break;
            case ReportedContentType.Post:
                await SetHiddenAsync(_db.Set<OpenLearning.Community.Models.Post>(), contentId);
                break;
            case ReportedContentType.PostReply:
                await SetHiddenAsync(_db.Set<OpenLearning.Community.Models.PostReply>(), contentId);
                break;
        }
    }

    private async Task SetHiddenAsync<T>(DbSet<T> set, int id)
        where T : class
    {
        var entity = await set.FindAsync(id);
        if (entity is null)
        {
            return;
        }

        switch (entity)
        {
            case OpenLearning.Ratings.Models.Review review:
                review.IsHidden = true;
                break;
            case OpenLearning.Ratings.Models.ReviewComment comment:
                comment.IsHidden = true;
                break;
            case OpenLearning.Community.Models.Question question:
                question.IsHidden = true;
                break;
            case OpenLearning.Community.Models.QuestionReply questionReply:
                questionReply.IsHidden = true;
                break;
            case OpenLearning.Community.Models.Post post:
                post.IsHidden = true;
                break;
            case OpenLearning.Community.Models.PostReply postReply:
                postReply.IsHidden = true;
                break;
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>Author id of the reported content, or null when it no longer exists.</summary>
    private async Task<string?> GetAuthorIdAsync(ReportedContentType type, int contentId)
    {
        switch (type)
        {
            case ReportedContentType.Review:
                return await _db.Set<Review>()
                    .Where(x => x.Id == contentId)
                    .Select(x => x.UserId)
                    .FirstOrDefaultAsync();
            case ReportedContentType.ReviewComment:
                return await _db.Set<ReviewComment>()
                    .Where(x => x.Id == contentId)
                    .Select(x => x.AuthorId)
                    .FirstOrDefaultAsync();
            case ReportedContentType.Question:
                return await _db.Set<Question>()
                    .Where(x => x.Id == contentId)
                    .Select(x => x.AuthorId)
                    .FirstOrDefaultAsync();
            case ReportedContentType.QuestionReply:
                return await _db.Set<QuestionReply>()
                    .Where(x => x.Id == contentId)
                    .Select(x => x.AuthorId)
                    .FirstOrDefaultAsync();
            case ReportedContentType.Post:
                return await _db.Set<Post>()
                    .Where(x => x.Id == contentId)
                    .Select(x => x.AuthorId)
                    .FirstOrDefaultAsync();
            case ReportedContentType.PostReply:
                return await _db.Set<PostReply>()
                    .Where(x => x.Id == contentId)
                    .Select(x => x.AuthorId)
                    .FirstOrDefaultAsync();
            default:
                return null;
        }
    }

    private static string Truncate(string? value, int max = 200)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= max ? text : text[..max] + "…";
    }
}
