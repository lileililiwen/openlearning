using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth.Models;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.Classes.Services;

/// <summary>One row of the class roster.</summary>
public sealed record ClassRosterRow(
    int EnrollmentId,
    string StudentId,
    string StudentName,
    string StudentEmail,
    DateTime EnrolledAt);

/// <summary>Enrolled students of a class group.</summary>
public class ClassRosterService
{
    private readonly DbContext _db;

    public ClassRosterService(DbContext db)
    {
        _db = db;
    }

    public async Task<List<ClassRosterRow>> GetRosterAsync(int classGroupId)
    {
        var rows = await _db.Set<EnrollmentEntity>().AsNoTracking()
            .Where(e => e.ClassGroupId == classGroupId)
            .Select(e => new
            {
                e.Id,
                e.StudentId,
                e.EnrolledAt,
                StudentName = e.Student != null ? e.Student.DisplayName : string.Empty,
                StudentEmail = e.Student != null ? e.Student.Email : string.Empty,
            })
            .ToListAsync();
        return rows
            .Select(r => new ClassRosterRow(r.Id, r.StudentId, r.StudentName, r.StudentEmail ?? string.Empty, r.EnrolledAt))
            .OrderBy(r => r.StudentName)
            .ToList();
    }
}
