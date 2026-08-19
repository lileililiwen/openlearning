using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using OpenLearning.Community.Models;
using OpenLearning.CourseManagement.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Community.Services;

/// <summary>
/// Course Q&amp;A and community posts. Reads/writes require enrollment, course
/// ownership, or admin; posts can be class-scoped (ClassGroupId) and are then
/// visible only to members of that class.
/// </summary>
public class CommunityService
{
    private readonly DbContext _db;

    public CommunityService(DbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanAccessAsync(int courseId, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        return await _db.Set<EnrollmentEntity>().AnyAsync(e => e.StudentId == userId && e.CourseId == courseId)
            || await _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }

    public async Task<(bool Ok, string? Error)> AskAsync(int courseId, string userId, string title, string body, int? classGroupId, bool isAdmin)
    {
        if (!await CanAccessAsync(courseId, userId, isAdmin))
        {
            return (false, "You must be enrolled in this course to ask a question.");
        }

        _db.Set<Question>().Add(new Question
        {
            CourseId = courseId,
            AuthorId = userId,
            Title = title.Trim(),
            Body = body.Trim(),
            ClassGroupId = classGroupId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReplyToQuestionAsync(int questionId, string userId, string body, bool isAdmin)
    {
        var question = await _db.Set<Question>()
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == questionId);
        if (question is null)
        {
            return (false, "Question not found.");
        }

        if (!await CanAccessAsync(question.CourseId, userId, isAdmin))
        {
            return (false, "You must be enrolled in this course to reply.");
        }

        if (await _db.Set<QuestionReply>().AnyAsync(r =>
                r.QuestionId == questionId && r.AuthorId == userId && r.Body == body.Trim()))
        {
            return (false, "You already posted this exact reply.");
        }

        _db.Set<QuestionReply>().Add(new QuestionReply
        {
            QuestionId = questionId,
            AuthorId = userId,
            Body = body.Trim(),
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CreatePostAsync(int courseId, string userId, string body, int? classGroupId, bool isAdmin)
    {
        if (!await CanAccessAsync(courseId, userId, isAdmin))
        {
            return (false, "You must be enrolled in this course to post.");
        }

        _db.Set<Post>().Add(new Post
        {
            CourseId = courseId,
            AuthorId = userId,
            Body = body.Trim(),
            ClassGroupId = classGroupId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReplyToPostAsync(int postId, string userId, string body, bool isAdmin)
    {
        var post = await _db.Set<Post>()
            .Include(p => p.Course)
            .FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null)
        {
            return (false, "Post not found.");
        }

        if (!await CanAccessAsync(post.CourseId, userId, isAdmin))
        {
            return (false, "You must be enrolled in this course to reply.");
        }

        if (await _db.Set<PostReply>().AnyAsync(r =>
                r.PostId == postId && r.AuthorId == userId && r.Body == body.Trim()))
        {
            return (false, "You already posted this exact reply.");
        }

        _db.Set<PostReply>().Add(new PostReply
        {
            PostId = postId,
            AuthorId = userId,
            Body = body.Trim(),
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Questions visible to the user: course-wide plus the user's class groups in the course.</summary>
    public async Task<List<Question>> GetQuestionsAsync(int courseId, string? userId, bool isAdmin)
    {
        List<Question> questions;
        if (isAdmin)
        {
            questions = await _db.Set<Question>().AsNoTracking()
                .Include(q => q.Author)
                .Include(q => q.Replies).ThenInclude(r => r.Author)
                .Where(q => q.CourseId == courseId && !q.IsHidden)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }
        else
        {
            var classIds = await GetUserClassIdsAsync(userId, courseId);
            questions = await _db.Set<Question>().AsNoTracking()
                .Include(q => q.Author)
                .Include(q => q.Replies).ThenInclude(r => r.Author)
                .Where(q => q.CourseId == courseId && !q.IsHidden
                    && (q.ClassGroupId == null || (userId != null && classIds.Contains(q.ClassGroupId.Value))))
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        foreach (var question in questions)
        {
            question.Replies = question.Replies.Where(r => !r.IsHidden).OrderBy(r => r.CreatedAt).ToList();
        }

        return questions;
    }

    /// <summary>Posts visible to the user: course-wide plus the user's class groups in the course.</summary>
    public async Task<List<Post>> GetPostsAsync(int courseId, string? userId, bool isAdmin)
    {
        List<Post> posts;
        if (isAdmin)
        {
            posts = await _db.Set<Post>().AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Replies).ThenInclude(r => r.Author)
                .Where(p => p.CourseId == courseId && !p.IsHidden)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        else
        {
            var classIds = await GetUserClassIdsAsync(userId, courseId);
            posts = await _db.Set<Post>().AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Replies).ThenInclude(r => r.Author)
                .Where(p => p.CourseId == courseId && !p.IsHidden
                    && (p.ClassGroupId == null || (userId != null && classIds.Contains(p.ClassGroupId.Value))))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        foreach (var post in posts)
        {
            post.Replies = post.Replies.Where(r => !r.IsHidden).OrderBy(r => r.CreatedAt).ToList();
        }

        return posts;
    }

    public async Task<bool> IsOwnerAsync(int courseId, string userId)
    {
        return await _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }

    // ---- Admin moderation hooks (content-review plugs in later) ----

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        var question = await _db.Set<Question>().FirstOrDefaultAsync(q => q.Id == questionId);
        if (question is null)
        {
            return false;
        }

        _db.Set<Question>().Remove(question);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuestionReplyAsync(int replyId)
    {
        var reply = await _db.Set<QuestionReply>().FirstOrDefaultAsync(r => r.Id == replyId);
        if (reply is null)
        {
            return false;
        }

        _db.Set<QuestionReply>().Remove(reply);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePostAsync(int postId)
    {
        var post = await _db.Set<Post>().FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null)
        {
            return false;
        }

        _db.Set<Post>().Remove(post);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePostReplyAsync(int replyId)
    {
        var reply = await _db.Set<PostReply>().FirstOrDefaultAsync(r => r.Id == replyId);
        if (reply is null)
        {
            return false;
        }

        _db.Set<PostReply>().Remove(reply);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Class group ids the user belongs to in a course (via enrollment).</summary>
    private async Task<List<int>> GetUserClassIdsAsync(string? userId, int courseId)
    {
        if (userId is null)
        {
            return new List<int>();
        }

        return await _db.Set<EnrollmentEntity>()
            .Where(e => e.StudentId == userId && e.CourseId == courseId && e.ClassGroupId != null)
            .Select(e => e.ClassGroupId!.Value)
            .Distinct()
            .ToListAsync();
    }
}
