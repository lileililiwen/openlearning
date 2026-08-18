using Microsoft.EntityFrameworkCore;
using OpenLearning.Operations.Models;

namespace OpenLearning.Operations.Services;

/// <summary>
/// Admin CRUD for banners, pop-ups, campaigns, and homepage features, plus
/// active-content queries used by the public homepage.
/// </summary>
public class OperationsService
{
    private readonly DbContext _db;

    public OperationsService(DbContext db)
    {
        _db = db;
    }

    // ===== Active queries (homepage) =====

    /// <summary>
    /// Banners that are active and whose campaign (if any) is currently in
    /// its window, ordered for the carousel.
    /// </summary>
    public async Task<List<Banner>> GetActiveBannersAsync()
    {
        var now = DateTime.UtcNow;
        // Resolve in-window campaign ids first: navigation-based predicates in
        // an OR are not reliably translated (InMemory provider), so filter on
        // scalar ids instead.
        var inWindowCampaignIds = await _db.Set<Campaign>().AsNoTracking()
            .Where(c => c.IsActive && c.StartsAt <= now && c.EndsAt >= now)
            .Select(c => c.Id)
            .ToListAsync();

        return await _db.Set<Banner>().AsNoTracking()
            .Include(b => b.Campaign)
            .Where(b => b.IsActive &&
                (b.CampaignId == null || inWindowCampaignIds.Contains(b.CampaignId.Value)))
            .OrderBy(b => b.OrderIndex)
            .ThenBy(b => b.Id)
            .ToListAsync();
    }

    /// <summary>The single active pop-up in its window, if any.</summary>
    public Task<Popup?> GetActivePopupAsync()
    {
        var now = DateTime.UtcNow;
        return _db.Set<Popup>().AsNoTracking()
            .Where(p => p.IsActive && p.StartsAt <= now && p.EndsAt >= now)
            .OrderByDescending(p => p.EndsAt)
            .FirstOrDefaultAsync();
    }

    public Task<List<HomepageFeature>> GetHomepageFeaturesAsync()
    {
        return _db.Set<HomepageFeature>().AsNoTracking()
            .OrderBy(f => f.OrderIndex)
            .ThenBy(f => f.Id)
            .ToListAsync();
    }

    // ===== Admin CRUD =====

    public Task<List<Banner>> GetAllBannersAsync()
    {
        return _db.Set<Banner>().AsNoTracking()
            .Include(b => b.Campaign)
            .OrderBy(b => b.OrderIndex)
            .ThenBy(b => b.Id)
            .ToListAsync();
    }

    public Task<Banner?> GetBannerByIdAsync(int id)
    {
        return _db.Set<Banner>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<(bool Ok, string? Error)> CreateBannerAsync(
        string title, string imageUrl, string linkUrl, int? campaignId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, "Banner title is required.");
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return (false, "Banner image URL is required.");
        }

        if (campaignId is not null &&
            !await _db.Set<Campaign>().AnyAsync(c => c.Id == campaignId))
        {
            return (false, "Campaign not found.");
        }

        var maxOrder = await _db.Set<Banner>().AnyAsync()
            ? await _db.Set<Banner>().MaxAsync(b => b.OrderIndex)
            : 0;

        _db.Set<Banner>().Add(new Banner
        {
            Title = title.Trim(),
            ImageUrl = imageUrl.Trim(),
            LinkUrl = (linkUrl ?? string.Empty).Trim(),
            OrderIndex = maxOrder + 1,
            CampaignId = campaignId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateBannerAsync(
        int id, string title, string imageUrl, string linkUrl, int? campaignId, bool isActive)
    {
        var banner = await _db.Set<Banner>().FindAsync(id);
        if (banner is null)
        {
            return (false, "Banner not found.");
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(imageUrl))
        {
            return (false, "Banner title and image URL are required.");
        }

        banner.Title = title.Trim();
        banner.ImageUrl = imageUrl.Trim();
        banner.LinkUrl = (linkUrl ?? string.Empty).Trim();
        banner.CampaignId = campaignId;
        banner.IsActive = isActive;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetBannerOrderAsync(int id, int orderIndex)
    {
        var banner = await _db.Set<Banner>().FindAsync(id);
        if (banner is null)
        {
            return (false, "Banner not found.");
        }

        banner.OrderIndex = orderIndex;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteBannerAsync(int id)
    {
        var banner = await _db.Set<Banner>().FindAsync(id);
        if (banner is null)
        {
            return (false, "Banner not found.");
        }

        _db.Set<Banner>().Remove(banner);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<List<Popup>> GetAllPopupsAsync()
    {
        return _db.Set<Popup>().AsNoTracking()
            .OrderByDescending(p => p.EndsAt)
            .ToListAsync();
    }

    public async Task<(bool Ok, string? Error)> CreatePopupAsync(
        string title, string body, string linkUrl, DateTime startsAt, DateTime endsAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, "Pop-up title is required.");
        }

        if (endsAt <= startsAt)
        {
            return (false, "Pop-up end must be after its start.");
        }

        _db.Set<Popup>().Add(new Popup
        {
            Title = title.Trim(),
            Body = (body ?? string.Empty).Trim(),
            LinkUrl = (linkUrl ?? string.Empty).Trim(),
            StartsAt = startsAt,
            EndsAt = endsAt,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> TogglePopupAsync(int id)
    {
        var popup = await _db.Set<Popup>().FindAsync(id);
        if (popup is null)
        {
            return (false, "Pop-up not found.");
        }

        popup.IsActive = !popup.IsActive;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeletePopupAsync(int id)
    {
        var popup = await _db.Set<Popup>().FindAsync(id);
        if (popup is null)
        {
            return (false, "Pop-up not found.");
        }

        _db.Set<Popup>().Remove(popup);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public Task<List<Campaign>> GetAllCampaignsAsync()
    {
        return _db.Set<Campaign>().AsNoTracking()
            .OrderByDescending(c => c.EndsAt)
            .ToListAsync();
    }

    public async Task<(bool Ok, string? Error)> CreateCampaignAsync(
        string name, DateTime startsAt, DateTime endsAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Campaign name is required.");
        }

        if (endsAt <= startsAt)
        {
            return (false, "Campaign end must be after its start.");
        }

        _db.Set<Campaign>().Add(new Campaign
        {
            Name = name.Trim(),
            StartsAt = startsAt,
            EndsAt = endsAt,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ToggleCampaignAsync(int id)
    {
        var campaign = await _db.Set<Campaign>().FindAsync(id);
        if (campaign is null)
        {
            return (false, "Campaign not found.");
        }

        campaign.IsActive = !campaign.IsActive;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteCampaignAsync(int id)
    {
        var campaign = await _db.Set<Campaign>().FindAsync(id);
        if (campaign is null)
        {
            return (false, "Campaign not found.");
        }

        _db.Set<Campaign>().Remove(campaign);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ===== Homepage features =====

    public async Task<(bool Ok, string? Error)> SetHomepageFeaturesAsync(
        IReadOnlyList<(string? Category, int? CourseId)> features)
    {
        var existing = await _db.Set<HomepageFeature>().ToListAsync();
        foreach (var feature in existing)
        {
            _db.Set<HomepageFeature>().Remove(feature);
        }

        for (var i = 0; i < features.Count; i++)
        {
            _db.Set<HomepageFeature>().Add(new HomepageFeature
            {
                Category = features[i].Category,
                CourseId = features[i].CourseId,
                OrderIndex = i,
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }
}
