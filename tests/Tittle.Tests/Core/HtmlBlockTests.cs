using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

public class HtmlBlockTests
{
    [Fact]
    public void HtmlTable_ConvertsToGfmTable()
    {
        var html = "<table>\n<tr><th>A</th><th>B</th></tr>\n<tr><td>1</td><td>2</td></tr>\n</table>";

        var result = MarkdownPreprocessor.Transform(html, null);

        Assert.Contains("|", result);        // GFM table pipes
        Assert.Contains("A", result);
        Assert.Contains("1", result);
        Assert.DoesNotContain("<table>", result); // raw HTML gone
    }

    [Fact]
    public void HtmlDiv_UnwrapsToContent()
    {
        var result = MarkdownPreprocessor.Transform("<div>Hello world</div>", null);

        Assert.Contains("Hello world", result);
        Assert.DoesNotContain("<div>", result);
    }

    [Fact]
    public void FencedHtmlBlock_LeftAsSource()
    {
        var result = MarkdownPreprocessor.Transform("```\n<table><tr><td>x</td></tr></table>\n```", null);

        Assert.Contains("<table>", result); // inside a code fence — not converted
    }

    [Fact]
    public void MarkdownWithoutHtmlBlocks_Untouched()
    {
        const string md = "# Heading\n\nA paragraph with no HTML at all.";
        Assert.Equal(md, MarkdownPreprocessor.Transform(md, null));
    }
}
