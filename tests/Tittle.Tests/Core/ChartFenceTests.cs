using System;
using System.Collections.Generic;
using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

public class ChartFenceTests
{
    [Fact]
    public void ChartFenceWithType_BecomesChartContainer_TypeAndBodyEncoded()
    {
        var result = MarkdownPreprocessor.Transform("```chart:line\nX,Y\na,1\n```", null);

        Assert.Contains("::: chart", result);
        Assert.Contains(Uri.EscapeDataString("line") + "|", result); // typeHint|body transport
    }

    [Fact]
    public void PlainChartFence_BecomesContainer_EmptyType()
    {
        var result = MarkdownPreprocessor.Transform("```chart\n{\"data\":{\"datasets\":[{\"data\":[1]}]}}\n```", null);

        Assert.Contains("::: chart", result);
    }

    [Fact]
    public void NonChartFence_Untouched()
    {
        var result = MarkdownPreprocessor.Transform("```python\nprint(1)\n```", null);

        Assert.DoesNotContain("::: chart", result);
        Assert.Contains("print(1)", result);
    }

    [Fact]
    public void ChartBody_WithLeadingUnderscoreToken_SurvivesTheEmphasisPass()
    {
        // A leading _id_ CSV header would be rewritten _id_ → *id* by ConvertUnderscoreEmphasisInPlace
        // unless the ::: chart container body is protected from the inline passes. Guard against silent
        // chart-data corruption (regression for the blind-review MAJOR finding).
        var result = MarkdownPreprocessor.Transform("```chart\n_id_,Q1\n_id_,5\n```", null);

        Assert.Contains("_id_", result);       // underscores preserved in the encoded body
        Assert.DoesNotContain("*id*", result); // NOT mangled to emphasis
    }

    [Fact]
    public void CharterFence_NotMistakenForChart()
    {
        // "charter" starts with "chart" but is a different language — the exact chart/chart: test excludes it.
        var result = MarkdownPreprocessor.Transform("```charter\nfoo\n```", null);

        Assert.DoesNotContain("::: chart", result);
    }

    // A chart-free document must pay ZERO chart-walk rebuild work: the fast bail returns the SAME
    // list reference (test seam — InternalsVisibleTo). Prose and fenced examples containing the word
    // "chart" must NOT enable the pass (opener matching + exact info test, not a substring search).
    [Fact]
    public void ChartFreeDocument_BailReturnsSameReference()
    {
        var lines = new List<string>
        {
            "# Report",
            "",
            "The chart below shows growth; see charting notes.",
            "",
            "```python",
            "def plot_chart():",
            "    print(\"chart data\")",
            "```",
            "",
            "```charter",
            "not a chart fence",
            "```",
        };

        Assert.True(ReferenceEquals(lines, MarkdownPreprocessor.ConvertChartFences(lines)));
    }

    [Fact]
    public void DocumentWithRealChartFence_DoesNotBail()
    {
        var lines = new List<string> { "Intro", "", "```chart", "{\"type\":\"bar\"}", "```", "", "Outro" };

        Assert.False(ReferenceEquals(lines, MarkdownPreprocessor.ConvertChartFences(lines)));
        Assert.Contains("::: chart", MarkdownPreprocessor.ConvertChartFences(lines));
    }

    [Fact]
    public void LargeChartFreeDocument_RoundTripsByteIdentical()
    {
        // Fences, code blocks, the PROSE word "chart" and a ```python example mentioning chart —
        // but no real ```chart fence. Every preprocessor pass must be a no-op here, so the exact
        // expected output IS the input (pinned byte-for-byte).
        const string section =
            "## Section heading\n"
            + "\n"
            + "Plain prose mentioning the chart word and charting in general.\n"
            + "\n"
            + "```python\n"
            + "def plot_chart():\n"
            + "    print(\"chart data\")\n"
            + "```\n"
            + "\n"
            + "```js\n"
            + "const chart = {data: [1, 2, 3]};\n"
            + "```\n"
            + "\n";
        var builder = new System.Text.StringBuilder("# Quarterly report\n\n");
        for (var i = 0; i < 50; i++)
            builder.Append(section);
        builder.Append("Final paragraph mentioning chart one more time.");
        var md = builder.ToString();

        var result = MarkdownPreprocessor.Transform(md, null);

        Assert.Equal(md, result);
    }

    [Fact]
    public void DocumentWithChartFence_StillConvertsAfterBail()
    {
        // The bail must not fire on a document that really has a ```chart fence.
        var result = MarkdownPreprocessor.Transform(
            "Intro\n\n```chart\n{\"type\":\"bar\",\"data\":[1,2]}\n```\n\nOutro\n", null);

        Assert.Contains("::: chart", result);
        Assert.DoesNotContain("```chart", result);
    }

    [Fact]
    public void ChartFenceInsideOuterFence_Untouched()
    {
        // A ```chart shown INSIDE an outer fence is an example, not a chart — copied verbatim.
        var md = "```markdown\nBefore\n```chart\nA,B\n```\nAfter\n```";

        var result = MarkdownPreprocessor.Transform(md, null);

        Assert.Equal(md, result);
    }
}
