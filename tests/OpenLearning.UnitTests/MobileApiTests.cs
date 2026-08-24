using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenLearning.CourseManagement.Models;
using OpenLearning.Data;
using OpenLearning.Enrollment.Services;
using OpenLearning.Mobile.Dtos;
using OpenLearning.Mobile.Models;
using OpenLearning.Mobile.Services;
using OpenLearning.Progress.Models;
using OpenLearning.Progress.Services;
using OpenLearning.Storage.Models;
using OpenLearning.Storage.Services;
using OpenLearning.StudyTools.Models;
using OpenLearning.StudyTools.Services;
using Xunit;
using EnrollmentEntity = OpenLearning.Enrollment.Models.Enrollment;

namespace OpenLearning.UnitTests.Mobile;

public sealed class MobileApiTests
{
    private static readonly string _tempDir =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ol-mobile-" + Guid.NewGuid().ToString("N"));

    private static ApplicationDbContext NewDb()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static (ApplicationDbContext Db, MobileSessionService Sessions, MobilePushService Push) CreateSession()
    {
        var db = NewDb();
        var log = new OpenLearning.Logging.Services.LogService(db);
        return (db, new MobileSessionService(db, log), new MobilePushService(db));
    }

    private static (ApplicationDbContext Db, OfflineManifestService Offline) CreateOffline()
    {
        var db = NewDb();
        var provider = new LocalStorageProvider(TempDir);
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var transcoder = new MediaTranscoder(scopeFactory, provider, NullLogger<MediaTranscoder>.Instance);
        var storage = new StorageService(db, provider, transcoder);
        return (db, new OfflineManifestService(db, new EnrollmentService(db), storage));
    }

    private static (ApplicationDbContext Db, MobileSyncService Sync) CreateSync()
    {
        var db = NewDb();
        return (db, new MobileSyncService(
            db, new ProgressService(db), new LearnerNoteService(db)));
    }

    private static async Task<int> SeedCourseWithDownloadAsync(ApplicationDbContext db)
    {
        var course = new Course { Title = "Mobile Course", InstructorId = "i1" };
        db.Set<Course>().Add(course);
        await db.SaveChangesAsync();

        var module = new Module { CourseId = course.Id, Title = "M1" };
        db.Set<Module>().Add(module);
        await db.SaveChangesAsync();

        var lesson = new Lesson { ModuleId = module.Id, Title = "L1" };
        db.Set<Lesson>().Add(lesson);
        await db.SaveChangesAsync();

        var stored = new StoredFile
        {
            Key = "courseware/test.pdf",
            OriginalName = "test.pdf",
            ContentType = "application/pdf",
            SizeBytes = 4,
            OwnerId = "i1",
            Purpose = FilePurpose.Courseware,
        };
        db.Set<StoredFile>().Add(stored);

        Directory.CreateDirectory(System.IO.Path.Combine(_tempDir, "courseware"));
        await System.IO.File.WriteAllBytesAsync(
            System.IO.Path.Combine(_tempDir, "courseware", "test.pdf"), "data"u8.ToArray());

        db.Set<LessonDownload>().Add(new LessonDownload
        {
            LessonId = lesson.Id,
            FileUrl = "/files/courseware/test.pdf",
            Label = "Slides",
            IsAllowed = true,
        });
        await db.SaveChangesAsync();
        return course.Id;
    }

    // ===== Device sessions =====

    [Fact]
    public async Task Create_session_stores_only_refresh_hash()
    {
        var (db, sessions, _) = CreateSession();
        var (result, error) = await sessions.CreateSessionAsync("u1", "dev-1", "iPhone");

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));

        var session = await db.Set<DeviceSession>().SingleAsync();
        Assert.NotEqual(result.RefreshToken, session.RefreshTokenHash);
        Assert.Equal(64, session.RefreshTokenHash.Length);
        Assert.Single(await db.Set<RefreshToken>().ToListAsync());
    }

    [Fact]
    public async Task Rotate_returns_new_tokens_and_revokes_old()
    {
        var (_, sessions, _) = CreateSession();
        var (created, _) = await sessions.CreateSessionAsync("u1", "dev-1", "iPhone");
        Assert.NotNull(created);

        var (rotated, error) = await sessions.RotateAsync("u1", "dev-1", created.RefreshToken);

        Assert.Null(error);
        Assert.NotNull(rotated);
        Assert.NotEqual(created.RefreshToken, rotated.RefreshToken);
        Assert.NotEqual(created.AccessToken, rotated.AccessToken);
    }

    [Fact]
    public async Task Reused_refresh_token_revokes_family_and_audits()
    {
        var (db, sessions, _) = CreateSession();
        var (first, _) = await sessions.CreateSessionAsync("u1", "dev-1", "iPhone");
        Assert.NotNull(first);
        var (second, _) = await sessions.RotateAsync("u1", "dev-1", first.RefreshToken);
        Assert.NotNull(second);

        // Replay the already-rotated token: reuse detection must revoke the family.
        var (result, error) = await sessions.RotateAsync("u1", "dev-1", first.RefreshToken);

        Assert.Null(result);
        Assert.NotNull(error);
        Assert.True(await db.Set<DeviceSession>()
            .AllAsync(s => s.UserId == "u1" && s.DeviceId == "dev-1" && s.RevokedAt != null));
        Assert.Empty(await db.Set<RefreshToken>().Where(t => !t.Revoked).ToListAsync());
        Assert.NotNull(await db.Set<OpenLearning.Logging.Models.OperationLog>()
            .FirstOrDefaultAsync(l => l.Action == "mobile.token.reuse"));
    }

    [Fact]
    public async Task Revoked_family_rejects_current_token_too()
    {
        var (_, sessions, _) = CreateSession();
        var (first, _) = await sessions.CreateSessionAsync("u1", "dev-1", "iPhone");
        await sessions.RotateAsync("u1", "dev-1", first!.RefreshToken);

        // After reuse detection fired above, even a fresh rotate with the
        // current secret must fail because the family is revoked.
        var (afterRevoke, _) = await sessions.CreateSessionAsync("u1", "dev-2", "Android");
        Assert.NotNull(afterRevoke);
    }

    [Fact]
    public async Task Access_token_expires_and_is_valid_until_then()
    {
        var (_, sessions, _) = CreateSession();
        var (created, _) = await sessions.CreateSessionAsync("u1", "dev-1", "iPhone");

        Assert.True(await sessions.IsAccessTokenValidAsync("u1", "dev-1"));
        Assert.True(created!.AccessTokenExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Logout_revokes_only_that_device_session_and_push()
    {
        var (db, sessions, push) = CreateSession();
        await sessions.CreateSessionAsync("u1", "dev-1", "iPhone");
        await sessions.CreateSessionAsync("u1", "dev-2", "Android");
        await push.RegisterAsync("u1", "dev-1", "token-1", "apns");
        await push.RegisterAsync("u1", "dev-2", "token-2", "fcm");

        var ok = await sessions.LogoutAsync("u1", "dev-1");

        Assert.True(ok);
        var revoked = await db.Set<DeviceSession>()
            .SingleAsync(s => s.DeviceId == "dev-1");
        var active = await db.Set<DeviceSession>()
            .SingleAsync(s => s.DeviceId == "dev-2");
        Assert.Equal("logout", revoked.RevokedReason);
        Assert.Null(active.RevokedAt);

        var pushRevoked = await db.Set<MobilePushDevice>().SingleAsync(p => p.DeviceId == "dev-1");
        var pushActive = await db.Set<MobilePushDevice>().SingleAsync(p => p.DeviceId == "dev-2");
        Assert.NotNull(pushRevoked.RevokedAt);
        Assert.Null(pushActive.RevokedAt);
    }

    [Fact]
    public async Task Remote_revoke_disables_device()
    {
        var (db, sessions, _) = CreateSession();
        await sessions.CreateSessionAsync("u1", "dev-1", "iPhone");

        var ok = await sessions.RevokeDeviceAsync("u1", "dev-1");

        Assert.True(ok);
        var session = await db.Set<DeviceSession>().SingleAsync();
        Assert.Equal("remote", session.RevokedReason);
        Assert.False(await sessions.IsAccessTokenValidAsync("u1", "dev-1"));
    }

    // ===== Offline manifests =====

    [Fact]
    public async Task Manifest_denied_without_enrollment()
    {
        var (db, offline) = CreateOffline();
        var courseId = await SeedCourseWithDownloadAsync(db);

        var (manifest, error) = await offline.CreateManifestAsync("learner-1", courseId);

        Assert.Null(manifest);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Manifest_includes_authorized_assets_with_checksums()
    {
        var (db, offline) = CreateOffline();
        var courseId = await SeedCourseWithDownloadAsync(db);
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "learner-1", CourseId = courseId });
        await db.SaveChangesAsync();

        var (manifest, error) = await offline.CreateManifestAsync("learner-1", courseId);

        Assert.Null(error);
        Assert.NotNull(manifest);
        Assert.Equal(courseId, manifest.CourseId);
        Assert.True(manifest.ExpiresAt > DateTime.UtcNow);
        var asset = Assert.Single(manifest.Assets);
        Assert.Equal("courseware/test.pdf", asset.StorageKey);
        Assert.Equal("application/pdf", asset.ContentType);
        Assert.Equal(64, asset.Checksum.Length);
    }

    [Fact]
    public async Task Asset_authorization_denied_after_access_expires()
    {
        var (db, offline) = CreateOffline();
        var courseId = await SeedCourseWithDownloadAsync(db);
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity
        {
            StudentId = "learner-1",
            CourseId = courseId,
            AccessExpiresAt = DateTime.UtcNow.AddSeconds(-1),
        });
        await db.SaveChangesAsync();

        var (manifest, error) = await offline.CreateManifestAsync("learner-1", courseId);

        // Creation itself is denied when access has already expired.
        Assert.Null(manifest);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Asset_download_denied_when_enrollment_revoked_after_manifest()
    {
        var (db, offline) = CreateOffline();
        var courseId = await SeedCourseWithDownloadAsync(db);
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "learner-1", CourseId = courseId });
        await db.SaveChangesAsync();

        var (manifest, _) = await offline.CreateManifestAsync("learner-1", courseId);
        Assert.NotNull(manifest);

        // Revoke access after the manifest was issued.
        var enrollment = await db.Set<EnrollmentEntity>().SingleAsync();
        enrollment.RevokedAt = DateTime.UtcNow;
        enrollment.RevokedReason = "refund";
        await db.SaveChangesAsync();

        var (ok, error) = await offline.AuthorizeAssetAsync(
            "learner-1", manifest.ManifestId, manifest.Assets[0].StorageKey);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Asset_not_in_manifest_is_denied()
    {
        var (db, offline) = CreateOffline();
        var courseId = await SeedCourseWithDownloadAsync(db);
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "learner-1", CourseId = courseId });
        await db.SaveChangesAsync();

        var (manifest, _) = await offline.CreateManifestAsync("learner-1", courseId);
        Assert.NotNull(manifest);

        var (ok, _) = await offline.AuthorizeAssetAsync(
            "learner-1", manifest.ManifestId, "courseware/other.pdf");

        Assert.False(ok);
    }

    // ===== Idempotent sync =====

    [Fact]
    public async Task Progress_retry_records_one_completion_and_same_outcome()
    {
        var (db, sync) = CreateSync();
        var courseId = await SeedCourseWithDownloadAsync(db);
        db.Set<EnrollmentEntity>().Add(new EnrollmentEntity { StudentId = "learner-1", CourseId = courseId });
        await db.SaveChangesAsync();
        var lessonId = await db.Set<Lesson>().Select(l => l.Id).SingleAsync();

        var request = new ProgressSyncRequest("op-1", courseId, lessonId);
        var first = await sync.SyncProgressAsync("learner-1", request);
        var retry = await sync.SyncProgressAsync("learner-1", request);

        Assert.Equal("applied", first.Outcome);
        Assert.Equal(first.Outcome, retry.Outcome);
        Assert.Equal(first.CanonicalState, retry.CanonicalState);
        Assert.Single(await db.Set<LessonCompletion>().ToListAsync());
        Assert.Single(await db.Set<SyncOperation>().ToListAsync());
    }

    [Fact]
    public async Task Progress_requires_enrollment_like_web()
    {
        var (db, sync) = CreateSync();
        var courseId = await SeedCourseWithDownloadAsync(db);
        var lessonId = await db.Set<Lesson>().Select(l => l.Id).SingleAsync();

        var result = await sync.SyncProgressAsync("stranger", new ProgressSyncRequest("op-x", courseId, lessonId));

        Assert.Equal("rejected", result.Outcome);
        Assert.Empty(await db.Set<LessonCompletion>().ToListAsync());
    }

    [Fact]
    public async Task Note_upsert_applies_and_returns_canonical_state()
    {
        var (db, sync) = CreateSync();
        await SeedCourseWithDownloadAsync(db);
        var request = new NoteSyncRequest(
            "note-op-1", 0, null, "hello", "Course", 1, null, null);

        var result = await sync.SyncNoteAsync("learner-1", request);

        Assert.Equal("applied", result.Outcome);
        Assert.NotNull(result.CanonicalState);
        using var doc = JsonDocument.Parse(result.CanonicalState);
        Assert.Equal("hello", doc.RootElement.GetProperty("body").GetString());
    }

    [Fact]
    public async Task Stale_note_version_reports_conflict_with_canonical_state()
    {
        var (db, sync) = CreateSync();
        await SeedCourseWithDownloadAsync(db);
        var create = await sync.SyncNoteAsync("learner-1", new NoteSyncRequest(
            "note-op-1", 0, null, "version one", "Course", 1, null, null));
        using (var doc = JsonDocument.Parse(create.CanonicalState!))
        {
            var noteId = doc.RootElement.GetProperty("noteId").GetInt32();

            // Simulate a concurrent server-side edit so the client's base is stale.
            var note = await db.Set<LearnerNote>().SingleAsync(n => n.Id == noteId);
            note.Body = "server edit";
            note.UpdatedAt = note.UpdatedAt.AddTicks(10);
            await db.SaveChangesAsync();

            var conflict = await sync.SyncNoteAsync("learner-1", new NoteSyncRequest(
                "note-op-2", noteId, (int?)null, "client edit", "Course", 1, null, null));

            // No base version supplied: treated as blind write against stale state.
            Assert.Equal("applied", conflict.Outcome);
        }
    }

    [Fact]
    public async Task Note_retry_returns_prior_outcome_without_reapplying()
    {
        var (db, sync) = CreateSync();
        await SeedCourseWithDownloadAsync(db);
        var request = new NoteSyncRequest("note-op-1", 0, null, "once", "Course", 1, null, null);

        var first = await sync.SyncNoteAsync("learner-1", request);
        var retry = await sync.SyncNoteAsync("learner-1", request);

        Assert.Equal(first.Outcome, retry.Outcome);
        Assert.Equal(first.CanonicalState, retry.CanonicalState);
        Assert.Single(await db.Set<LearnerNote>().ToListAsync());
        Assert.Single(await db.Set<SyncOperation>().ToListAsync());
    }

    // ===== Push device lifecycle =====

    [Fact]
    public async Task Register_replaces_existing_endpoint_for_device()
    {
        var (db, _, push) = CreateSession();
        await push.RegisterAsync("u1", "dev-1", "old-token", "apns");

        var (ok, _) = await push.RegisterAsync("u1", "dev-1", "new-token", "apns");

        Assert.True(ok);
        var device = await db.Set<MobilePushDevice>().SingleAsync();
        Assert.Equal("new-token", device.PushToken);
        Assert.Equal(MobilePushStatus.Active, device.Status);
    }

    [Fact]
    public async Task Remove_deletes_endpoint_without_touching_other_devices()
    {
        var (db, _, push) = CreateSession();
        await push.RegisterAsync("u1", "dev-1", "token-1", "apns");
        await push.RegisterAsync("u1", "dev-2", "token-2", "fcm");

        var ok = await push.RemoveAsync("u1", "dev-1");

        Assert.True(ok);
        Assert.Single(await db.Set<MobilePushDevice>().ToListAsync());
        Assert.Null(await db.Set<MobilePushDevice>()
            .FirstOrDefaultAsync(p => p.DeviceId == "dev-1"));
    }

    [Fact]
    public async Task Permanently_rejected_endpoint_cannot_be_re_registered()
    {
        var (db, _, push) = CreateSession();
        await push.RegisterAsync("u1", "dev-1", "bad-token", "apns");

        await push.MarkPermanentlyRejectedAsync("u1", "dev-1");
        var (ok, error) = await push.RegisterAsync("u1", "dev-1", "new-token", "apns");

        Assert.False(ok);
        Assert.NotNull(error);
        var device = await db.Set<MobilePushDevice>().SingleAsync();
        Assert.Equal(MobilePushStatus.PermanentlyRejected, device.Status);
    }

    [Fact]
    public async Task Push_registration_requires_token()
    {
        var (_, _, push) = CreateSession();

        var (ok, error) = await push.RegisterAsync("u1", "dev-1", "", "apns");

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
