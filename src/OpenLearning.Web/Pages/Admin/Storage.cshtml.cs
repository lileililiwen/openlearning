using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenLearning.Auth;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Services;

namespace OpenLearning.Web.Pages.Admin;

[Authorize(Policy = Policies.RequireAdmin)]
public class StorageModel : PageModel
{
    private readonly SystemConfigService _config;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly System.Text.RegularExpressions.Regex _extensionPattern =
        new(@"^\.[A-Za-z0-9]{1,10}$", System.Text.RegularExpressions.RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public StorageModel(
        SystemConfigService config,
        IConfiguration configuration,
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _configuration = configuration;
        _env = env;
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public string Provider { get; set; } = "Local";

    [BindProperty]
    public string S3Endpoint { get; set; } = string.Empty;

    [BindProperty]
    public string S3Bucket { get; set; } = string.Empty;

    [BindProperty]
    public string S3AccessKeyId { get; set; } = string.Empty;

    [BindProperty]
    public string S3Secret { get; set; } = string.Empty;

    [BindProperty]
    public string S3Region { get; set; } = "us-east-1";

    [BindProperty]
    public bool S3PathStyle { get; set; }

    [BindProperty]
    public bool S3ClearSecret { get; set; }

    [BindProperty]
    public string OssEndpoint { get; set; } = string.Empty;

    [BindProperty]
    public string OssBucket { get; set; } = string.Empty;

    [BindProperty]
    public string OssAccessKeyId { get; set; } = string.Empty;

    [BindProperty]
    public string OssSecret { get; set; } = string.Empty;

    [BindProperty]
    public bool OssClearSecret { get; set; }

    [BindProperty]
    public Dictionary<string, string> LimitMaxBytesMb { get; set; } = new();

    [BindProperty]
    public Dictionary<string, string> LimitExtensions { get; set; } = new();

    public bool S3HasSecret { get; set; }

    public bool OssHasSecret { get; set; }

    public string LocalRoot { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string? MessageType { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var validationError = await ValidateAndSaveAsync();
        if (validationError is not null)
        {
            Message = validationError;
            MessageType = "danger";
        }
        else
        {
            Message = "已保存。策略与连接配置将在应用重启后生效。";
            MessageType = "success";
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostTestAsync()
    {
        var testError = string.Empty;
        try
        {
            var localRoot = _configuration["Storage:Root"] ?? Path.Combine(_env.ContentRootPath, "storage");
            var provider = await StorageProviderFactory.CreateAsync(_configuration, _config, localRoot, _httpClientFactory);
            var probeKey = $"storage-probe/{Guid.NewGuid():N}";
            await using (var probe = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("probe")))
            {
                await provider.SaveAsync(probe, probeKey);
            }

            var read = await provider.OpenAsync(probeKey);
            if (read is null)
            {
                testError = "连接测试失败：无法读回探针对象。";
            }
            else
            {
                await read.DisposeAsync();
                await provider.DeleteAsync(probeKey);
            }
        }
        catch (Exception ex)
        {
            testError = $"连接测试失败：{ex.Message}";
        }

        await LoadAsync();
        Message = string.IsNullOrEmpty(testError)
            ? $"连接测试成功（当前保存的后端：{Provider}）。"
            : testError;
        MessageType = string.IsNullOrEmpty(testError) ? "success" : "danger";
        return Page();
    }

    private async Task<string?> ValidateAndSaveAsync()
    {
        var provider = Provider.Trim();
        if (provider is not ("Local" or "S3" or "MinIO" or "AliyunOss"))
        {
            return "存储策略必须是 Local、S3、MinIO 或 AliyunOss。";
        }

        if ((provider is "S3" or "MinIO") && (string.IsNullOrWhiteSpace(S3Endpoint) || string.IsNullOrWhiteSpace(S3Bucket)))
        {
            return "S3/MinIO 端点与桶名必填。";
        }

        if (provider == "AliyunOss" && (string.IsNullOrWhiteSpace(OssEndpoint) || string.IsNullOrWhiteSpace(OssBucket)))
        {
            return "OSS 端点与桶名必填。";
        }

        await _config.SetAsync("Storage.Provider", provider);
        await _config.SetAsync("Storage.S3.Endpoint", (S3Endpoint ?? string.Empty).Trim());
        await _config.SetAsync("Storage.S3.Bucket", (S3Bucket ?? string.Empty).Trim());
        await _config.SetAsync("Storage.S3.AccessKeyId", (S3AccessKeyId ?? string.Empty).Trim());
        await _config.SetAsync("Storage.S3.Region", string.IsNullOrWhiteSpace(S3Region) ? "us-east-1" : S3Region.Trim());
        await _config.SetAsync("Storage.S3.PathStyle", S3PathStyle.ToString());
        await _config.SetAsync("Storage.Oss.Endpoint", (OssEndpoint ?? string.Empty).Trim());
        await _config.SetAsync("Storage.Oss.Bucket", (OssBucket ?? string.Empty).Trim());
        await _config.SetAsync("Storage.Oss.AccessKeyId", (OssAccessKeyId ?? string.Empty).Trim());

        // Secrets: a non-empty value replaces; the clear flag deletes; empty keeps the saved value.
        if (!string.IsNullOrWhiteSpace(S3Secret))
        {
            await _config.SetAsync("Storage.S3.SecretAccessKey", S3Secret.Trim());
        }
        else if (S3ClearSecret)
        {
            await _config.SetAsync("Storage.S3.SecretAccessKey", string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(OssSecret))
        {
            await _config.SetAsync("Storage.Oss.SecretAccessKey", OssSecret.Trim());
        }
        else if (OssClearSecret)
        {
            await _config.SetAsync("Storage.Oss.SecretAccessKey", string.Empty);
        }

        foreach (var purpose in Enum.GetValues<FilePurpose>())
        {
            var key = purpose.ToString();
            var (_, defaultExtensions) = StorageService.GetLimits(purpose);
            var maxMbText = LimitMaxBytesMb.GetValueOrDefault(key) ?? string.Empty;
            if (!int.TryParse(maxMbText, out var maxMb) || maxMb < 1)
            {
                return $"{purpose} 的上限必须是不小于 1 的整数（MB）。";
            }

            var extensionsText = LimitExtensions.GetValueOrDefault(key) ?? string.Empty;
            if (extensionsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(ext => !_extensionPattern.IsMatch(ext.Trim())))
            {
                return $"{purpose} 的扩展名列表格式无效（如 .jpg,.png）。";
            }

            await _config.SetAsync($"Storage.Limits.{key}.MaxBytes", ((long)maxMb * 1024 * 1024).ToString(System.Globalization.CultureInfo.InvariantCulture));
            await _config.SetAsync($"Storage.Limits.{key}.Extensions", string.IsNullOrWhiteSpace(extensionsText)
                ? string.Join(",", defaultExtensions)
                : extensionsText);
        }

        return null;
    }

    private async Task LoadAsync()
    {
        Provider = await _config.GetStringAsync("Storage.Provider", "Local");
        S3Endpoint = await _config.GetStringAsync("Storage.S3.Endpoint", string.Empty);
        S3Bucket = await _config.GetStringAsync("Storage.S3.Bucket", string.Empty);
        S3AccessKeyId = await _config.GetStringAsync("Storage.S3.AccessKeyId", string.Empty);
        S3Region = await _config.GetStringAsync("Storage.S3.Region", "us-east-1");
        S3PathStyle = await _config.GetBoolAsync("Storage.S3.PathStyle", false);
        S3HasSecret = !string.IsNullOrWhiteSpace(await _config.GetAsync("Storage.S3.SecretAccessKey"));
        OssEndpoint = await _config.GetStringAsync("Storage.Oss.Endpoint", string.Empty);
        OssBucket = await _config.GetStringAsync("Storage.Oss.Bucket", string.Empty);
        OssAccessKeyId = await _config.GetStringAsync("Storage.Oss.AccessKeyId", string.Empty);
        OssHasSecret = !string.IsNullOrWhiteSpace(await _config.GetAsync("Storage.Oss.SecretAccessKey"));
        LocalRoot = _configuration["Storage:Root"] ?? Path.Combine(_env.ContentRootPath, "storage");

        LimitMaxBytesMb = new Dictionary<string, string>();
        LimitExtensions = new Dictionary<string, string>();
        foreach (var purpose in Enum.GetValues<FilePurpose>())
        {
            var key = purpose.ToString();
            var (defaultMax, defaultExtensions) = StorageService.GetLimits(purpose);
            var maxBytes = await _config.GetIntAsync($"Storage.Limits.{key}.MaxBytes", (int)defaultMax);
            LimitMaxBytesMb[key] = (maxBytes / (1024 * 1024)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            LimitExtensions[key] = await _config.GetStringAsync($"Storage.Limits.{key}.Extensions", string.Join(",", defaultExtensions));
        }
    }
}
