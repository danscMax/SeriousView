using System;
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
}
