using System;
using System.Diagnostics;
using System.Linq;
using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

/// <summary>
/// Regression tripwires for the document-wide scan budgets in MarkdownPreprocessor's block walks
/// (math / diagram / chart). Without a budget, n consecutive UNCLOSED openers make each subsequent
/// opener rescan from its own line to EOF (~n²/2 anchored regex evaluations) inside the synchronous,
/// cached PreviewMarkdown getter — a crafted ~20 KB file froze the UI for seconds.
/// </summary>
public class BlockScanBudgetTests
{
    [Fact]
    public void ManyUnclosedBracketMathOpeners_CompleteFast_LeftVerbatim()
    {
        var src = string.Join("\n", Enumerable.Repeat("\\[", 5000).Concat(new[] { "trailing prose" }));

        string? result = null;
        // A generous CI-safe ceiling — a tripwire against accidental budget removal, NOT a benchmark.
        var sw = Stopwatch.StartNew();
        result = MarkdownPreprocessor.Transform(src);
        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"Transform took {sw.Elapsed}");

        Assert.Equal(src, result); // every opener unclosed → emitted as authored, no bogus containers
    }

    [Fact]
    public void ManyBareFenceOpeners_LeftVerbatim_WithDiagramsOnAndOff()
    {
        var src = string.Join("\n", Enumerable.Repeat("```", 5000));

        // Adjacent bare fences pair up into empty blocks that every walk re-emits verbatim
        // (and the language autodetect never guesses on an empty body).
        Assert.Equal(src, MarkdownPreprocessor.Transform(src, null, diagramsEnabled: false));
        Assert.Equal(src, MarkdownPreprocessor.Transform(src, null, diagramsEnabled: true));
    }

    [Fact]
    public void ManyUnclosedChartOpeners_CompleteFast_LeftVerbatim()
    {
        // Every line carries an info string, so nothing ever closes → each opener scans to EOF:
        // the pathological n² shape the chart-walk budget caps at O(n).
        var src = string.Join("\n", Enumerable.Repeat("```chart", 5000));

        string? result = null;
        var sw = Stopwatch.StartNew();
        result = MarkdownPreprocessor.Transform(src);
        sw.Stop();

        Assert.Equal(src, result);
    }

    [Fact]
    public void WellFormedBlocks_StillConvert_AsBefore()
    {
        // Expectations copied from the existing green tests (MarkdownPreprocessorTests /
        // ChartFenceTests): the budgets must never engage for well-formed documents.
        var math = MarkdownPreprocessor.Transform("text\n$$\nE = mc^2\n$$\nafter");
        Assert.Contains("::: math\n" + Uri.EscapeDataString("E = mc^2") + "\n:::", math);
        Assert.DoesNotContain("$$", math);

        var diagram = MarkdownPreprocessor.Transform(
            "text\n\n```mermaid\ngraph TD;A-->B\n```\n\nmore", null, diagramsEnabled: true);
        Assert.Contains("::: diagram", diagram);
        Assert.DoesNotContain("```mermaid", diagram);
        Assert.Contains("mermaid|" + Uri.EscapeDataString("graph TD;A-->B"), diagram);

        var chart = MarkdownPreprocessor.Transform("```chart:line\nX,Y\na,1\n```", null);
        Assert.Contains("::: chart", chart);
        Assert.Contains(Uri.EscapeDataString("line") + "|", chart);
    }
}
