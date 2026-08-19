using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenLearning.Auth;
using OpenLearning.CouponIO.Models;

namespace OpenLearning.Web.Pages.Admin.Coupons;

[Authorize(Policy = Policies.RequireAdmin)]
public class ImportJobsModel : PageModel
{
    private readonly DbContext _db;

    public ImportJobsModel(DbContext db)
    {
        _db = db;
    }

    public List<CouponImportJob> Jobs { get; set; } = new();

    public async Task OnGetAsync()
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Jobs = await _db.Set<CouponImportJob>().AsNoTracking()
            .Where(j => j.UserId == adminId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public static string StatusLabel(CouponImportJobStatus status)
    {
        return status switch
        {
            CouponImportJobStatus.Pending => "等待中",
            CouponImportJobStatus.Running => "导入中",
            CouponImportJobStatus.Success => "已完成",
            _ => "失败",
        };
    }
}
