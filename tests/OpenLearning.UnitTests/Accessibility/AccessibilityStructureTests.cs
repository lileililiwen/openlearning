using HtmlAgilityPack;
using Xunit;

namespace OpenLearning.UnitTests.Accessibility;

/// <summary>
/// Structural a11y checks: validates that key HTML files contain required ARIA
/// attributes, skip links, landmark IDs, and labeled form controls.
/// </summary>
public sealed class AccessibilityStructureTests
{
    private static HtmlDocument LoadPage(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(root, "src", "OpenLearning.Web", "Pages", relativePath);
        Assert.True(File.Exists(path), $"File not found: {path}");
        var doc = new HtmlDocument();
        doc.Load(path);
        return doc;
    }

    [Fact]
    public void Layout_HasSkipLink()
    {
        var doc = LoadPage("Shared/_Layout.cshtml");
        var skipLink = doc.DocumentNode.SelectSingleNode("//a[@class='skip-link']");
        Assert.NotNull(skipLink);
        Assert.Equal("#main", skipLink.GetAttributeValue("href", ""));
    }

    [Fact]
    public void Layout_MainHasIdMain()
    {
        var doc = LoadPage("Shared/_Layout.cshtml");
        var main = doc.DocumentNode.SelectSingleNode("//main[@id='main']");
        Assert.NotNull(main);
    }

    [Fact]
    public void Layout_SidebarHasAriaLabel()
    {
        var doc = LoadPage("Shared/_Layout.cshtml");
        var aside = doc.DocumentNode.SelectSingleNode("//aside[@aria-label='Sidebar']");
        Assert.NotNull(aside);
    }

    [Fact]
    public void Layout_NavHasAriaLabel()
    {
        var doc = LoadPage("Shared/_Layout.cshtml");
        var nav = doc.DocumentNode.SelectSingleNode("//nav[@aria-label='Primary']");
        Assert.NotNull(nav);
    }

    [Fact]
    public void Layout_SidebarToggleHasAriaExpanded()
    {
        var doc = LoadPage("Shared/_Layout.cshtml");
        var btn = doc.DocumentNode.SelectSingleNode("//button[@data-sidebar-open]");
        Assert.NotNull(btn);
        Assert.Equal("false", btn.GetAttributeValue("aria-expanded", ""));
        Assert.Equal("appSidebar", btn.GetAttributeValue("aria-controls", ""));
    }

    [Fact]
    public void ProgressBar_HasAriaAttributes()
    {
        var files = new[]
        {
            "MyCourses.cshtml",
            "Dashboard/Index.cshtml",
            "Courses/Details.cshtml",
            "Courses/Roster/Student.cshtml",
            "Courses/Roster/Index.cshtml",
        };

        foreach (var file in files)
        {
            var doc = LoadPage(file);
            var bars = doc.DocumentNode.SelectNodes("//div[@role='progressbar']");
            Assert.True(bars?.Count > 0, $"No progressbar found in {file}");
            foreach (var bar in bars)
            {
                Assert.True(bar.HasAttributes && bar.Attributes.Contains("aria-valuenow"),
                    $"{file}: progressbar missing aria-valuenow");
                Assert.True(bar.HasAttributes && bar.Attributes.Contains("aria-valuemin"),
                    $"{file}: progressbar missing aria-valuemin");
                Assert.True(bar.HasAttributes && bar.Attributes.Contains("aria-valuemax"),
                    $"{file}: progressbar missing aria-valuemax");
                Assert.True(bar.HasAttributes && bar.Attributes.Contains("aria-label"),
                    $"{file}: progressbar missing aria-label");
            }
        }
    }

    [Fact]
    public void UnlabeledInputs_HaveAriaLabel()
    {
        var files = new[]
        {
            ("Courses/Lessons/View.cshtml", "danmu-input"),
            ("Courses/Details.cshtml", "commentBody"),
            ("Courses/Qa/Index.cshtml", "replyBody"),
            ("Courses/Exams/Take.cshtml", "FillBlank"),
        };

        foreach (var (file, identifier) in files)
        {
            var doc = LoadPage(file);
            var inputs = doc.DocumentNode.SelectNodes("//input[not(@type='hidden')]");
            Assert.True(inputs?.Count > 0, $"No visible inputs found in {file}");
            var hasLabeled = false;
            foreach (var input in inputs)
            {
                var hasAriaLabel = input.HasAttributes && input.Attributes.Contains("aria-label");
                var hasAspFor = input.HasAttributes && input.Attributes.Contains("asp-for");
                var hasId = input.GetAttributeValue("id", "");
                var hasLabel = !string.IsNullOrEmpty(hasId) &&
                    doc.DocumentNode.SelectSingleNode($"//label[@for='{hasId}']") is not null;
                if (hasAriaLabel || hasAspFor || hasLabel)
                    hasLabeled = true;
            }
            Assert.True(hasLabeled, $"{file}: no labeled input found near '{identifier}'");
        }
    }
}
