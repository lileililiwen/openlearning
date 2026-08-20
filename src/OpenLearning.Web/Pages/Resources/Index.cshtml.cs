using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenLearning.Auth;
using OpenLearning.ResourceCenter.Services;
using OpenLearning.Storage.Models;

namespace OpenLearning.Web.Pages.Resources;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ResourceService _resources;

    public IndexModel(ResourceService resources)
    {
        _resources = resources;
    }

    public List<ResourceRow> Items { get; set; } = new();

    public int Total { get; set; }

    public bool IsAdmin { get; set; }

    [BindProperty(SupportsGet = true)]
    public FilePurpose? Purpose { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [BindProperty]
    public FilePurpose UploadPurpose { get; set; } = FilePurpose.Image;

    public string? Message { get; set; }

    public string? MessageType { get; set; }

    public string? UploadedUrl { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsAdmin = User.IsInRole(Roles.Admin);
        var (items, total) = await _resources.ListAsync(
            userId, IsAdmin, Purpose, Search, Math.Max(1, PageNumber));
        Items = items;
        Total = total;
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (UploadFile is null || UploadFile.Length == 0)
        {
            Message = "请选择要上传的文件。";
            MessageType = "danger";
        }
        else
        {
            var (file, error) = await _resources.UploadAsync(userId, UploadPurpose, UploadFile);
            if (error is not null || file is null)
            {
                Message = error ?? "上传失败。";
                MessageType = "danger";
            }
            else
            {
                Message = "上传成功。";
                MessageType = "success";
                UploadedUrl = $"/files/{file.Key}";
            }
        }

        return await ReloadPageAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string key)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var (ok, error) = await _resources.DeleteAsync(key, userId, isAdmin);
        Message = ok ? "已删除。" : (error ?? "删除失败。");
        MessageType = ok ? "success" : "danger";
        return await ReloadPageAsync();
    }

    public async Task<IActionResult> OnPostShareAsync(string key, bool shared)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Roles.Admin);
        var (ok, error) = await _resources.SetSharedAsync(key, userId, isAdmin, shared);
        if (ok)
        {
            Message = shared ? "已设为共享。" : "已取消共享。";
        }
        else
        {
            Message = error ?? "操作失败。";
        }

        MessageType = ok ? "success" : "danger";
        return await ReloadPageAsync();
    }

    private async Task<IActionResult> ReloadPageAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        IsAdmin = User.IsInRole(Roles.Admin);
        var (items, total) = await _resources.ListAsync(
            userId, IsAdmin, Purpose, Search, Math.Max(1, PageNumber));
        Items = items;
        Total = total;
        return Page();
    }

    public static int PageCount(int total)
    {
        return Math.Max(1, (int)Math.Ceiling(total / (double)ResourceService.PageSize));
    }
}
