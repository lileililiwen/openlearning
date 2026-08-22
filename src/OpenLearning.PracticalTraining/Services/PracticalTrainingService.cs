using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenLearning.PracticalTraining.Models;
using OpenLearning.Storage.Models;

namespace OpenLearning.PracticalTraining.Services;

public sealed record CompletionResult(bool Ok, string? Error, PracticalCompletion? Completion);

public sealed class PracticalTrainingService
{
    private readonly DbContext _db;
    public PracticalTrainingService(DbContext db)
    {
        _db = db;
    }

    public async Task<PracticalProgram> CreateProgramAsync(string title, decimal minimumHours, IEnumerable<string> competencies)
    {
        if (string.IsNullOrWhiteSpace(title) || minimumHours <= 0)
        {
            throw new ArgumentException("Title and positive minimum hours are required.");
        }
        var names = competencies.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (names.Count == 0)
        {
            throw new ArgumentException("At least one competency is required.");
        }
        var version = await _db.Set<PracticalProgram>().Where(x => x.Title == title.Trim()).MaxAsync(x => (int?)x.Version) ?? 0;
        var program = new PracticalProgram
        {
            Title = title.Trim(),
            MinimumHours = minimumHours,
            Version = version + 1,
            Competencies = names.Select(x => new ProgramCompetency { Name = x }).ToList()
        };
        _db.Add(program);
        await _db.SaveChangesAsync();
        return program;
    }

    public async Task<HostOrganization> CreateHostAsync(string name, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Host name is required.");
        }
        var host = new HostOrganization { Name = name.Trim(), ContactEmail = email?.Trim() ?? string.Empty };
        _db.Add(host);
        await _db.SaveChangesAsync();
        return host;
    }

    public async Task<Placement> CreatePlacementAsync(int programId, int hostId, string learnerId, string coordinatorId,
        string? supervisorName, string? supervisorEmail, DateOnly? startsOn, DateOnly? endsOn)
    {
        var program = await _db.Set<PracticalProgram>().Include(x => x.Competencies).SingleOrDefaultAsync(x => x.Id == programId);
        if (program is null || !await _db.Set<HostOrganization>().AnyAsync(x => x.Id == hostId))
        {
            throw new ArgumentException("Program and host must exist.");
        }
        if (string.IsNullOrWhiteSpace(learnerId) || string.IsNullOrWhiteSpace(coordinatorId))
        {
            throw new ArgumentException("Learner and coordinator are required.");
        }
        var placement = new Placement
        {
            PracticalProgramId = programId,
            HostOrganizationId = hostId,
            LearnerId = learnerId,
            CoordinatorId = coordinatorId,
            SupervisorName = supervisorName?.Trim() ?? string.Empty,
            SupervisorEmail = supervisorEmail?.Trim() ?? string.Empty,
            StartsOn = startsOn,
            EndsOn = endsOn,
            Competencies = program.Competencies.Select(x => new PlacementCompetency { ProgramCompetencyId = x.Id }).ToList()
        };
        _db.Add(placement);
        await _db.SaveChangesAsync();
        return placement;
    }

    public async Task<(bool Ok, string? Error)> ActivateAsync(int placementId, string coordinatorId, bool isAdmin)
    {
        var p = await OwnedPlacement(placementId, coordinatorId, isAdmin);
        if (p is null)
        {
            return (false, "Placement not found or access denied.");
        }
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(p.LearnerId))
        {
            missing.Add("learner");
        }
        if (p.PracticalProgramId == 0)
        {
            missing.Add("program");
        }
        if (p.HostOrganizationId == 0)
        {
            missing.Add("host");
        }
        if (string.IsNullOrWhiteSpace(p.CoordinatorId))
        {
            missing.Add("coordinator");
        }
        if (string.IsNullOrWhiteSpace(p.SupervisorEmail))
        {
            missing.Add("supervisor");
        }
        if (p.StartsOn is null || p.EndsOn is null || p.EndsOn < p.StartsOn)
        {
            missing.Add("valid dates");
        }
        if (missing.Count > 0)
        {
            return (false, $"Missing requirements: {string.Join(", ", missing)}.");
        }
        if (p.Status != PlacementStatus.Draft && p.Status != PlacementStatus.Suspended)
        {
            return (false, "Only draft or suspended placements can be activated.");
        }
        p.Status = PlacementStatus.Active;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(string? Token, string? Error)> InviteSupervisorAsync(int placementId, string coordinatorId, bool isAdmin, TimeSpan lifetime)
    {
        var p = await OwnedPlacement(placementId, coordinatorId, isAdmin);
        if (p is null)
        {
            return (null, "Placement not found or access denied.");
        }
        if (string.IsNullOrWhiteSpace(p.SupervisorEmail))
        {
            return (null, "Supervisor email is required.");
        }
        foreach (var old in await _db.Set<SupervisorInvitation>().Where(x => x.PlacementId == placementId && x.RevokedAt == null).ToListAsync())
        {
            old.RevokedAt = DateTime.UtcNow;
        }
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.Add(new SupervisorInvitation { PlacementId = placementId, TokenHash = Hash(token), ExpiresAt = DateTime.UtcNow.Add(lifetime) });
        await _db.SaveChangesAsync();
        return (token, null);
    }

    public async Task<Placement?> ResolveSupervisorAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }
        var now = DateTime.UtcNow;
        var hash = Hash(token);
        var invitation = await _db.Set<SupervisorInvitation>().AsNoTracking()
            .Include(x => x.Placement).ThenInclude(x => x!.Host)
            .Include(x => x.Placement).ThenInclude(x => x!.Program)
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null && x.ExpiresAt > now);
        return invitation?.Placement;
    }

    public async Task<(bool Ok, string? Error)> RevokeSupervisorAsync(int placementId, string coordinatorId, bool isAdmin)
    {
        if (await OwnedPlacement(placementId, coordinatorId, isAdmin) is null)
        {
            return (false, "Placement not found or access denied.");
        }
        var invitations = await _db.Set<SupervisorInvitation>().Where(x => x.PlacementId == placementId && x.RevokedAt == null).ToListAsync();
        foreach (var invitation in invitations)
        {
            invitation.RevokedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, PracticalHourLog? Log)> SubmitHoursAsync(int placementId, string learnerId,
        DateTime startedAt, DateTime endedAt, string? description, int? amendsLogId = null)
    {
        if (endedAt <= startedAt || endedAt - startedAt > TimeSpan.FromHours(24))
        {
            return (false, "A valid work interval of at most 24 hours is required.", null);
        }
        var p = await _db.Set<Placement>().SingleOrDefaultAsync(x => x.Id == placementId && x.LearnerId == learnerId && x.Status == PlacementStatus.Active);
        if (p is null)
        {
            return (false, "Active placement not found or access denied.", null);
        }
        PracticalHourLog? prior = null;
        if (amendsLogId is not null)
        {
            prior = await _db.Set<PracticalHourLog>().SingleOrDefaultAsync(x => x.Id == amendsLogId && x.PlacementId == placementId && x.Status == PracticalLogStatus.Approved);
            if (prior is null)
            {
                return (false, "Only an approved log in this placement can be amended.", null);
            }
        }
        var overlap = await _db.Set<PracticalHourLog>().AnyAsync(x => x.PlacementId == placementId && x.Status == PracticalLogStatus.Approved &&
            x.Id != amendsLogId && startedAt < x.EndedAt && endedAt > x.StartedAt);
        if (overlap)
        {
            return (false, "The interval overlaps approved hours.", null);
        }
        var log = new PracticalHourLog
        {
            PlacementId = placementId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            Description = description?.Trim() ?? string.Empty,
            AmendsLogId = amendsLogId
        };
        _db.Add(log);
        await _db.SaveChangesAsync();
        return (true, null, log);
    }

    public async Task<(bool Ok, string? Error)> ReviewHoursAsync(string token, int placementId, int logId, Guid concurrencyStamp, bool approve, string? note)
    {
        var access = await ResolveSupervisorAsync(token);
        if (access?.Id != placementId)
        {
            return (false, "Access denied.");
        }
        var log = await _db.Set<PracticalHourLog>().SingleOrDefaultAsync(x => x.Id == logId && x.PlacementId == placementId);
        if (log is null || log.Status != PracticalLogStatus.Submitted || log.ConcurrencyStamp != concurrencyStamp)
        {
            return (false, "Log changed, was already reviewed, or does not belong to this placement.");
        }
        if (approve && await _db.Set<PracticalHourLog>().AnyAsync(x => x.PlacementId == placementId && x.Status == PracticalLogStatus.Approved && x.Id != log.AmendsLogId && log.StartedAt < x.EndedAt && log.EndedAt > x.StartedAt))
        {
            return (false, "The interval overlaps approved hours.");
        }
        log.Status = approve ? PracticalLogStatus.Approved : PracticalLogStatus.Rejected;
        log.ReviewedAt = DateTime.UtcNow;
        log.ReviewedBy = access.SupervisorEmail;
        log.ReviewNote = note?.Trim();
        log.ConcurrencyStamp = Guid.NewGuid();
        if (approve && log.AmendsLogId is int priorId)
        {
            var prior = await _db.Set<PracticalHourLog>().SingleAsync(x => x.Id == priorId);
            prior.Status = PracticalLogStatus.Superseded;
        }
        try
        {
            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "The log was reviewed concurrently.");
        }
    }

    public async Task<(bool Ok, string? Error)> AddEvidenceAsync(int placementId, string learnerId, int storedFileId, string? description)
    {
        if (!await _db.Set<Placement>().AnyAsync(x => x.Id == placementId && x.LearnerId == learnerId && x.Status == PlacementStatus.Active))
        {
            return (false, "Active placement not found or access denied.");
        }
        var file = await _db.Set<StoredFile>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == storedFileId && x.OwnerId == learnerId);
        if (file is null || file.Purpose is not (FilePurpose.Answer or FilePurpose.Document or FilePurpose.Image))
        {
            return (false, "Evidence file is missing, unsafe, or not owned by the learner.");
        }
        _db.Add(new PracticalEvidence { PlacementId = placementId, LearnerId = learnerId, StoredFileId = storedFileId, Description = description?.Trim() ?? string.Empty });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> EvaluateCompetencyAsync(string token, int placementId, int competencyId, bool achieved, string evaluation)
    {
        var access = await ResolveSupervisorAsync(token);
        if (access?.Id != placementId)
        {
            return (false, "Access denied.");
        }
        var item = await _db.Set<PlacementCompetency>().SingleOrDefaultAsync(x => x.Id == competencyId && x.PlacementId == placementId);
        if (item is null)
        {
            return (false, "Competency not found.");
        }
        item.IsAchieved = achieved;
        item.Evaluation = evaluation.Trim();
        item.EvaluatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SubmitEvaluationAsync(string token, int placementId, string summary)
    {
        var access = await ResolveSupervisorAsync(token);
        if (access?.Id != placementId)
        {
            return (false, "Access denied.");
        }
        if (string.IsNullOrWhiteSpace(summary))
        {
            return (false, "Evaluation is required.");
        }
        var existing = await _db.Set<PracticalEvaluation>().SingleOrDefaultAsync(x => x.PlacementId == placementId && x.EvaluatorKind == "Supervisor");
        if (existing is null)
        {
            _db.Add(new PracticalEvaluation { PlacementId = placementId, EvaluatorKind = "Supervisor", Summary = summary.Trim() });
        }
        else
        {
            existing.Summary = summary.Trim();
            existing.SubmittedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<PlacementIncident> ReportIncidentAsync(int placementId, string coordinatorId, bool isAdmin, IncidentSeverity severity, string summary)
    {
        if (await OwnedPlacement(placementId, coordinatorId, isAdmin) is null)
        {
            throw new UnauthorizedAccessException();
        }
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Incident summary is required.");
        }
        var incident = new PlacementIncident { PlacementId = placementId, Severity = severity, Summary = summary.Trim() };
        _db.Add(incident);
        await _db.SaveChangesAsync();
        return incident;
    }

    public async Task<(bool Ok, string? Error)> ResolveIncidentAsync(int placementId, int incidentId, string coordinatorId, bool isAdmin, string resolution)
    {
        if (await OwnedPlacement(placementId, coordinatorId, isAdmin) is null)
        {
            return (false, "Placement not found or access denied.");
        }
        var incident = await _db.Set<PlacementIncident>().SingleOrDefaultAsync(x => x.Id == incidentId && x.PlacementId == placementId);
        if (incident is null || string.IsNullOrWhiteSpace(resolution))
        {
            return (false, "Incident and resolution are required.");
        }
        incident.Resolution = resolution.Trim();
        incident.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<CompletionResult> ConfirmCompletionAsync(int placementId, string coordinatorId, bool isAdmin)
    {
        var p = await _db.Set<Placement>().Include(x => x.Program).ThenInclude(x => x!.Competencies).Include(x => x.Competencies)
            .SingleOrDefaultAsync(x => x.Id == placementId && (isAdmin || x.CoordinatorId == coordinatorId));
        if (p?.Program is null)
        {
            return new(false, "Placement not found or access denied.", null);
        }
        var existing = await _db.Set<PracticalCompletion>().SingleOrDefaultAsync(x => x.PlacementId == placementId);
        if (existing is not null)
        {
            return new(true, null, existing);
        }
        var intervals = await _db.Set<PracticalHourLog>().Where(x => x.PlacementId == placementId && x.Status == PracticalLogStatus.Approved)
            .Select(x => new { x.StartedAt, x.EndedAt }).ToListAsync();
        var approved = intervals.Sum(x => (decimal)(x.EndedAt - x.StartedAt).TotalHours);
        if (approved < p.Program.MinimumHours)
        {
            return new(false, $"Approved hours {approved:0.##}/{p.Program.MinimumHours:0.##} are incomplete.", null);
        }
        var requiredIds = p.Program.Competencies.Where(x => x.IsRequired).Select(x => x.Id).ToHashSet();
        if (p.Competencies.Any(x => requiredIds.Contains(x.ProgramCompetencyId) && !x.IsAchieved))
        {
            return new(false, "Required competencies are incomplete.", null);
        }
        if (!await _db.Set<PracticalEvaluation>().AnyAsync(x => x.PlacementId == placementId && x.EvaluatorKind == "Supervisor"))
        {
            return new(false, "Supervisor evaluation is incomplete.", null);
        }
        if (await _db.Set<PlacementIncident>().AnyAsync(x => x.PlacementId == placementId && x.Severity == IncidentSeverity.Blocking && x.ResolvedAt == null))
        {
            return new(false, "A blocking incident remains unresolved.", null);
        }
        var completion = new PracticalCompletion { PlacementId = placementId, ApprovedHours = approved, ConfirmedBy = coordinatorId, ConfirmationKey = $"practical:{placementId}" };
        p.Status = PlacementStatus.Completed;
        p.CompletedAt = completion.ConfirmedAt;
        _db.Add(completion);
        await _db.SaveChangesAsync();
        return new(true, null, completion);
    }

    public Task<List<Placement>> ListForCoordinatorAsync(string userId, bool isAdmin)
    {
        return _db.Set<Placement>().AsNoTracking().Include(x => x.Program).Include(x => x.Host).Where(x => isAdmin || x.CoordinatorId == userId).OrderByDescending(x => x.Id).ToListAsync();
    }

    public Task<List<Placement>> ListForLearnerAsync(string userId)
    {
        return _db.Set<Placement>().AsNoTracking().Include(x => x.Program).Include(x => x.Host).Where(x => x.LearnerId == userId).OrderByDescending(x => x.Id).ToListAsync();
    }

    public Task<List<PracticalHourLog>> ListLogsAsync(int placementId)
    {
        return _db.Set<PracticalHourLog>().AsNoTracking().Where(x => x.PlacementId == placementId).OrderByDescending(x => x.StartedAt).ToListAsync();
    }

    public Task<List<PlacementIncident>> ListIncidentsAsync(int placementId)
    {
        return _db.Set<PlacementIncident>().AsNoTracking().Where(x => x.PlacementId == placementId).OrderByDescending(x => x.ReportedAt).ToListAsync();
    }

    public Task<List<PlacementCompetency>> ListCompetenciesAsync(int placementId)
    {
        return _db.Set<PlacementCompetency>().AsNoTracking().Include(x => x.ProgramCompetency).Where(x => x.PlacementId == placementId).ToListAsync();
    }

    public Task<List<PracticalProgram>> ListProgramsAsync()
    {
        return _db.Set<PracticalProgram>().AsNoTracking().OrderBy(x => x.Title).ThenByDescending(x => x.Version).ToListAsync();
    }

    public Task<List<HostOrganization>> ListHostsAsync()
    {
        return _db.Set<HostOrganization>().AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    private Task<Placement?> OwnedPlacement(int id, string userId, bool admin)
    {
        return _db.Set<Placement>().SingleOrDefaultAsync(x => x.Id == id && (admin || x.CoordinatorId == userId));
    }

    private static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
