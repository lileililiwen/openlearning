using Markdig;

namespace OpenLearning.Web;

public static class MarkdownRenderer
{
    public static string ToHtml(string? markdown)
    {
        return Markdown.ToHtml(markdown ?? string.Empty);
    }
}
