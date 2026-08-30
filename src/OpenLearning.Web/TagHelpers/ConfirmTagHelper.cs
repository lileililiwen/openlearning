using Microsoft.AspNetCore.Razor.TagHelpers;

namespace OpenLearning.Web.TagHelpers;

/// <summary>
/// Adds an explicit confirmation prompt before a destructive action.
/// <para>
/// Usage: <c>&lt;form confirm="Delete this course?"&gt;…&lt;/form&gt;</c> or
/// <c>&lt;a confirm="Leave?" href="/x"&gt;…&lt;/a&gt;</c>. The prompt text is moved to a
/// <c>data-confirm</c> attribute which <c>site-chrome.js</c> intercepts via <c>window.confirm</c>
/// (a styled modal can replace it later without touching markup). Links have their original
/// destination preserved in <c>data-confirm-href</c> so navigation still works after confirmation.
/// </para>
/// </summary>
[HtmlTargetElement("form", Attributes = _confirmAttributeName)]
[HtmlTargetElement("a", Attributes = _confirmAttributeName)]
[HtmlTargetElement("button", Attributes = _confirmAttributeName)]
public class ConfirmTagHelper : TagHelper
{
    private const string _confirmAttributeName = "confirm";

    [HtmlAttributeName(_confirmAttributeName)]
    public string? Confirm { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(Confirm))
        {
            return;
        }

        output.Attributes.RemoveAll(_confirmAttributeName);
        output.Attributes.SetAttribute("data-confirm", Confirm);

        if (string.Equals(output.TagName, "a", StringComparison.OrdinalIgnoreCase))
        {
            var originalHref = output.Attributes["href"]?.Value?.ToString();
            output.Attributes.SetAttribute("role", "button");
            if (!string.IsNullOrEmpty(originalHref))
            {
                output.Attributes.SetAttribute("data-confirm-href", originalHref);
                output.Attributes.SetAttribute("href", "#");
            }
        }
        else
        {
            output.Attributes.SetAttribute("aria-label", Confirm);
        }
    }
}
