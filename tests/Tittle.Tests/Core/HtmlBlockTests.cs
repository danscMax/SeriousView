using System.Linq;
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
    public void UnterminatedTable_ThenFenceWithStrayCloseTag_DoesNotSwallowTheFence()
    {
        // An unterminated <table> must not be "closed" by a </table> that appears inside a later fenced
        // code block — that would sweep the prose and the whole fence into one HTML blob.
        var src = "<table>\n<tr><td>x</td></tr>\n\ntext\n\n```\n</table>\n```\n";

        var result = MarkdownPreprocessor.Transform(src, null);

        Assert.Contains("```", result);      // the fence survives intact
        Assert.Contains("<table>", result);  // the unterminated opener stays as source, not converted
    }

    [Fact]
    public void TableWithChildTagStartingWithTable_StillConverts()
    {
        // A child element whose name starts with "table" (e.g. a web-component) must not inflate the open
        // count and make a valid table look unterminated (→ dropped by Markdown.Avalonia).
        var src = "<table><tr><td><tablecell>x</tablecell></td></tr></table>";

        var result = MarkdownPreprocessor.Transform(src, null);

        Assert.DoesNotContain("<table>", result); // converted, not left as raw (dropped) HTML
        Assert.Contains("x", result);
    }

    [Fact]
    public void ManyUnclosedTableOpeners_BailFast_LeftAsSource()
    {
        // Pathological input the synchronous preview getter must survive: thousands of bare <table> openers
        // with no close. The fast-bail (no </table> anywhere) returns without the O(n²) forward scan.
        var src = string.Join("\n", Enumerable.Repeat("<table>", 5000));

        var result = MarkdownPreprocessor.Transform(src, null);

        Assert.Contains("<table>", result); // left as source, no hang
    }

    [Fact]
    public void ManyOpenersThenOneStrayClose_TerminatesViaScanBudget()
    {
        // Many openers plus a single stray close (unbalanced) — the scan budget bounds total work to O(n)
        // so this returns instead of freezing; the openers degrade gracefully to source.
        var src = string.Join("\n", Enumerable.Repeat("<table>", 6000)) + "\n</table>";

        var result = MarkdownPreprocessor.Transform(src, null);

        Assert.Contains("<table>", result);
    }

    [Fact]
    public void MarkdownWithoutHtmlBlocks_Untouched()
    {
        const string md = "# Heading\n\nA paragraph with no HTML at all.";
        Assert.Equal(md, MarkdownPreprocessor.Transform(md, null));
    }
}
