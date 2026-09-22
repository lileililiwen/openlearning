using Xunit;

namespace OpenLearning.UnitTests.Web;

/// <summary>
/// Smoke checks for the responsive design system: breakpoint tokens, narrow-viewport
/// sidebar collapse, and shared state primitive styling must all be present so that
/// the learner shell adapts to narrow viewports without horizontal scrolling.
/// </summary>
public sealed class ResponsiveCssTests
{
    private static string LoadCss(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(root, "src", "OpenLearning.Web", "wwwroot", "css", relativePath);
        Assert.True(File.Exists(path), $"CSS not found: {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void Site_DefinesBreakpointTokens()
    {
        var css = LoadCss("site.css");

        Assert.Contains("--bp-narrow:", css);
        Assert.Contains("--bp-medium:", css);
        Assert.Contains("--bp-wide:", css);
    }

    [Fact]
    public void SiteChrome_CollapsesSidebarAtNarrowViewport()
    {
        var css = LoadCss("site-chrome.css");

        Assert.Contains("@media (max-width: 991.98px)", css);
        Assert.Contains(".app-sidebar", css);
        Assert.Contains(".sidebar-backdrop", css);
    }

    [Fact]
    public void Site_StylesSharedStatePrimitives()
    {
        var css = LoadCss("site.css");

        Assert.Contains(".state-loading", css);
        Assert.Contains(".state-empty", css);
        Assert.Contains(".state-error", css);
    }

    [Fact]
    public void SiteChrome_DefinesSkipLink()
    {
        var css = LoadCss("site-chrome.css");

        Assert.Contains(".skip-link", css);
    }
}
