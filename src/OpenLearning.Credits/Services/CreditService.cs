using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Credits.Models;

namespace OpenLearning.Credits.Services;

public class AuditResult
{
    public bool Eligible { get; set; }

    public decimal EarnedTotal { get; set; }

    public Dictionary<CreditCategory, decimal> EarnedByCategory { get; set; } = new();

    public List<string> UnmetRequirements { get; set; } = new();
}

public class CreditService
{
    private readonly DbContext _db;

    public CreditService(DbContext db)
    {
        _db = db;
    }

    public async Task<CreditAward?> AwardAsync(
        string studentId,
        decimal amount,
        CreditCategory category,
        string sourceType,
        string? sourceId,
        int ruleVersion,
        string? reason,
        string actorId)
    {
        if (sourceId != null)
        {
            var existing = await _db.Set<CreditAward>()
                .FirstOrDefaultAsync(a =>
                    a.StudentId == studentId &&
                    a.SourceType == sourceType &&
                    a.SourceId == sourceId);

            if (existing != null && existing.Amount > 0)
            {
                return null;
            }
        }

        var award = new CreditAward
        {
            StudentId = studentId,
            Amount = amount,
            Category = category,
            SourceType = sourceType,
            SourceId = sourceId,
            RuleVersion = ruleVersion,
            Reason = reason,
            ActorId = actorId,
        };

        _db.Set<CreditAward>().Add(award);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException) when (sourceId is not null)
        {
            _db.Entry(award).State = EntityState.Detached;
            if (await _db.Set<CreditAward>().AnyAsync(a => a.StudentId == studentId &&
                a.SourceType == sourceType && a.SourceId == sourceId))
            {
                return null;
            }

            throw;
        }
        return award;
    }

    public async Task<CourseCreditRule> PublishCourseRuleAsync(
        int courseId, decimal amount, CreditCategory category)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Credit amount must be positive.");
        var existing = await _db.Set<CourseCreditRule>().Where(r => r.CourseId == courseId).ToListAsync();
        foreach (var priorRule in existing)
            priorRule.IsActive = false;
        var newRule = new CourseCreditRule
        {
            CourseId = courseId,
            Amount = amount,
            Category = category,
            Version = existing.Select(r => r.Version).DefaultIfEmpty().Max() + 1,
            IsActive = true,
        };
        _db.Add(newRule);
        await _db.SaveChangesAsync();
        return newRule;
    }

    public Task<List<CourseCreditRule>> ListCourseRulesAsync()
    {
        return _db.Set<CourseCreditRule>()
        .AsNoTracking().OrderBy(r => r.CourseId).ThenByDescending(r => r.Version).ToListAsync();
    }

    public async Task<CreditAward?> ProcessCourseCompletionAsync(string studentId, int courseId)
    {
        var rule = await _db.Set<CourseCreditRule>().AsNoTracking()
            .Where(r => r.CourseId == courseId && r.IsActive)
            .OrderByDescending(r => r.Version).FirstOrDefaultAsync();
        return rule is null ? null : await AwardAsync(studentId, rule.Amount, rule.Category,
            "course-completion", courseId.ToString(CultureInfo.InvariantCulture), rule.Version,
            $"Completed course {courseId}", studentId);
    }

    public async Task RevokeAsync(int awardId, string reason, string actorId)
    {
        var original = await _db.Set<CreditAward>().FindAsync(awardId);
        if (original == null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Award {0} not found",
                    awardId));
        }

        if (original.Amount <= 0)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Award {0} has non-positive amount and cannot be revoked",
                    awardId));
        }

        var revoke = new CreditAward
        {
            StudentId = original.StudentId,
            Amount = -original.Amount,
            Category = original.Category,
            SourceType = original.SourceType,
            SourceId = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/revoke/{1}",
                original.SourceId ?? original.Id.ToString(CultureInfo.InvariantCulture),
                original.Id),
            RuleVersion = original.RuleVersion,
            Reason = reason,
            ActorId = actorId,
        };

        _db.Set<CreditAward>().Add(revoke);
        await _db.SaveChangesAsync();
    }

    public Task<List<CreditAward>> GetLedgerAsync(string studentId)
    {
        return _db.Set<CreditAward>()
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .OrderBy(a => a.AwardedAt)
            .ToListAsync();
    }

    public Task<decimal> GetTotalCreditsAsync(string studentId)
    {
        return _db.Set<CreditAward>()
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .SumAsync(a => a.Amount);
    }

    public async Task<Dictionary<CreditCategory, decimal>> GetCreditsByCategoryAsync(string studentId)
    {
        var awards = await _db.Set<CreditAward>()
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .ToListAsync();

        var result = new Dictionary<CreditCategory, decimal>();
        foreach (var award in awards)
        {
            if (result.TryGetValue(award.Category, out var existing))
            {
                result[award.Category] = existing + award.Amount;
            }
            else
            {
                result[award.Category] = award.Amount;
            }
        }

        return result;
    }

    public async Task<GraduationProgram> CreateProgramAsync(
        string name,
        decimal minTotalCredits,
        Dictionary<CreditCategory, decimal> categoryMinimums,
        List<string> requiredCourseIds,
        int? creditExpiryDays = null)
    {
        var maxVersion = await _db.Set<GraduationProgram>()
            .Where(p => p.Name == name)
            .MaxAsync(p => (int?)p.Version) ?? 0;

        var program = new GraduationProgram
        {
            Name = name,
            Version = maxVersion + 1,
            IsActive = true,
            MinTotalCredits = minTotalCredits,
            CategoryMinimums = JsonSerializer.Serialize(categoryMinimums),
            RequiredCourseIds = JsonSerializer.Serialize(requiredCourseIds),
            CreditExpiryDays = creditExpiryDays,
        };

        _db.Set<GraduationProgram>().Add(program);
        await _db.SaveChangesAsync();
        return program;
    }

    public async Task AssignProgramAsync(string studentId, int programId)
    {
        var existing = await _db.Set<LearnerProgram>().FirstOrDefaultAsync(lp => lp.StudentId == studentId);

        if (existing != null)
        {
            existing.ProgramId = programId;
            existing.AssignedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return;
        }

        var learnerProgram = new LearnerProgram
        {
            StudentId = studentId,
            ProgramId = programId,
        };

        _db.Set<LearnerProgram>().Add(learnerProgram);
        await _db.SaveChangesAsync();
    }

    public Task<GraduationProgram?> GetActiveProgramAsync(int programId)
    {
        return _db.Set<GraduationProgram>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == programId && p.IsActive);
    }

    public Task<List<GraduationProgram>> ListProgramsAsync()
    {
        return _db.Set<GraduationProgram>()
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Version)
            .ToListAsync();
    }

    public Task<LearnerProgram?> GetLearnerProgramAsync(string studentId)
    {
        return _db.Set<LearnerProgram>()
            .AsNoTracking()
            .Include(lp => lp.Program)
            .FirstOrDefaultAsync(lp => lp.StudentId == studentId);
    }

    public async Task<AuditResult> EvaluateAsync(string studentId)
    {
        var learnerProgram = await GetLearnerProgramAsync(studentId);
        if (learnerProgram?.Program == null)
        {
            return new AuditResult
            {
                Eligible = false,
                UnmetRequirements = new List<string> { "No program assigned" },
            };
        }

        var program = learnerProgram.Program;
        var cutoff = program.CreditExpiryDays is int days ? DateTime.UtcNow.AddDays(-days) : (DateTime?)null;
        var applicableAwards = await _db.Set<CreditAward>().AsNoTracking()
            .Where(a => a.StudentId == studentId && (cutoff == null || a.AwardedAt >= cutoff)).ToListAsync();
        var earnedTotal = applicableAwards.Sum(a => a.Amount);
        var earnedByCategory = applicableAwards.GroupBy(a => a.Category)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Amount));
        var unmetRequirements = new List<string>();

        if (earnedTotal < program.MinTotalCredits)
        {
            unmetRequirements.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Total credits {0} < {1}",
                earnedTotal,
                program.MinTotalCredits));
        }

        var categoryMinimums = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            program.CategoryMinimums) ?? new Dictionary<string, decimal>();

        foreach (var kvp in categoryMinimums)
        {
            if (Enum.TryParse<CreditCategory>(kvp.Key, ignoreCase: true, out var category))
            {
                earnedByCategory.TryGetValue(category, out var earned);
                if (earned < kvp.Value)
                {
                    unmetRequirements.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} credits {1} < {2}",
                        kvp.Key,
                        earned,
                        kvp.Value));
                }
            }
        }

        var requiredCourseIds = JsonSerializer.Deserialize<List<string>>(
            program.RequiredCourseIds) ?? new List<string>();

        var completedCourseIds = applicableAwards
            .Where(a => a.SourceType == "course-completion" && a.SourceId != null)
            .GroupBy(a => a.SourceId!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Sum(a => a.Amount) > 0).Select(g => g.Key).ToList();

        foreach (var courseId in requiredCourseIds)
        {
            var found = completedCourseIds.Any(c =>
                string.Equals(c, courseId, StringComparison.OrdinalIgnoreCase));

            if (!found)
            {
                unmetRequirements.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "Required course {0} not completed",
                    courseId));
            }
        }

        return new AuditResult
        {
            Eligible = unmetRequirements.Count == 0,
            EarnedTotal = earnedTotal,
            EarnedByCategory = earnedByCategory,
            UnmetRequirements = unmetRequirements,
        };
    }

    public async Task<GraduationDecision> GraduateAsync(
        string studentId,
        int programId,
        string actorId,
        string? notes)
    {
        var learnerProgram = await _db.Set<LearnerProgram>()
            .AsNoTracking()
            .Include(lp => lp.Program)
            .FirstOrDefaultAsync(lp =>
                lp.StudentId == studentId && lp.ProgramId == programId);

        if (learnerProgram == null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Student {0} is not assigned to program {1}",
                    studentId,
                    programId));
        }

        var audit = await EvaluateAsync(studentId);

        if (!audit.Eligible)
        {
            var joined = string.Join(
                "; ",
                audit.UnmetRequirements.Select(r =>
                    string.Format(CultureInfo.InvariantCulture, "{0}", r)));

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Student {0} is not eligible for graduation: {1}",
                    studentId,
                    joined));
        }

        var decision = new GraduationDecision
        {
            StudentId = studentId,
            ProgramId = programId,
            Decision = GraduationDecisionType.Graduated,
            Notes = notes,
            ActorId = actorId,
        };

        _db.Set<GraduationDecision>().Add(decision);
        await _db.SaveChangesAsync();
        return decision;
    }
}
