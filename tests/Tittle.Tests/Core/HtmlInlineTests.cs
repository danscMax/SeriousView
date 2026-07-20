using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

public class HtmlInlineTests
{
    [Fact]
    public void BoldItalicCode_ConvertToMarkdown()
    {
        Assert.Contains("**x**", MarkdownPreprocessor.Transform("<b>x</b>", null));
        Assert.Contains("**y**", MarkdownPreprocessor.Transform("<strong>y</strong>", null));
        Assert.Contains("*i*", MarkdownPreprocessor.Transform("<i>i</i>", null));
        Assert.Contains("*e*", MarkdownPreprocessor.Transform("<em>e</em>", null));
        Assert.Contains("`c`", MarkdownPreprocessor.Transform("<code>c</code>", null));
        Assert.Contains("`k`", MarkdownPreprocessor.Transform("<kbd>k</kbd>", null));
    }

    [Fact]
    public void AnchorConvertsToMarkdownLink()
    {
        Assert.Contains("[text](https://a.com)",
            MarkdownPreprocessor.Transform("Смотри <a href=\"https://a.com\">text</a> тут", null));
    }

    [Fact]
    public void FencedHtml_LeftAsSource()
    {
        var result = MarkdownPreprocessor.Transform("```\n<b>x</b>\n```", null);
        Assert.Contains("<b>x</b>", result);   // inside a code fence — not converted
        Assert.DoesNotContain("**x**", result);
    }

    [Fact]
    public void InlineCodeHtml_LeftAsSource()
    {
        Assert.Contains("`<b>x</b>`", MarkdownPreprocessor.Transform("`<b>x</b>`", null));
    }

    [Fact]
    public void PlainMarkdown_Untouched()
    {
        Assert.Equal("just text, no tags", MarkdownPreprocessor.Transform("just text, no tags", null));
    }
}
