using Microsoft.AspNetCore.Razor.TagHelpers;
using OpenLearning.Web.TagHelpers;
using Xunit;

namespace OpenLearning.UnitTests.Web;

public sealed class ConfirmTagHelperTests
{
    private static (TagHelperContext Context, TagHelperOutput Output) NewContext(string tagName)
    {
        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "test");
        // The tag helper inspects output.TagName to decide anchor handling.
        var output = new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        return (context, output);
    }

    [Fact]
    public void Form_WithConfirm_MovesToDataConfirmAndAddsAriaLabel()
    {
        var (context, output) = NewContext("form");
        output.Attributes.SetAttribute("confirm", "Delete this course?");
        var helper = new ConfirmTagHelper { Confirm = "Delete this course?" };

        helper.Process(context, output);

        Assert.False(output.Attributes.ContainsName("confirm"));
        Assert.Equal("Delete this course?", output.Attributes["data-confirm"]?.Value);
        Assert.Equal("Delete this course?", output.Attributes["aria-label"]?.Value);
    }

    [Fact]
    public void Anchor_WithConfirm_PreservesHrefAndBecomesButton()
    {
        var (context, output) = NewContext("a");
        output.Attributes.SetAttribute("href", "/courses/1");
        output.Attributes.SetAttribute("confirm", "Leave?");
        var helper = new ConfirmTagHelper { Confirm = "Leave?" };

        helper.Process(context, output);

        Assert.False(output.Attributes.ContainsName("confirm"));
        Assert.Equal("Leave?", output.Attributes["data-confirm"]?.Value);
        Assert.Equal("button", output.Attributes["role"]?.Value);
        Assert.Equal("#", output.Attributes["href"]?.Value);
        Assert.Equal("/courses/1", output.Attributes["data-confirm-href"]?.Value);
    }

    [Fact]
    public void EmptyConfirm_IsNoOp()
    {
        var (context, output) = NewContext("form");
        output.Attributes.SetAttribute("confirm", string.Empty);
        var helper = new ConfirmTagHelper { Confirm = string.Empty };

        helper.Process(context, output);

        Assert.True(output.Attributes.ContainsName("confirm"));
    }
}
