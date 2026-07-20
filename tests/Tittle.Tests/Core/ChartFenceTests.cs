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
    public void CharterFence_NotMistakenForChart()
    {
        // "charter" starts with "chart" but is a different language — the exact chart/chart: test excludes it.
        var result = MarkdownPreprocessor.Transform("```charter\nfoo\n```", null);

        Assert.DoesNotContain("::: chart", result);
    }
}
