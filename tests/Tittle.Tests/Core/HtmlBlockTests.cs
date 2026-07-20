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
    public void TableWithInternalBlankLines_StillConvertsAsOneTable()
    {
        // Hand-formatted HTML often puts blank lines between rows — the span is collected by tag balance,
        // so a cosmetic blank line inside the table must not split it (regression guard for the old
        // "collect until the first blank line" logic).
        var html = "<table>\n<tr><td>A</td></tr>\n\n<tr><td>B</td></tr>\n</table>";

        var result = MarkdownPreprocessor.Transform(html, null);

        Assert.Contains("|", result);
        Assert.Contains("A", result);
        Assert.Contains("B", result);
        Assert.DoesNotContain("<table>", result);
    }

    [Fact]
    public void MarkdownAfterTableClose_IsNotSweptIntoTheTable()
    {
        // Prose right after </table> must keep its markdown — the old pass swept every following non-blank
        // line into the HTML chunk, which escaped *emphasis* into literal \*text\*.
        var src = "<table><tr><td>1</td></tr></table>\nThis is *kept* text.";

        var result = MarkdownPreprocessor.Transform(src, null);

        Assert.Contains("*kept*", result);
        Assert.DoesNotContain(@"\*kept", result);
    }

    [Fact]
    public void DivWrappingMarkdown_IsLeftUntouched_NoCorruption()
    {
        // A <div> wrapping markdown is deliberately NOT converted: doing so would escape the markdown
        // (*important* → \*important\*). The markdown must survive verbatim.
        var src = "<div class=\"note\">\n*important*\n</div>";

        var result = MarkdownPreprocessor.Transform(src, null);

        Assert.Contains("*important*", result);
        Assert.DoesNotContain(@"\*important", result);
    }

    [Fact]
    public void FencedHtmlTable_LeftAsSource()
    {
        var result = MarkdownPreprocessor.Transform("```\n<table><tr><td>x</td></tr></table>\n```", null);

        Assert.Contains("<table>", result); // inside a code fence — not converted
    }

    [Fact]
    public void UnterminatedTable_LeftAsSource_NoCrash()
    {
        var result = MarkdownPreprocessor.Transform("<table>\n<tr><td>x</td></tr>", null);

        Assert.Contains("<table>", result); // no closing tag → not a real block, source preserved
    }

    [Fact]
    public void MarkdownWithoutHtmlBlocks_Untouched()
    {
        const string md = "# Heading\n\nA paragraph with no HTML at all.";
        Assert.Equal(md, MarkdownPreprocessor.Transform(md, null));
    }
}
