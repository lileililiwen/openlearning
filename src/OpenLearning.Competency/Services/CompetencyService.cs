using Microsoft.EntityFrameworkCore;
using OpenLearning.Assignments.Services;
using OpenLearning.Auth.Services;
using OpenLearning.Competency.Models;
using OpenLearning.Enrollment.Services;
using OpenLearning.Progress.Services;

namespace OpenLearning.Competency.Services;

/// <summary>
/// Versioned competency frameworks, activity-to-competency mappings, idempotent
/// automatic evidence from trusted completions, manual evidence approval, and
/// gap analysis. Attainment is strictly separate from grades, credits,
/// graduation, certificates, and payments.
/// </summary>
public class CompetencyService
{
    private readonly DbContext _db;
    private readonly ProgressService _progress;
    private readonly AssignmentService _assignments;
    private readonly EnrollmentService _enrollments;
    private readonly UserService _users;

    public CompetencyService(
        DbContext db,
        ProgressService progress,
        AssignmentService assignments,
        EnrollmentService enrollments,
        UserService users)
    {
        _db = db;
        _progress = progress;
        _assignments = assignments;
        _enrollments = enrollments;
        _users = users;
    }

    // ===== Framework management (Admin) =====

    public Task<List<CompetencyFramework>> ListFrameworksAsync(bool includeArchived)
    {
        return _db.Set<CompetencyFramework>().AsNoTracking()
            .Include(f => f.ScaleLevels)
            .Where(f => includeArchived || !f.IsArchived)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public Task<CompetencyFramework?> GetFrameworkAsync(int id)
    {
        return _db.Set<CompetencyFramework>().AsNoTracking()
            .Include(f => f.ScaleLevels.OrderBy(l => l.SortOrder))
            .Include(f => f.Competencies.OrderBy(c => c.SortOrder))
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<(bool Ok, string? Error)> CreateFrameworkAsync(
        string name, string description, IReadOnlyList<string> scaleLabels, string adminId)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 200)
        {
            return (false, "Framework name is required (200 characters or fewer).");
        }

        var labels = scaleLabels
            .Select(l => l?.Trim() ?? string.Empty)
            .Where(l => l.Length > 0)
            .ToList();
        if (labels.Count is < 2 or > 10)
        {
            return (false, "Provide between 2 and 10 scale level labels.");
        }

        var framework = new CompetencyFramework
        {
            Name = trimmed,
            Description = description?.Trim() ?? string.Empty,
        };
        for (var i = 0; i < labels.Count; i++)
        {
            framework.ScaleLevels.Add(new FrameworkScaleLevel { SortOrder = i + 1, Label = labels[i] });
        }

        _db.Set<CompetencyFramework>().Add(framework);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> EditFrameworkAsync(
        int id, string name, string description)
    {
        var framework = await _db.Set<CompetencyFramework>().FindAsync(id);
        if (framework is null)
        {
            return (false, "Framework not found.");
        }

        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 200)
        {
            return (false, "Framework name is required (200 characters or fewer).");
        }

        framework.Name = trimmed;
        framework.Description = description?.Trim() ?? string.Empty;
        await BumpVersionIfNeededAsync(id);
        framework.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetArchivedAsync(int id, bool archived)
    {
        var framework = await _db.Set<CompetencyFramework>().FindAsync(id);
        if (framework is null)
        {
            return (false, "Framework not found.");
        }

        framework.IsArchived = archived;
        framework.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> AddCompetencyAsync(
        int frameworkId, int? parentId, string title, string description)
    {
        var framework = await _db.Set<CompetencyFramework>()
            .Include(f => f.Competencies)
            .FirstOrDefaultAsync(f => f.Id == frameworkId);
        if (framework is null)
        {
            return (false, "Framework not found.");
        }

        if (framework.IsArchived)
        {
            return (false, "Archived frameworks cannot be edited.");
        }

        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 200)
        {
            return (false, "Competency title is required (200 characters or fewer).");
        }

        if (parentId is not null &&
            framework.Competencies.All(c => c.Id != parentId))
        {
            return (false, "Parent competency not found in this framework.");
        }

        var maxOrder = framework.Competencies.Count == 0 ? 0 : framework.Competencies.Max(c => c.SortOrder);
        framework.Competencies.Add(new CompetencyNode
        {
            ParentId = parentId,
            Title = trimmed,
            Description = description?.Trim() ?? string.Empty,
            SortOrder = maxOrder + 1,
        });
        await BumpVersionIfNeededAsync(frameworkId);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateCompetencyAsync(
        int nodeId, string title, string description)
    {
        var node = await _db.Set<CompetencyNode>().FindAsync(nodeId);
        if (node is null)
        {
            return (false, "Competency not found.");
        }

        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 200)
        {
            return (false, "Competency title is required (200 characters or fewer).");
        }

        node.Title = trimmed;
        node.Description = description?.Trim() ?? string.Empty;
        await BumpVersionIfNeededAsync(node.FrameworkId);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteCompetencyAsync(int nodeId)
    {
        var node = await _db.Set<CompetencyNode>().FindAsync(nodeId);
        if (node is null)
        {
            return (false, "Competency not found.");
        }

        var hasChildren = await _db.Set<CompetencyNode>().AnyAsync(c => c.ParentId == nodeId);
        if (hasChildren)
        {
            return (false, "Delete child competencies first.");
        }

        var hasMappings = await _db.Set<ActivityMapping>().AnyAsync(m => m.CompetencyId == nodeId);
        var hasEvidence = await _db.Set<CompetencyEvidence>().AnyAsync(e => e.CompetencyId == nodeId);
        if (hasMappings || hasEvidence)
        {
            return (false, "Competency is in use by mappings or earned evidence and cannot be deleted.");
        }

        _db.Set<CompetencyNode>().Remove(node);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>Bumps the framework version only when earned evidence would otherwise be rewritten.</summary>
    private async Task BumpVersionIfNeededAsync(int frameworkId)
    {
        var hasEvidence = await _db.Set<CompetencyEvidence>()
            .AnyAsync(e => e.Competency!.FrameworkId == frameworkId);
        if (!hasEvidence)
        {
            return;
        }

        var framework = await _db.Set<CompetencyFramework>().FindAsync(frameworkId);
        if (framework is not null)
        {
            framework.Version++;
        }
    }

    // ===== Mappings =====

    public async Task<List<ActivityMapping>> GetCourseMappingsAsync(int courseId)
    {
        var courseAssignmentIds = await _db.Set<global::OpenLearning.Assignments.Models.Assignment>().AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Select(a => a.Id)
            .ToListAsync();

        return await _db.Set<ActivityMapping>().AsNoTracking()
            .Include(m => m.Competency)
            .Where(m => m.CourseId == courseId ||
                        (m.AssignmentId != null && courseAssignmentIds.Contains(m.AssignmentId.Value)))
            .OrderBy(m => m.Id)
            .ToListAsync();
    }

    public async Task<(bool Ok, string? Error)> MapCourseAsync(
        int courseId, int competencyId, string requesterId, bool isAdmin)
    {
        var node = await _db.Set<CompetencyNode>().AsNoTracking()
            .Include(n => n.Framework)
            .FirstOrDefaultAsync(n => n.Id == competencyId);
        if (node is null)
        {
            return (false, "Competency not found.");
        }

        if (node.Framework!.IsArchived)
        {
            return (false, "Archived frameworks cannot receive new mappings.");
        }

        var exists = await _db.Set<ActivityMapping>()
            .AnyAsync(m => m.CompetencyId == competencyId && m.CourseId == courseId);
        if (exists)
        {
            return (false, "This course is already mapped to that competency.");
        }

        _db.Set<ActivityMapping>().Add(new ActivityMapping
        {
            CompetencyId = competencyId,
            CourseId = courseId,
            CreatedBy = requesterId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> MapAssignmentAsync(
        int assignmentId, int competencyId, string requesterId, bool isAdmin)
    {
        var owned = await _assignments.IsOwnerAsync(assignmentId, requesterId);
        if (!owned && !isAdmin)
        {
            return (false, "Only the course owner can map its assignments.");
        }

        var node = await _db.Set<CompetencyNode>().AsNoTracking()
            .Include(n => n.Framework)
            .FirstOrDefaultAsync(n => n.Id == competencyId);
        if (node is null)
        {
            return (false, "Competency not found.");
        }

        if (node.Framework!.IsArchived)
        {
            return (false, "Archived frameworks cannot receive new mappings.");
        }

        var exists = await _db.Set<ActivityMapping>()
            .AnyAsync(m => m.CompetencyId == competencyId && m.AssignmentId == assignmentId);
        if (exists)
        {
            return (false, "This assignment is already mapped to that competency.");
        }

        _db.Set<ActivityMapping>().Add(new ActivityMapping
        {
            CompetencyId = competencyId,
            AssignmentId = assignmentId,
            CreatedBy = requesterId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UnmapAsync(int mappingId, string requesterId, bool isAdmin)
    {
        var mapping = await _db.Set<ActivityMapping>().FindAsync(mappingId);
        if (mapping is null)
        {
            return (false, "Mapping not found.");
        }

        if (mapping.CourseId is int courseId)
        {
            var owned = await IsCourseOwnerAsync(courseId, requesterId);
            if (!owned && !isAdmin)
            {
                return (false, "Only the course owner can remove its mappings.");
            }
        }
        else if (mapping.AssignmentId is int assignmentId)
        {
            var owned = await _assignments.IsOwnerAsync(assignmentId, requesterId);
            if (!owned && !isAdmin)
            {
                return (false, "Only the course owner can remove its mappings.");
            }
        }

        _db.Set<ActivityMapping>().Remove(mapping);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task<bool> IsCourseOwnerAsync(int courseId, string userId)
    {
        return await _db.Set<global::OpenLearning.CourseManagement.Models.Course>().AsNoTracking()
            .AnyAsync(c => c.Id == courseId && c.InstructorId == userId);
    }

    // ===== Evidence sync =====

    /// <summary>
    /// Creates missing automatic evidence for a learner from trusted completion
    /// state. Idempotent per (competency, source key).
    /// </summary>
    public async Task SyncEvidenceForUserAsync(string userId)
    {
        var mappings = await _db.Set<ActivityMapping>().AsNoTracking()
            .Include(m => m.Competency)
            .ThenInclude(c => c!.Framework)
            .ThenInclude(f => f!.ScaleLevels)
            .Where(m => m.CourseId != null || m.AssignmentId != null)
            .ToListAsync();
        if (mappings.Count == 0)
        {
            return;
        }

        var existingKeys = await _db.Set<CompetencyEvidence>()
            .Where(e => e.UserId == userId)
            .Select(e => new { e.CompetencyId, e.SourceKey })
            .ToDictionaryAsync(x => (x.CompetencyId, x.SourceKey), x => true);

        var created = false;

        foreach (var mapping in mappings.Where(m => m.CourseId is not null))
        {
            var courseId = mapping.CourseId!.Value;
            if (!await _enrollments.IsEnrolledAsync(userId, courseId))
            {
                continue;
            }

            var percent = await _progress.GetProgressPercentAsync(userId, courseId);
            if (percent < 100)
            {
                continue;
            }

            created |= EnsureAutoEvidenceAsync(mapping, userId, $"course:{courseId}:user:{userId}", existingKeys);
        }

        var gradedSubmissionAssignments = await _db.Set<global::OpenLearning.Assignments.Models.AssignmentSubmission>().AsNoTracking()
            .Where(s => s.StudentId == userId && s.GradedAt != null)
            .Select(s => s.AssignmentId)
            .Distinct()
            .ToListAsync();

        foreach (var mapping in mappings.Where(m => m.AssignmentId is not null))
        {
            var assignmentId = mapping.AssignmentId!.Value;
            if (!gradedSubmissionAssignments.Contains(assignmentId))
            {
                continue;
            }

            created |= EnsureAutoEvidenceAsync(mapping, userId, $"assignment:{assignmentId}:user:{userId}", existingKeys);
        }

        if (created)
        {
            await _db.SaveChangesAsync();
        }
    }

    private bool EnsureAutoEvidenceAsync(
        ActivityMapping mapping,
        string userId,
        string sourceKey,
        Dictionary<(int, string), bool> existingKeys)
    {
        if (existingKeys.ContainsKey((mapping.CompetencyId, sourceKey)))
        {
            return false;
        }

        _db.Set<CompetencyEvidence>().Add(new CompetencyEvidence
        {
            CompetencyId = mapping.CompetencyId,
            UserId = userId,
            SourceKey = sourceKey,
            Status = EvidenceStatus.Auto,
            LevelSortOrder = mapping.Competency!.Framework!.ScaleLevels.Count > 0
                ? mapping.Competency.Framework.ScaleLevels.Max(l => l.SortOrder)
                : null,
            FrameworkVersion = mapping.Competency.Framework.Version,
            CompetencyTitleSnapshot = mapping.Competency.Title,
        });
        existingKeys[(mapping.CompetencyId, sourceKey)] = true;
        return true;
    }

    // ===== Manual evidence =====

    public async Task<(bool Ok, string? Error)> SubmitManualEvidenceAsync(
        string userId, int competencyId, string description, string? attachmentUrl)
    {
        var node = await _db.Set<CompetencyNode>().AsNoTracking()
            .Include(n => n.Framework)
            .FirstOrDefaultAsync(n => n.Id == competencyId);
        if (node is null)
        {
            return (false, "Competency not found.");
        }

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 4000)
        {
            return (false, "Describe the evidence (4000 characters or fewer).");
        }

        _db.Set<CompetencyEvidence>().Add(new CompetencyEvidence
        {
            CompetencyId = competencyId,
            UserId = userId,
            SourceKey = $"manual:{Guid.NewGuid():N}",
            Status = EvidenceStatus.Pending,
            FrameworkVersion = node.Framework!.Version,
            CompetencyTitleSnapshot = node.Title,
            Description = description.Trim(),
            AttachmentUrl = string.IsNullOrWhiteSpace(attachmentUrl) ? null : attachmentUrl.Trim(),
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<List<CompetencyEvidence>> GetPendingReviewsAsync()
    {
        return _db.Set<CompetencyEvidence>().AsNoTracking()
            .Include(e => e.Competency)
            .ThenInclude(c => c!.Framework)
            .ThenInclude(f => f!.ScaleLevels)
            .Where(e => e.Status == EvidenceStatus.Pending)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Reviewers are Admins and Instructors who own a course mapped to the competency.</summary>
    public async Task<bool> CanReviewAsync(string reviewerId, int competencyId, bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        var mappedCourseIds = await _db.Set<ActivityMapping>().AsNoTracking()
            .Where(m => m.CompetencyId == competencyId && m.CourseId != null)
            .Select(m => m.CourseId!.Value)
            .ToListAsync();

        var mappedAssignmentIds = await _db.Set<ActivityMapping>().AsNoTracking()
            .Where(m => m.CompetencyId == competencyId && m.AssignmentId != null)
            .Select(m => m.AssignmentId!.Value)
            .ToListAsync();

        if (mappedAssignmentIds.Count > 0)
        {
            var assignmentCourses = await _db.Set<global::OpenLearning.Assignments.Models.Assignment>().AsNoTracking()
                .Where(a => mappedAssignmentIds.Contains(a.Id))
                .Select(a => a.CourseId)
                .ToListAsync();
            mappedCourseIds.AddRange(assignmentCourses);
        }

        if (mappedCourseIds.Count == 0)
        {
            return false;
        }

        return await _db.Set<global::OpenLearning.CourseManagement.Models.Course>().AsNoTracking()
            .AnyAsync(c => mappedCourseIds.Contains(c.Id) && c.InstructorId == reviewerId);
    }

    public async Task<(bool Ok, string? Error)> ReviewEvidenceAsync(
        int evidenceId, string reviewerId, bool isAdmin, bool approve, int? levelSortOrder, string? reason)
    {
        var evidence = await _db.Set<CompetencyEvidence>().FindAsync(evidenceId);
        if (evidence is null || evidence.Status != EvidenceStatus.Pending)
        {
            return (false, "Pending evidence not found.");
        }

        if (!await CanReviewAsync(reviewerId, evidence.CompetencyId, isAdmin))
        {
            return (false, "You are not a reviewer for this competency.");
        }

        if (approve)
        {
            var frameworkId = await _db.Set<CompetencyNode>().AsNoTracking()
                .Where(n => n.Id == evidence.CompetencyId)
                .Select(n => n.FrameworkId)
                .FirstOrDefaultAsync();
            var maxLevel = await _db.Set<FrameworkScaleLevel>().AsNoTracking()
                .Where(l => l.FrameworkId == frameworkId)
                .MaxAsync(l => (int?)l.SortOrder);
            if (levelSortOrder is null || levelSortOrder < 1 || levelSortOrder > maxLevel)
            {
                return (false, "Choose a valid achievement level to approve.");
            }

            evidence.Status = EvidenceStatus.Approved;
            evidence.LevelSortOrder = levelSortOrder;
        }
        else
        {
            evidence.Status = EvidenceStatus.Rejected;
            evidence.ReviewReason = string.IsNullOrWhiteSpace(reason) ? "Rejected." : reason.Trim();
        }

        evidence.ReviewerId = reviewerId;
        evidence.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Profiles and gap analysis =====

    public sealed record CompetencyStatus(
        CompetencyNode Competency,
        string State,
        int? Level,
        string LevelLabel,
        List<CompetencyEvidence> Evidence);

    public sealed record ProfileRow(
        CompetencyFramework Framework,
        List<CompetencyStatus> Statuses);

    /// <summary>Syncs then computes attainment for every non-archived framework.</summary>
    public async Task<List<ProfileRow>> GetProfileAsync(string userId)
    {
        await SyncEvidenceForUserAsync(userId);

        var frameworks = await _db.Set<CompetencyFramework>().AsNoTracking()
            .Include(f => f.ScaleLevels)
            .Include(f => f.Competencies)
            .Where(f => !f.IsArchived)
            .OrderBy(f => f.Name)
            .ToListAsync();

        var evidence = await _db.Set<CompetencyEvidence>().AsNoTracking()
            .Where(e => e.UserId == userId &&
                        (e.Status == EvidenceStatus.Auto || e.Status == EvidenceStatus.Approved || e.Status == EvidenceStatus.Pending))
            .ToListAsync();

        var result = new List<ProfileRow>();
        foreach (var framework in frameworks)
        {
            var statuses = new List<CompetencyStatus>();
            foreach (var node in framework.Competencies)
            {
                var own = evidence.Where(e => e.CompetencyId == node.Id).ToList();
                var approved = own.Where(e => e.Status is EvidenceStatus.Auto or EvidenceStatus.Approved).ToList();
                var pending = own.Where(e => e.Status == EvidenceStatus.Pending).ToList();

                string state;
                int? level = null;
                if (approved.Count > 0)
                {
                    state = "achieved";
                    level = approved.Max(e => e.LevelSortOrder);
                }
                else if (pending.Count > 0)
                {
                    state = "partial";
                }
                else
                {
                    state = "missing";
                }

                var label = level is null
                    ? string.Empty
                    : framework.ScaleLevels.FirstOrDefault(l => l.SortOrder == level)?.Label ?? string.Empty;

                statuses.Add(new CompetencyStatus(node, state, level, label, own));
            }

            result.Add(new ProfileRow(framework, statuses));
        }

        return result;
    }

    public sealed record GapRow(string UserId, string State);

    /// <summary>Gap analysis of one learner against one target framework.</summary>
    public async Task<(List<GapRow>? Rows, string? Error)> GetGapAsync(string viewerId, bool isAdmin, string userId, int frameworkId)
    {
        if (viewerId != userId && !isAdmin && !await CanViewLearnerAsync(viewerId, userId))
        {
            return (null, "You are not authorized to view this learner's profile.");
        }

        var profile = await GetProfileAsync(userId);
        var row = profile.FirstOrDefault(p => p.Framework.Id == frameworkId);
        if (row is null)
        {
            return (null, "Framework not found.");
        }

        return (row.Statuses.Select(s => new GapRow(userId, s.State)).ToList(), null);
    }

    /// <summary>Cohort gap: enrolled students of a course against a framework.</summary>
    public async Task<(List<(string UserId, int Achieved, int Partial, int Missing)>? Rows, string? Error)>
        GetCohortGapAsync(string viewerId, bool isAdmin, int courseId, int frameworkId)
    {
        if (!isAdmin && !await IsCourseOwnerAsync(courseId, viewerId))
        {
            return (null, "Only the course owner can view this cohort gap analysis.");
        }

        var roster = await _enrollments.GetEnrollmentsForRosterAsync(courseId);
        var studentIds = roster.Enrollments
            .Where(e => e.RevokedAt is null)
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        var framework = await _db.Set<CompetencyFramework>().AsNoTracking()
            .Include(f => f.Competencies)
            .FirstOrDefaultAsync(f => f.Id == frameworkId);
        if (framework is null)
        {
            return (null, "Framework not found.");
        }

        var competencyIds = framework.Competencies.Select(c => c.Id).ToList();
        var evidence = await _db.Set<CompetencyEvidence>().AsNoTracking()
            .Where(e => competencyIds.Contains(e.CompetencyId) &&
                        studentIds.Contains(e.UserId) &&
                        (e.Status == EvidenceStatus.Auto || e.Status == EvidenceStatus.Approved || e.Status == EvidenceStatus.Pending))
            .ToListAsync();

        var rows = new List<(string UserId, int Achieved, int Partial, int Missing)>();
        foreach (var studentId in studentIds)
        {
            var own = evidence.Where(e => e.UserId == studentId).ToList();
            int achieved = 0, partial = 0, missing = 0;
            foreach (var competencyId in competencyIds)
            {
                var forNode = own.Where(e => e.CompetencyId == competencyId).ToList();
                if (forNode.Any(e => e.Status is EvidenceStatus.Auto or EvidenceStatus.Approved))
                {
                    achieved++;
                }
                else if (forNode.Any(e => e.Status == EvidenceStatus.Pending))
                {
                    partial++;
                }
                else
                {
                    missing++;
                }
            }

            rows.Add((studentId, achieved, partial, missing));
        }

        return (rows, null);
    }

    /// <summary>Authorized viewers: self, admins, instructors owning a course the learner is enrolled in, and organization managers.</summary>
    public async Task<bool> CanViewLearnerAsync(string viewerId, string learnerId)
    {
        var enrolledCourseIds = await _db.Set<global::OpenLearning.Enrollment.Models.Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == learnerId && e.RevokedAt == null)
            .Select(e => e.CourseId)
            .ToListAsync();

        if (enrolledCourseIds.Count > 0)
        {
            var ownsEnrolledCourse = await _db.Set<global::OpenLearning.CourseManagement.Models.Course>().AsNoTracking()
                .AnyAsync(c => enrolledCourseIds.Contains(c.Id) && c.InstructorId == viewerId);
            if (ownsEnrolledCourse)
            {
                return true;
            }
        }

        var memberships = await _db.Set<global::OpenLearning.Organizations.Models.OrganizationMembership>().AsNoTracking()
            .Where(m => m.UserId == learnerId && m.Status == global::OpenLearning.Organizations.Models.MembershipStatus.Active)
            .Select(m => new { m.OrganizationId, m.Role })
            .ToListAsync();
        if (memberships.Count > 0)
        {
            var orgIds = memberships.Select(m => m.OrganizationId).ToList();
            var viewerIsManager = await _db.Set<global::OpenLearning.Organizations.Models.OrganizationMembership>().AsNoTracking()
                .AnyAsync(m => m.UserId == viewerId &&
                               orgIds.Contains(m.OrganizationId) &&
                               m.Status == global::OpenLearning.Organizations.Models.MembershipStatus.Active &&
                               (m.Role == global::OpenLearning.Organizations.Models.OrganizationRole.Manager ||
                                m.Role == global::OpenLearning.Organizations.Models.OrganizationRole.OrganizationAdmin));
            if (viewerIsManager)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<Dictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> userIds)
    {
        var users = await _users.GetByIdsAsync(userIds.Distinct());
        return users
            .Where(u => u is not null)
            .ToDictionary(u => u!.Id, u => u!.DisplayName);
    }
}
