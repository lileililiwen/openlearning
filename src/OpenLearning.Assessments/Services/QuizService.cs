using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.CourseManagement.Models;

namespace OpenLearning.Assessments.Services;

public class QuizService
{
    private readonly DbContext _db;

    public QuizService(DbContext db)
    {
        _db = db;
    }

    public Task<List<Quiz>> GetForCourseAsync(int courseId)
        => _db.Set<Quiz>().AsNoTracking()
            .Where(q => q.CourseId == courseId)
            .OrderBy(q => q.OrderIndex)
            .Include(q => q.Questions)
            .ToListAsync();

    public Task<Quiz?> GetByIdAsync(int id)
        => _db.Set<Quiz>().AsNoTracking()
            .Include(q => q.Course)
            .Include(q => q.Questions.OrderBy(x => x.OrderIndex))
                .ThenInclude(x => x.AnswerOptions.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == id);

    public Task<bool> IsOwnerAsync(int quizId, string userId)
        => _db.Set<Quiz>().AsNoTracking()
            .AnyAsync(q => q.Id == quizId && q.Course!.InstructorId == userId);

    public Task<bool> IsCourseOwnerAsync(int courseId, string userId)
        => _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == userId);

    public async Task<Quiz?> CreateAsync(int courseId, string ownerId, string title, string description)
    {
        if (!await IsCourseOwnerAsync(courseId, ownerId))
        {
            return null;
        }

        var nextOrder = await _db.Set<Quiz>()
            .Where(q => q.CourseId == courseId)
            .Select(q => (int?)q.OrderIndex)
            .MaxAsync() ?? 0;

        var quiz = new Quiz { CourseId = courseId, Title = title, Description = description, OrderIndex = nextOrder + 1 };
        _db.Set<Quiz>().Add(quiz);
        await _db.SaveChangesAsync();
        return quiz;
    }

    public async Task<bool> UpdateAsync(int quizId, string ownerId, string title, string description)
    {
        var quiz = await _db.Set<Quiz>()
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz?.Course is null || quiz.Course.InstructorId != ownerId)
        {
            return false;
        }

        quiz.Title = title;
        quiz.Description = description;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int quizId, string ownerId)
    {
        var quiz = await _db.Set<Quiz>()
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz?.Course is null || quiz.Course.InstructorId != ownerId)
        {
            return false;
        }

        _db.Set<Quiz>().Remove(quiz);
        await _db.SaveChangesAsync();
        return true;
    }
}
