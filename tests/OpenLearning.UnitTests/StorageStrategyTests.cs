using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenLearning.Data;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;
using OpenLearning.SystemConfig.Models;
using OpenLearning.SystemConfig.Services;
using Xunit;

namespace OpenLearning.UnitTests.Storage;

public sealed class StorageStrategyTests
{
    private static readonly string[] _videoExtensionsOverride = [".mp4", ".mkv"];

    private static ApplicationDbContext NewDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static ServiceProvider BuildServices(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddScoped<DbContext>(_ => db);
        services.AddScoped<SystemConfigService>();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHttpClient();
        return services.BuildServiceProvider();
    }

    private static string NewTempDir()
    {
        return Path.Combine(Path.GetTempPath(), "ol-storage-" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public async Task Factory_defaults_to_local_when_no_settings()
    {
        var db = NewDb();
        var provider = await StorageProviderFactory.CreateAsync(
            new ConfigurationBuilder().Build(), new SystemConfigService(db), NewTempDir(), BuildServices(db).GetRequiredService<IHttpClientFactory>());

        Assert.IsType<LocalStorageProvider>(provider);
    }

    [Fact]
    public async Task LazyProvider_resolves_local_and_roundtrips()
    {
        var db = NewDb();
        var root = NewTempDir();
        var sp = BuildServices(db);
        var provider = new LazyStorageProvider(sp, root);

        var key = "video/" + Guid.NewGuid().ToString("N") + ".mp4";
        await using (var stream = new MemoryStream("hello-storage"u8.ToArray()))
        {
            await provider.SaveAsync(stream, key);
        }

        await using (var read = await provider.OpenAsync(key))
        {
            Assert.NotNull(read);
            using var reader = new StreamReader(read);
            Assert.Equal("hello-storage", await reader.ReadToEndAsync());
        }

        await provider.DeleteAsync(key);
        Assert.Null(await provider.OpenAsync(key));
    }

    [Fact]
    public async Task Limits_read_system_config_overrides()
    {
        var db = NewDb();
        db.Settings.AddRange(
            new Setting { Key = "Storage.Limits.Video.MaxBytes", Value = "9999999" },
            new Setting { Key = "Storage.Limits.Video.Extensions", Value = ".mp4,.mkv" });
        await db.SaveChangesAsync();

        var config = new SystemConfigService(db);
        var storage = new StorageService(db, new LocalStorageProvider(NewTempDir()), NullTranscoder(), config);

        var (maxBytes, extensions) = await storage.GetLimitsAsync(FilePurpose.Video);

        Assert.Equal(9999999, maxBytes);
        Assert.Equal(_videoExtensionsOverride, extensions);
    }

    [Fact]
    public async Task Limits_fall_back_to_defaults_without_config()
    {
        var db = NewDb();
        var storage = new StorageService(db, new LocalStorageProvider(NewTempDir()), NullTranscoder(), new SystemConfigService(db));

        var (maxBytes, extensions) = await storage.GetLimitsAsync(FilePurpose.Avatar);

        Assert.Equal(2L * 1024 * 1024, maxBytes);
        Assert.Contains(".png", extensions);
    }

    private static MediaTranscoder NullTranscoder()
    {
        return new MediaTranscoder(null!, null!, NullLogger<MediaTranscoder>.Instance);
    }
}
