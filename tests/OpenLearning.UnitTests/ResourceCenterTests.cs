using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenLearning.Auth.Models;
using OpenLearning.Data;
using OpenLearning.ResourceCenter.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;
using Xunit;

namespace OpenLearning.UnitTests.ResourceCenter;

public sealed class ResourceCenterTests
{
    private static (ApplicationDbContext Db, ResourceService Service, string TempDir) Create()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var tempDir = Path.Combine(Path.GetTempPath(), "ol-resource-" + Guid.NewGuid().ToString("N"));
        var provider = new LocalStorageProvider(tempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddScoped<DbContext>(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        var service = new ResourceService(db, storage);
        return (db, service, tempDir);
    }

    private static async Task SeedUsersAsync(ApplicationDbContext db)
    {
        db.Users.AddRange(
            new ApplicationUser { Id = "u1", UserName = "u1@x.com", Email = "u1@x.com", DisplayName = "User One" },
            new ApplicationUser { Id = "u2", UserName = "u2@x.com", Email = "u2@x.com", DisplayName = "User Two" });
        await db.SaveChangesAsync();
    }

    private static FormFile MakeFile(byte[] bytes, string name, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    [Fact]
    public async Task Upload_image_video_document_accepted_others_rejected()
    {
        var (db, service, _) = Create();
        await SeedUsersAsync(db);

        var image = await service.UploadAsync("u1", FilePurpose.Image, MakeFile("x"u8.ToArray(), "a.png", "image/png"));
        Assert.Null(image.Error);
        Assert.Equal(FilePurpose.Image, image.File!.Purpose);

        var video = await service.UploadAsync("u1", FilePurpose.Video, MakeFile("x"u8.ToArray(), "a.mp4", "video/mp4"));
        Assert.Null(video.Error);

        var doc = await service.UploadAsync("u1", FilePurpose.Document, MakeFile("x"u8.ToArray(), "a.pdf", "application/pdf"));
        Assert.Null(doc.Error);

        var rejected = await service.UploadAsync("u1", FilePurpose.Answer, MakeFile("x"u8.ToArray(), "a.pdf", "application/pdf"));
        Assert.NotNull(rejected.Error);
    }

    [Fact]
    public async Task List_shows_own_and_shared_but_not_others_private()
    {
        var (db, service, _) = Create();
        await SeedUsersAsync(db);
        await service.UploadAsync("u1", FilePurpose.Image, MakeFile("x"u8.ToArray(), "own.png", "image/png"));
        await service.UploadAsync("u2", FilePurpose.Image, MakeFile("x"u8.ToArray(), "other.png", "image/png"));
        await service.UploadAsync("u2", FilePurpose.Image, MakeFile("x"u8.ToArray(), "shared.png", "image/png"));

        var shared = await db.StoredFiles.FirstAsync(f => f.OriginalName == "shared.png");
        var (ok, error) = await service.SetSharedAsync(shared.Key, "admin", isAdmin: true, shared: true);
        Assert.True(ok);
        Assert.Null(error);

        var (items, total) = await service.ListAsync("u1", isAdmin: false, null, null, 1);
        Assert.Equal(2, total); // own.png + shared.png
        Assert.Contains(items, i => i.File.OriginalName == "own.png");
        Assert.Contains(items, i => i.File.OriginalName == "shared.png");
        Assert.DoesNotContain(items, i => i.File.OriginalName == "other.png");

        var (_, adminTotal) = await service.ListAsync("u1", isAdmin: true, null, null, 1);
        Assert.Equal(3, adminTotal);
    }

    [Fact]
    public async Task Delete_owner_ok_non_owner_denied()
    {
        var (db, service, _) = Create();
        await SeedUsersAsync(db);
        var (file, _) = await service.UploadAsync("u1", FilePurpose.Image, MakeFile("x"u8.ToArray(), "a.png", "image/png"));

        var denied = await service.DeleteAsync(file!.Key, "u2", isAdmin: false);
        Assert.False(denied.Ok);

        var ok = await service.DeleteAsync(file.Key, "u1", isAdmin: false);
        Assert.True(ok.Ok);
        Assert.False(await db.StoredFiles.AnyAsync(f => f.Key == file.Key));
    }

    [Fact]
    public async Task Share_admin_only_and_private_rejected()
    {
        var (db, service, _) = Create();
        await SeedUsersAsync(db);
        var (file, _) = await service.UploadAsync("u1", FilePurpose.Image, MakeFile("x"u8.ToArray(), "a.png", "image/png"));
        var privateFile = new StoredFile
        {
            Key = "answer/private-answer.pdf",
            OriginalName = "b.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1,
            OwnerId = "u1",
            Purpose = FilePurpose.Answer,
            IsPrivate = true,
        };
        db.StoredFiles.Add(privateFile);
        await db.SaveChangesAsync();

        var nonAdmin = await service.SetSharedAsync(file!.Key, "u1", isAdmin: false, shared: true);
        Assert.False(nonAdmin.Ok);

        var ok = await service.SetSharedAsync(file.Key, "admin", isAdmin: true, shared: true);
        Assert.True(ok.Ok);
        Assert.True((await db.StoredFiles.FirstAsync(f => f.Key == file.Key)).IsShared);

        var privateDenied = await service.SetSharedAsync(privateFile.Key, "admin", isAdmin: true, shared: true);
        Assert.False(privateDenied.Ok);
    }

    [Fact]
    public async Task List_filters_by_purpose_and_search()
    {
        var (db, service, _) = Create();
        await SeedUsersAsync(db);
        await service.UploadAsync("u1", FilePurpose.Image, MakeFile("x"u8.ToArray(), "cat.png", "image/png"));
        await service.UploadAsync("u1", FilePurpose.Video, MakeFile("x"u8.ToArray(), "cat.mp4", "video/mp4"));
        await service.UploadAsync("u1", FilePurpose.Document, MakeFile("x"u8.ToArray(), "notes.pdf", "application/pdf"));

        var (videos, videoTotal) = await service.ListAsync("u1", isAdmin: false, FilePurpose.Video, null, 1);
        Assert.Equal(1, videoTotal);
        Assert.Equal("cat.mp4", videos[0].File.OriginalName);

        var (_, searchTotal) = await service.ListAsync("u1", isAdmin: false, null, "cat", 1);
        Assert.Equal(2, searchTotal);
    }
}
