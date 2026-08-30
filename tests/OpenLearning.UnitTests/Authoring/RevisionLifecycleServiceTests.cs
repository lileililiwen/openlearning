using Microsoft.EntityFrameworkCore;
using OpenLearning.Authoring.Models;
using OpenLearning.Authoring.Services;
using OpenLearning.Data;
using Xunit;

namespace OpenLearning.UnitTests.Authoring;

public sealed class RevisionLifecycleServiceTests
{
    private static ApplicationDbContext NewDb()
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }


    private static RevisionPatch ValidPatch()
    {
        return new RevisionPatch
        {
            Title = "Intro to Rust",
            Summary = "Systems programming basics.",
            Language = "en",
            Level = "Beginner",
            ContentSnapshotJson = "{\"Modules\":[{\"Title\":\"M1\",\"Lessons\":[{\"Title\":\"L1\",\"Type\":\"video\"}]}]}",
        };
    }

    [Fact]
    public async Task EnsureDraft_creates_draft_without_an_active_revision()
    {
        await using var db = NewDb();
        var svc = new RevisionLifecycleService(db);

        var draft = await svc.EnsureDraftAsync(1, "owner1");
        var active = await svc.GetActiveRevisionAsync(1);

        Assert.Equal("owner1", draft.OwnerId);
        Assert.Equal(RevisionState.Draft, draft.State);
        Assert.Null(active); // learner sees nothing until published
    }

    [Fact]
    public async Task EditDraft_does_not_change_the_published_revision()
    {
        await using var db = NewDb();
        var svc = new RevisionLifecycleService(db);

        await svc.EnsureDraftAsync(1, "owner1");
        var draft = await svc.EditDraftAsync(1, "owner1", ValidPatch());
        var active = await svc.GetActiveRevisionAsync(1);

        Assert.Equal("Intro to Rust", draft.Title);
        Assert.Null(active);
    }

    [Fact]
    public async Task ValidateDraft_reports_field_errors_for_invalid_content()
    {
        await using var db = NewDb();
        var svc = new RevisionLifecycleService(db);

        await svc.EnsureDraftAsync(1, "owner1");
        await svc.EditDraftAsync(1, "owner1", new RevisionPatch { Title = "", Summary = "", Language = "", Level = "" });

        var outcome = await svc.ValidateDraftAsync(1, "owner1");

        Assert.False(outcome.IsValid);
        Assert.NotEmpty(outcome.Errors);
    }

    [Fact]
    public async Task Publish_promotes_draft_and_keeps_editing_draft_separate()
    {
        await using var db = NewDb();
        var svc = new RevisionLifecycleService(db);

        await svc.EnsureDraftAsync(1, "owner1");
        await svc.EditDraftAsync(1, "owner1", ValidPatch());
        var result = await svc.PublishAsync(1, "owner1");

        Assert.True(result.Succeeded);
        var active = await svc.GetActiveRevisionAsync(1);
        Assert.NotNull(active);
        Assert.Equal(RevisionState.Published, active.State);

        // further edits go to a new draft, not the active revision
        await svc.EditDraftAsync(1, "owner1", new RevisionPatch { Title = "Intro to Rust (2nd ed)" });
        var active2 = await svc.GetActiveRevisionAsync(1);
        Assert.Equal("Intro to Rust", active2!.Title);
    }

    [Fact]
    public async Task Publish_rejects_invalid_draft()
    {
        await using var db = NewDb();
        var svc = new RevisionLifecycleService(db);

        await svc.EnsureDraftAsync(1, "owner1");
        var result = await svc.PublishAsync(1, "owner1");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Non_owner_is_denied_preview_and_rollback()
    {
        await using var db = NewDb();
        var svc = new RevisionLifecycleService(db);

        await svc.EnsureDraftAsync(1, "owner1");
        await svc.EditDraftAsync(1, "owner1", ValidPatch());
        var published = await svc.PublishAsync(1, "owner1");

        await Assert.ThrowsAsync<UnauthorizedRevisionOperationException>(() => svc.GetPreviewAsync(1, "attacker"));
        await Assert.ThrowsAsync<UnauthorizedRevisionOperationException>(
            () => svc.RollbackAsync(1, "attacker", published.ActiveRevisionId ?? 0));
    }

    [Fact]
    public async Task Rollback_creates_new_draft_from_published_revision_and_preserves_history()
    {
        await using var db = NewDb();
        var svc = new RevisionLifecycleService(db);

        await svc.EnsureDraftAsync(1, "owner1");
        await svc.EditDraftAsync(1, "owner1", ValidPatch());
        var published = await svc.PublishAsync(1, "owner1");

        var draft = await svc.RollbackAsync(1, "owner1", published.ActiveRevisionId ?? 0);

        Assert.Equal(RevisionState.Draft, draft.State);
        Assert.Equal("Intro to Rust", draft.Title);

        // the previously published revision is still readable as the active one
        var active = await svc.GetActiveRevisionAsync(1);
        Assert.NotNull(active);
    }
}
