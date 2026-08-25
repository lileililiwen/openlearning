using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Assessments.Models;
using OpenLearning.Assignments.Models;
using OpenLearning.AsyncIO.Models;
using OpenLearning.AsyncIO.Services;
using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using OpenLearning.Certificates.Models;
using OpenLearning.Classes.Models;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Exams.Models;
using OpenLearning.GradeExport.Models;
using OpenLearning.Logging.Services;
using OpenLearning.Progress.Services;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.GradeExport.Services;

/// <summary>
/// Streaming Excel export of submissions, quiz/exam attempts, and course
/// rosters. Runs synchronously up to <c>grade.export.syncMaxRows</c> (1000);
/// larger exports go through the async-io pipeline as kind
/// <c>grade-export</c>, which delivers an <c>export.ready</c> notification.
/// Ownership is enforced in the SQL WHERE clause, never in C#.
/// </summary>
public class GradeExportService : IAsyncIOProcessor
{
    public const string ExportKind = "grade-export";

    /// <summary>Keyset page size so memory stays bounded on large exports.</summary>
    private const int _batchSize = 1000;

    /// <summary>Platform default quiz pass threshold (matches AttemptService.GetCourseQuizPassRateAsync).</summary>
    private const double _quizPassThreshold = 0.7;

    private readonly DbContext _db;
    private readonly AsyncIOService _asyncIO;
    private readonly ProgressService _progress;
    private readonly IClassAssignmentLookup _classLookup;
    private readonly LogService _logs;

    public GradeExportService(
        DbContext db,
        AsyncIOService asyncIO,
        ProgressService progress,
        IClassAssignmentLookup classLookup,
        LogService logs)
    {
        _db = db;
        _asyncIO = asyncIO;
        _progress = progress;
        _classLookup = classLookup;
        _logs = logs;
    }

    public string Kind => ExportKind;

    public bool NeedsSourceFile => false;

    public async Task<int> CountAsync(GradeExportKind kind, GradeExportFilters filters, string ownerId)
    {
        var query = await BuildQueryAsync(kind, filters, ownerId);
        if (query is null)
        {
            return 0;
        }

        return kind switch
        {
            GradeExportKind.Submissions => await ((IQueryable<AssignmentSubmission>)query).CountAsync(),
            GradeExportKind.QuizAttempts => await ((IQueryable<QuizAttempt>)query).CountAsync(),
            GradeExportKind.ExamAttempts => await ((IQueryable<ExamAttempt>)query).CountAsync(),
            _ => await ((IQueryable<EnrollmentEntity>)query).CountAsync(),
        };
    }

    /// <summary>Builds the workbook synchronously and returns its bytes.</summary>
    public async Task<(byte[]? Bytes, string? Error, int RowCount)> ExportSyncAsync(
        GradeExportKind kind, GradeExportFilters filters, string ownerId)
    {
        using var stream = new MemoryStream();
        var result = await WriteExportAsync(kind, filters, ownerId, stream, null, null, default);
        if (!result.Ok)
        {
            return (null, result.Error, 0);
        }

        stream.Position = 0;
        await _logs.RecordAsync(
            ownerId,
            string.Empty,
            "GradeExport",
            "GradeExport",
            "-",
            BuildAuditDetails(kind, filters, result.RowCount),
            null);
        return (stream.ToArray(), null, result.RowCount);
    }

    /// <summary>Creates an async export job (paired with an AsyncIOJob).</summary>
    public async Task<(int? JobId, string? Error)> SubmitAsync(
        GradeExportKind kind, GradeExportFilters filters, string ownerId)
    {
        // Validate ownership up-front so a denial surfaces immediately rather
        // than after the job queue picks it up.
        var query = await BuildQueryAsync(kind, filters, ownerId);
        if (query is null)
        {
            return (null, "您无权导出该数据。");
        }

        var job = new AsyncIOJob
        {
            UserId = ownerId,
            Kind = ExportKind,
            FileKey = string.Empty,
        };
        var record = new GradeExportJob
        {
            UserId = ownerId,
            Kind = kind,
            FiltersJson = filters.ToJson(),
            Status = GradeExportJobStatus.Pending,
        };
        _db.Set<AsyncIOJob>().Add(job);
        _db.Set<GradeExportJob>().Add(record);
        await _db.SaveChangesAsync();
        record.AsyncIOJobId = job.Id;
        await _db.SaveChangesAsync();
        return (job.Id, null);
    }

    /// <summary>
    /// Async processor for kind <c>grade-export</c>: replays the filters from
    /// the paired record, streams the workbook, stores the result file, and
    /// reports progress at 25/50/75% once the job runs past 5 minutes.
    /// </summary>
    public async Task<(bool Ok, string? Error, int TotalRows, int SuccessRows)> ProcessAsync(
        AsyncIOJob job, Stream? fileStream, CancellationToken cancellationToken)
    {
        var record = await _db.Set<GradeExportJob>()
            .FirstOrDefaultAsync(r => r.AsyncIOJobId == job.Id, cancellationToken);
        if (record is null)
        {
            return (false, "导出任务记录不存在。", 0, 0);
        }

        var filters = GradeExportFilters.FromJson(record.FiltersJson);
        if (filters is null)
        {
            await MarkFailedAsync(record);
            return (false, "导出参数缺失。", 0, 0);
        }

        record.Status = GradeExportJobStatus.Running;
        await _db.SaveChangesAsync(cancellationToken);

        var total = await CountAsync(record.Kind, filters, job.UserId);
        var started = DateTime.UtcNow;
        var reported = new HashSet<int>();
        Func<int, Task>? onProgress = total <= 0
            ? null
            : async percent =>
            {
                if (DateTime.UtcNow - started < TimeSpan.FromMinutes(5))
                {
                    return;
                }

                foreach (var milestone in new[] { 25, 50, 75 })
                {
                    if (percent < milestone || !reported.Add(milestone))
                    {
                        continue;
                    }

                    await _asyncIO.ReportProgressAsync(job.Id, milestone);
                }
            };

        using var stream = new MemoryStream();
        var result = await WriteExportAsync(record.Kind, filters, job.UserId, stream, total, onProgress, cancellationToken);
        if (!result.Ok)
        {
            await MarkFailedAsync(record);
            return (false, result.Error, 0, 0);
        }

        stream.Position = 0;
        await _asyncIO.SetResultAsync(job.Id, $"grade-export-{job.Id}.xlsx", stream);

        var processed = await _db.Set<AsyncIOJob>().AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == job.Id, cancellationToken);
        record.Status = GradeExportJobStatus.Completed;
        record.FileKey = processed?.ResultFileKey;
        record.RowCount = result.RowCount;
        record.FinishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _logs.RecordAsync(
            job.UserId,
            string.Empty,
            "GradeExport",
            "GradeExportJob",
            record.Id.ToString(CultureInfo.InvariantCulture),
            BuildAuditDetails(record.Kind, filters, result.RowCount),
            null);
        return (true, null, result.RowCount, result.RowCount);
    }

    private async Task MarkFailedAsync(GradeExportJob record)
    {
        record.Status = GradeExportJobStatus.Failed;
        record.FinishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ===== Query building (ownership in WHERE) =====

    private Task<IQueryable?> BuildQueryAsync(GradeExportKind kind, GradeExportFilters filters, string ownerId)
    {
        return kind switch
        {
            GradeExportKind.Submissions => BuildSubmissionsQueryAsync(filters, ownerId),
            GradeExportKind.QuizAttempts => BuildQuizAttemptsQueryAsync(filters, ownerId),
            GradeExportKind.ExamAttempts => BuildExamAttemptsQueryAsync(filters, ownerId),
            _ => BuildRosterQueryAsync(filters, ownerId),
        };
    }

    private async Task<IQueryable?> BuildSubmissionsQueryAsync(GradeExportFilters filters, string ownerId)
    {
        if (filters.AssignmentId is not int assignmentId)
        {
            return null;
        }

        var assignment = await _db.Set<Assignment>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment is null)
        {
            return null;
        }

        if (!filters.IsAdmin &&
            !await _db.Set<Course>().AnyAsync(c => c.Id == assignment.CourseId && c.InstructorId == ownerId))
        {
            return null;
        }

        IQueryable<AssignmentSubmission> query = _db.Set<AssignmentSubmission>().AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId);

        if (filters.From is DateTime from)
        {
            query = query.Where(s => s.SubmittedAt >= NormalizeUtc(from));
        }

        if (filters.To is DateTime to)
        {
            query = query.Where(s => s.SubmittedAt < NormalizeUtc(to));
        }

        if (filters.GradedOnly is bool graded)
        {
            query = graded ? query.Where(s => s.GradedAt != null) : query.Where(s => s.GradedAt == null);
        }

        return query;
    }

    private async Task<IQueryable?> BuildQuizAttemptsQueryAsync(GradeExportFilters filters, string ownerId)
    {
        if (filters.QuizId is int quizId)
        {
            var quiz = await _db.Set<Quiz>().AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz is null)
            {
                return null;
            }

            if (!filters.IsAdmin &&
                !await _db.Set<Course>().AnyAsync(c => c.Id == quiz.CourseId && c.InstructorId == ownerId))
            {
                return null;
            }
        }
        else if (filters.CourseId is int courseId)
        {
            if (!filters.IsAdmin &&
                !await _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == ownerId))
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        IQueryable<QuizAttempt> query = _db.Set<QuizAttempt>().AsNoTracking();
        if (filters.QuizId is int quizFilter)
        {
            query = query.Where(a => a.QuizId == quizFilter);
        }
        else
        {
            var courseFilter = filters.CourseId!.Value;
            query = query.Where(a => a.Quiz!.CourseId == courseFilter);
        }

        if (!filters.IsAdmin)
        {
            // Ownership stays in the WHERE clause: the attempt's quiz must belong
            // to a course owned by the exporter.
            query = query.Where(a => _db.Set<Quiz>().Any(q =>
                q.Id == a.QuizId && _db.Set<Course>().Any(c => c.Id == q.CourseId && c.InstructorId == ownerId)));
        }

        if (filters.From is DateTime from)
        {
            query = query.Where(a => a.CompletedAt >= NormalizeUtc(from));
        }

        if (filters.To is DateTime to)
        {
            query = query.Where(a => a.CompletedAt < NormalizeUtc(to));
        }

        return query;
    }

    private async Task<IQueryable?> BuildExamAttemptsQueryAsync(GradeExportFilters filters, string ownerId)
    {
        if (filters.ExamId is not int examId)
        {
            return null;
        }

        var exam = await _db.Set<Exam>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId);
        if (exam is null)
        {
            return null;
        }

        if (!filters.IsAdmin &&
            !await _db.Set<Course>().AnyAsync(c => c.Id == exam.CourseId && c.InstructorId == ownerId))
        {
            return null;
        }

        IQueryable<ExamAttempt> query = _db.Set<ExamAttempt>().AsNoTracking()
            .Where(a => a.ExamId == examId && a.Status == ExamAttemptStatus.Completed);
        if (!filters.IsAdmin)
        {
            // Ownership stays in the WHERE clause: the attempt's exam must belong
            // to a course owned by the exporter.
            query = query.Where(a => _db.Set<Exam>().Any(e =>
                e.Id == a.ExamId && _db.Set<Course>().Any(c => c.Id == e.CourseId && c.InstructorId == ownerId)));
        }

        if (filters.From is DateTime from)
        {
            query = query.Where(a => a.SubmittedAt != null && a.SubmittedAt.Value >= NormalizeUtc(from));
        }

        if (filters.To is DateTime to)
        {
            query = query.Where(a => a.SubmittedAt != null && a.SubmittedAt.Value < NormalizeUtc(to));
        }

        return query;
    }

    private async Task<IQueryable?> BuildRosterQueryAsync(GradeExportFilters filters, string ownerId)
    {
        if (filters.ClassGroupId is int classGroupId)
        {
            if (filters.IsTaScope)
            {
                if (!await _classLookup.IsAssignedAsync(ownerId, classGroupId))
                {
                    return null;
                }
            }
            else if (!filters.IsAdmin &&
                !await _db.Set<ClassGroup>().AnyAsync(cg => cg.Id == classGroupId &&
                    _db.Set<Course>().Any(c => c.Id == cg.CourseId && c.InstructorId == ownerId)))
            {
                return null;
            }
        }
        else if (filters.CourseId is int courseId)
        {
            if (!filters.IsAdmin &&
                !await _db.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == ownerId))
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        IQueryable<EnrollmentEntity> query = _db.Set<EnrollmentEntity>().AsNoTracking();
        if (filters.ClassGroupId is int classFilter)
        {
            query = query.Where(e => e.ClassGroupId == classFilter);
        }
        else
        {
            var courseFilter = filters.CourseId!.Value;
            query = query.Where(e => e.CourseId == courseFilter);
        }

        if (!filters.IsAdmin && filters.ClassGroupId is null)
        {
            // Ownership stays in the WHERE clause: the enrollment's course must
            // be owned by the exporter.
            query = query.Where(e => _db.Set<Course>().Any(c => c.Id == e.CourseId && c.InstructorId == ownerId));
        }

        if (filters.From is DateTime from)
        {
            query = query.Where(e => e.EnrolledAt >= NormalizeUtc(from));
        }

        if (filters.To is DateTime to)
        {
            query = query.Where(e => e.EnrolledAt < NormalizeUtc(to));
        }

        return query;
    }

    // ===== Workbook writers =====

    private sealed record ExportResult(bool Ok, string? Error, int RowCount);

    private async Task<ExportResult> WriteExportAsync(
        GradeExportKind kind,
        GradeExportFilters filters,
        string ownerId,
        Stream target,
        int? totalCount,
        Func<int, Task>? onProgress,
        CancellationToken ct)
    {
        return kind switch
        {
            GradeExportKind.Submissions => await WriteSubmissionsAsync(filters, ownerId, target, totalCount, onProgress, ct),
            GradeExportKind.QuizAttempts => await WriteQuizAttemptsAsync(filters, ownerId, target, totalCount, onProgress, ct),
            GradeExportKind.ExamAttempts => await WriteExamAttemptsAsync(filters, ownerId, target, totalCount, onProgress, ct),
            _ => await WriteRosterAsync(filters, ownerId, target, totalCount, onProgress, ct),
        };
    }

    private async Task<ExportResult> WriteSubmissionsAsync(
        GradeExportFilters filters,
        string ownerId,
        Stream target,
        int? totalCount,
        Func<int, Task>? onProgress,
        CancellationToken ct)
    {
        if (filters.AssignmentId is not int assignmentId)
        {
            return new ExportResult(false, "缺少作业参数。", 0);
        }

        var assignment = await _db.Set<Assignment>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);
        if (assignment is null)
        {
            return new ExportResult(false, "作业不存在。", 0);
        }

        var query = (await BuildSubmissionsQueryAsync(filters, ownerId)) as IQueryable<AssignmentSubmission>;
        if (query is null)
        {
            return new ExportResult(false, "您不是该作业的所有者，无法导出。", 0);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Submissions");
        WriteHeaders(sheet, "StudentEmail", "StudentName", "AssignmentTitle", "SubmittedAt", "Status", "Score", "Feedback", "IsLate");

        var students = new Dictionary<string, (string Name, string Email)>();
        var row = 2;
        var count = 0;
        var lastId = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var batch = await query.Where(s => s.Id > lastId).OrderBy(s => s.Id).Take(_batchSize).ToListAsync(ct);
            if (batch.Count == 0)
            {
                break;
            }

            await LoadStudentsAsync(batch.Select(s => s.StudentId).Distinct(), students, ct);
            foreach (var submission in batch)
            {
                students.TryGetValue(submission.StudentId, out var student);
                sheet.Cell(row, 1).Value = student.Email;
                sheet.Cell(row, 2).Value = student.Name;
                sheet.Cell(row, 3).Value = assignment.Title;
                sheet.Cell(row, 4).Value = submission.SubmittedAt;
                sheet.Cell(row, 5).Value = submission.GradedAt is null ? "Ungraded" : "Graded";
                if (submission.Score is int score)
                {
                    sheet.Cell(row, 6).Value = score;
                }
                else
                {
                    sheet.Cell(row, 6).Value = string.Empty;
                }

                sheet.Cell(row, 7).Value = submission.Feedback ?? string.Empty;
                sheet.Cell(row, 8).Value = assignment.DueAt is DateTime due && submission.SubmittedAt > due ? "Yes" : "No";
                row++;
            }

            count += batch.Count;
            lastId = batch[^1].Id;
            await ReportProgressAsync(onProgress, totalCount, count);
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(target);
        return new ExportResult(true, null, count);
    }

    private async Task<ExportResult> WriteQuizAttemptsAsync(
        GradeExportFilters filters,
        string ownerId,
        Stream target,
        int? totalCount,
        Func<int, Task>? onProgress,
        CancellationToken ct)
    {
        var query = (await BuildQuizAttemptsQueryAsync(filters, ownerId)) as IQueryable<QuizAttempt>;
        if (query is null)
        {
            return new ExportResult(false, "您不是该测验的所有者，无法导出。", 0);
        }

        var quizQuery = _db.Set<Quiz>().AsNoTracking();
        if (filters.QuizId is int quizFilter)
        {
            quizQuery = quizQuery.Where(q => q.Id == quizFilter);
        }
        else if (filters.CourseId is int courseFilter)
        {
            quizQuery = quizQuery.Where(q => q.CourseId == courseFilter);
        }
        else
        {
            return new ExportResult(false, "缺少测验参数。", 0);
        }

        var quizIds = await quizQuery.ToDictionaryAsync(q => q.Id, q => q.Title, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attempts");
        WriteHeaders(sheet, "StudentEmail", "StudentName", "QuizTitle", "AttemptedAt", "ScorePercent", "Passed", "PerQuestionJson");

        var students = new Dictionary<string, (string Name, string Email)>();
        var row = 2;
        var count = 0;
        var lastId = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var batch = await query.Where(a => a.Id > lastId).OrderBy(a => a.Id).Take(_batchSize).ToListAsync(ct);
            if (batch.Count == 0)
            {
                break;
            }

            await LoadStudentsAsync(batch.Select(a => a.StudentId).Distinct(), students, ct);
            var answers = await LoadAttemptAnswersAsync(batch.Select(a => a.Id), ct);
            foreach (var attempt in batch)
            {
                students.TryGetValue(attempt.StudentId, out var student);
                var percent = attempt.MaxScore > 0 ? (int)Math.Round(attempt.Score * 100.0 / attempt.MaxScore) : 0;
                sheet.Cell(row, 1).Value = student.Email;
                sheet.Cell(row, 2).Value = student.Name;
                sheet.Cell(row, 3).Value = quizIds.GetValueOrDefault(attempt.QuizId) ?? string.Empty;
                sheet.Cell(row, 4).Value = attempt.CompletedAt;
                sheet.Cell(row, 5).Value = percent;
                sheet.Cell(row, 6).Value = percent >= _quizPassThreshold * 100 ? "Yes" : "No";
                sheet.Cell(row, 7).Value = BuildPerQuestionJson(answers.GetValueOrDefault(attempt.Id));
                row++;
            }

            count += batch.Count;
            lastId = batch[^1].Id;
            await ReportProgressAsync(onProgress, totalCount, count);
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(target);
        return new ExportResult(true, null, count);
    }

    private async Task<ExportResult> WriteExamAttemptsAsync(
        GradeExportFilters filters,
        string ownerId,
        Stream target,
        int? totalCount,
        Func<int, Task>? onProgress,
        CancellationToken ct)
    {
        if (filters.ExamId is not int examId)
        {
            return new ExportResult(false, "缺少考试参数。", 0);
        }

        var exam = await _db.Set<Exam>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId, ct);
        var query = (await BuildExamAttemptsQueryAsync(filters, ownerId)) as IQueryable<ExamAttempt>;
        if (query is null)
        {
            return new ExportResult(false, "您不是该考试的所有者，无法导出。", 0);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attempts");
        WriteHeaders(sheet, "StudentEmail", "StudentName", "ExamTitle", "StartedAt", "SubmittedAt", "ScorePercent", "Passed", "ScreenSwitchCount", "PerQuestionJson");

        var students = new Dictionary<string, (string Name, string Email)>();
        var row = 2;
        var count = 0;
        var lastId = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var batch = await query.Where(a => a.Id > lastId).OrderBy(a => a.Id).Take(_batchSize).ToListAsync(ct);
            if (batch.Count == 0)
            {
                break;
            }

            await LoadStudentsAsync(batch.Select(a => a.StudentId).Distinct(), students, ct);
            var answers = await LoadExamAnswersAsync(batch.Select(a => a.Id), ct);
            foreach (var attempt in batch)
            {
                students.TryGetValue(attempt.StudentId, out var student);
                sheet.Cell(row, 1).Value = student.Email;
                sheet.Cell(row, 2).Value = student.Name;
                sheet.Cell(row, 3).Value = exam?.Title ?? string.Empty;
                sheet.Cell(row, 4).Value = attempt.StartedAt;
                if (attempt.SubmittedAt is DateTime submitted)
                {
                    sheet.Cell(row, 5).Value = submitted;
                }
                else
                {
                    sheet.Cell(row, 5).Value = string.Empty;
                }

                sheet.Cell(row, 6).Value = attempt.Percent;
                sheet.Cell(row, 7).Value = attempt.Passed ? "Yes" : "No";
                sheet.Cell(row, 8).Value = attempt.ScreenSwitchCount;
                sheet.Cell(row, 9).Value = BuildPerQuestionJson(answers.GetValueOrDefault(attempt.Id));
                row++;
            }

            count += batch.Count;
            lastId = batch[^1].Id;
            await ReportProgressAsync(onProgress, totalCount, count);
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(target);
        return new ExportResult(true, null, count);
    }

    private async Task<ExportResult> WriteRosterAsync(
        GradeExportFilters filters,
        string ownerId,
        Stream target,
        int? totalCount,
        Func<int, Task>? onProgress,
        CancellationToken ct)
    {
        var query = (await BuildRosterQueryAsync(filters, ownerId)) as IQueryable<EnrollmentEntity>;
        if (query is null)
        {
            return new ExportResult(false, "您无权导出该班级/课程的花名册。", 0);
        }

        var rosterCourseId = filters.CourseId;
        if (rosterCourseId is null && filters.ClassGroupId is int classGroupId)
        {
            var classGroup = await _db.Set<ClassGroup>().AsNoTracking()
                .FirstOrDefaultAsync(cg => cg.Id == classGroupId, ct);
            rosterCourseId = classGroup?.CourseId;
        }

        var totalLessons = rosterCourseId is int courseId
            ? await _db.Set<Module>().AsNoTracking()
                .Where(m => m.CourseId == courseId)
                .SelectMany(m => m.Lessons)
                .CountAsync(ct)
            : 0;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Roster");
        WriteHeaders(sheet, "StudentEmail", "StudentName", "EnrolledAt", "LastActivityAt", "ProgressPercent", "FinalScore", "CertificateNumber");

        var students = new Dictionary<string, (string Name, string Email)>();
        var row = 2;
        var count = 0;
        var lastId = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var batch = await query.Where(e => e.Id > lastId).OrderBy(e => e.Id).Take(_batchSize).ToListAsync(ct);
            if (batch.Count == 0)
            {
                break;
            }

            var batchIds = batch.Select(e => e.Id).ToList();
            var batchStudentIds = batch.Select(e => e.StudentId).Distinct().ToList();
            await LoadStudentsAsync(batchStudentIds, students, ct);
            var (completedByEnrollment, lastAccessByEnrollment) = await _progress.GetEnrollmentProgressMapAsync(batchIds);
            var certificates = await _db.Set<Certificate>().AsNoTracking()
                .Where(c => batchIds.Contains(c.EnrollmentId))
                .Select(c => new { c.EnrollmentId, c.Code })
                .ToListAsync(ct);
            var certificateByEnrollment = certificates.ToDictionary(c => c.EnrollmentId, c => c.Code);
            var finalScoreByStudent = await ComputeFinalScoresAsync(batchStudentIds, rosterCourseId, ct);

            foreach (var enrollment in batch)
            {
                students.TryGetValue(enrollment.StudentId, out var student);
                sheet.Cell(row, 1).Value = student.Email;
                sheet.Cell(row, 2).Value = student.Name;
                sheet.Cell(row, 3).Value = enrollment.EnrolledAt;
                if (lastAccessByEnrollment.TryGetValue(enrollment.Id, out var lastAccess))
                {
                    sheet.Cell(row, 4).Value = lastAccess;
                }
                else
                {
                    sheet.Cell(row, 4).Value = string.Empty;
                }

                var completed = completedByEnrollment.GetValueOrDefault(enrollment.Id);
                sheet.Cell(row, 5).Value = totalLessons == 0 ? 0 : (int)Math.Round(completed * 100.0 / totalLessons);
                if (finalScoreByStudent.TryGetValue(enrollment.StudentId, out var finalScore))
                {
                    sheet.Cell(row, 6).Value = finalScore;
                }
                else
                {
                    sheet.Cell(row, 6).Value = string.Empty;
                }

                sheet.Cell(row, 7).Value = certificateByEnrollment.GetValueOrDefault(enrollment.Id, string.Empty);
                row++;
            }

            count += batch.Count;
            lastId = batch[^1].Id;
            await ReportProgressAsync(onProgress, totalCount, count);
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(target);
        return new ExportResult(true, null, count);
    }

    /// <summary>
    /// Average percent across completed quiz and exam attempts in the course
    /// (the learner's overall course grade); empty when nothing was attempted.
    /// </summary>
    private async Task<Dictionary<string, double>> ComputeFinalScoresAsync(
        List<string> studentIds, int? courseId, CancellationToken ct)
    {
        if (studentIds.Count == 0 || courseId is not int course)
        {
            return new Dictionary<string, double>();
        }

        var quizPercents = await _db.Set<QuizAttempt>().AsNoTracking()
            .Where(a => studentIds.Contains(a.StudentId) && a.Quiz!.CourseId == course && a.MaxScore > 0)
            .Select(a => new { a.StudentId, Percent = a.Score * 100.0 / a.MaxScore })
            .ToListAsync(ct);
        var examPercents = await _db.Set<ExamAttempt>().AsNoTracking()
            .Where(a => studentIds.Contains(a.StudentId) && a.Exam!.CourseId == course && a.Status == ExamAttemptStatus.Completed)
            .Select(a => new { a.StudentId, Percent = (double)a.Percent })
            .ToListAsync(ct);

        return quizPercents
            .Concat(examPercents)
            .GroupBy(x => x.StudentId)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(x => x.Percent), 1));
    }

    // ===== Shared helpers =====

    private async Task LoadStudentsAsync(
        IEnumerable<string> studentIds,
        Dictionary<string, (string Name, string Email)> students,
        CancellationToken ct)
    {
        var missing = studentIds.Where(id => !students.ContainsKey(id)).Distinct().ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var users = await _db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => missing.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToListAsync(ct);
        foreach (var user in users)
        {
            students[user.Id] = (user.DisplayName ?? string.Empty, user.Email ?? string.Empty);
        }
    }

    private async Task<Dictionary<int, List<QuizAttemptAnswer>>> LoadAttemptAnswersAsync(IEnumerable<int> attemptIds, CancellationToken ct)
    {
        var ids = attemptIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, List<QuizAttemptAnswer>>();
        }

        var answers = await _db.Set<QuizAttemptAnswer>().AsNoTracking()
            .Where(a => ids.Contains(a.AttemptId))
            .Include(a => a.Question)!
                .ThenInclude(q => q!.AnswerOptions)
            .Include(a => a.AnswerOption)
            .ToListAsync(ct);
        return answers.GroupBy(a => a.AttemptId).ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<int, List<ExamAttemptAnswer>>> LoadExamAnswersAsync(IEnumerable<int> attemptIds, CancellationToken ct)
    {
        var ids = attemptIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, List<ExamAttemptAnswer>>();
        }

        var answers = await _db.Set<ExamAttemptAnswer>().AsNoTracking()
            .Where(a => ids.Contains(a.AttemptId))
            .Include(a => a.Question)!
                .ThenInclude(q => q!.AnswerOptions)
            .Include(a => a.AnswerOption)
            .ToListAsync(ct);
        return answers.GroupBy(a => a.AttemptId).ToDictionary(g => g.Key, g => g.ToList());
    }

    private static string BuildPerQuestionJson(List<QuizAttemptAnswer>? answers)
    {
        if (answers is null || answers.Count == 0)
        {
            return string.Empty;
        }

        var items = answers
            .OrderBy(a => a.Question?.OrderIndex)
            .Select(a => new
            {
                q = a.Question?.OrderIndex ?? 0,
                question = a.Question?.Text ?? string.Empty,
                type = a.Question?.QuestionType.ToString() ?? string.Empty,
                answer = RenderAnswer(a),
                points = a.Question?.Points ?? 0,
                correct = a.IsCorrect,
                graded = a.IsGraded,
                score = a.GradedScore,
            });
        return JsonSerializer.Serialize(items);
    }

    private static string BuildPerQuestionJson(List<ExamAttemptAnswer>? answers)
    {
        if (answers is null || answers.Count == 0)
        {
            return string.Empty;
        }

        var items = answers
            .OrderBy(a => a.Question?.OrderIndex)
            .Select(a => new
            {
                q = a.Question?.OrderIndex ?? 0,
                question = a.Question?.Text ?? string.Empty,
                type = a.Question?.QuestionType.ToString() ?? string.Empty,
                answer = RenderAnswer(a),
                points = a.Question?.Points ?? 0,
                correct = a.IsCorrect,
                graded = a.IsGraded,
                score = a.GradedScore,
            });
        return JsonSerializer.Serialize(items);
    }

    private static string RenderAnswer(QuizAttemptAnswer answer)
    {
        var text = answer.Question?.QuestionType switch
        {
            QuestionType.SingleChoice or QuestionType.TrueFalse => answer.AnswerOption?.Text ?? string.Empty,
            QuestionType.MultipleChoice => RenderMultipleChoice(answer),
            QuestionType.FillBlank => answer.TextAnswer ?? string.Empty,
            QuestionType.ShortAnswer => answer.TextAnswer ?? string.Empty,
            QuestionType.FileUpload => answer.FileAnswerUrl ?? string.Empty,
            _ => string.Empty,
        };
        return text;
    }

    private static string RenderAnswer(ExamAttemptAnswer answer)
    {
        var text = answer.Question?.QuestionType switch
        {
            QuestionType.SingleChoice or QuestionType.TrueFalse => answer.AnswerOption?.Text ?? string.Empty,
            QuestionType.MultipleChoice => RenderMultipleChoice(answer),
            QuestionType.FillBlank => answer.TextAnswer ?? string.Empty,
            QuestionType.ShortAnswer => answer.TextAnswer ?? string.Empty,
            QuestionType.FileUpload => answer.FileAnswerUrl ?? string.Empty,
            _ => string.Empty,
        };
        return text;
    }

    private static string RenderMultipleChoice(QuizAttemptAnswer answer)
    {
        if (string.IsNullOrWhiteSpace(answer.SelectedOptionIds) || answer.Question is null)
        {
            return string.Empty;
        }

        var selected = answer.SelectedOptionIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0)
            .ToHashSet();
        return string.Join("; ", answer.Question.AnswerOptions.Where(o => selected.Contains(o.Id)).Select(o => o.Text));
    }

    private static string RenderMultipleChoice(ExamAttemptAnswer answer)
    {
        if (string.IsNullOrWhiteSpace(answer.SelectedOptionIds) || answer.Question is null)
        {
            return string.Empty;
        }

        var selected = answer.SelectedOptionIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0)
            .ToHashSet();
        return string.Join("; ", answer.Question.AnswerOptions.Where(o => selected.Contains(o.Id)).Select(o => o.Text));
    }

    private static void WriteHeaders(IXLWorksheet sheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }
    }

    private static async Task ReportProgressAsync(Func<int, Task>? onProgress, int? totalCount, int processed)
    {
        if (onProgress is not null && totalCount is int total && total > 0)
        {
            await onProgress((int)Math.Round(processed * 100.0 / total));
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime(),
        };
    }

    private static string BuildAuditDetails(GradeExportKind kind, GradeExportFilters filters, int rowCount)
    {
        return $"kind={kind}, course={filters.CourseId}, assignment={filters.AssignmentId}, "
            + $"quiz={filters.QuizId}, exam={filters.ExamId}, class={filters.ClassGroupId}, rows={rowCount}";
    }
}
