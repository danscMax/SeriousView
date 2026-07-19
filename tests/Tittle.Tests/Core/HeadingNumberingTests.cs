using System.Collections.Generic;
using System.Linq;
using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

public class HeadingNumberingTests
{
    private static List<string> Number(params string[] lines)
    {
        var list = new List<string>(lines);
        HeadingNumbering.Apply(list, _ => false); // no fences
        return list;
    }

    [Fact]
    public void NumbersHierarchically()
    {
        var result = Number("# A", "## B", "## C", "### D", "# E");

        Assert.Equal(new[] { "# 1 A", "## 1.1 B", "## 1.2 C", "### 1.2.1 D", "# 2 E" }, result);
    }

    [Fact]
    public void TrimsLeadingZeroLevels_WhenDocStartsBelowH1()
    {
        var result = Number("## First", "### Sub", "## Second");

        Assert.Equal(new[] { "## 1 First", "### 1.1 Sub", "## 2 Second" }, result);
    }

    [Fact]
    public void SkipsFencedLines()
    {
        var lines = new List<string> { "# Real", "```", "# not a heading", "```" };
        // Mark the three fence lines (index 1..3) as fenced; only line 0 is a real heading.
        HeadingNumbering.Apply(lines, i => i >= 1);

        Assert.Equal("# 1 Real", lines[0]);
        Assert.Equal("# not a heading", lines[2]); // untouched inside the fence
    }

    [Fact]
    public void NumbersIndentedHeadings_0to3LeadingSpaces()
    {
        // CommonMark allows up to 3 leading spaces; such a heading still renders + appears in the TOC,
        // so numbering must not skip it (else the numbers desync from the visible structure).
        var result = Number("# A", "  ## Indented", "### Deep");

        Assert.Equal(new[] { "# 1 A", "## 1.1 Indented", "### 1.1.1 Deep" }, result);
    }

    [Fact]
    public void IgnoresNonHeadings()
    {
        var result = Number("#nospace", "text", "#######7hashes");

        Assert.Equal(new[] { "#nospace", "text", "#######7hashes" }, result); // unchanged
    }

    [Fact]
    public void RunsThroughTransform_OnlyWhenEnabled()
    {
        Assert.Contains("# 1 Title", MarkdownPreprocessor.Transform("# Title", null, numberHeadings: true));
        Assert.DoesNotContain("# 1 Title", MarkdownPreprocessor.Transform("# Title", null, numberHeadings: false));
    }
}
