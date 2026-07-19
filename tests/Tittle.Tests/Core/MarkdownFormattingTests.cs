using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

/// <summary>The editor toolbar's markdown formatting: wrap/prefix rules + where the selection lands
/// after. Pure logic, so it's the oracle-clear part — tested exhaustively here rather than through the UI.</summary>
public class MarkdownFormattingTests
{
    private static string ApplyToString(string text, int start, int len, MarkdownFormatKind kind)
    {
        var e = MarkdownFormatting.Apply(text, start, len, kind);
        return text.Substring(0, e.Start) + e.NewText + text.Substring(e.Start + e.Length);
    }

    [Fact]
    public void Bold_WrapsSelection_AndSelectsTheInnerText()
    {
        var e = MarkdownFormatting.Apply("hello world", 0, 5, MarkdownFormatKind.Bold);
        Assert.Equal("**hello**", e.NewText);
        Assert.Equal(2, e.SelectionStart);   // inside the leading **
        Assert.Equal(5, e.SelectionLength);  // the original "hello" stays selected
    }

    [Fact]
    public void Bold_NoSelection_InsertsMarkersAroundAPlaceholder()
    {
        var e = MarkdownFormatting.Apply("", 0, 0, MarkdownFormatKind.Bold);
        Assert.Equal("**жирный**", e.NewText);
        Assert.Equal(2, e.SelectionStart);
        Assert.Equal("жирный".Length, e.SelectionLength); // placeholder selected → user types over it
    }

    [Theory]
    [InlineData(MarkdownFormatKind.Italic, "*sel*")]
    [InlineData(MarkdownFormatKind.Code, "`sel`")]
    public void Inline_WrapsWithItsMarker(MarkdownFormatKind kind, string expected)
    {
        Assert.Equal(expected, MarkdownFormatting.Apply("sel", 0, 3, kind).NewText);
    }

    [Fact]
    public void Link_WrapsSelection_AndSelectsTheUrlPlaceholder()
    {
        var text = "docs";
        var e = MarkdownFormatting.Apply(text, 0, 4, MarkdownFormatKind.Link);
        Assert.Equal("[docs](url)", e.NewText);
        Assert.Equal("url", e.NewText.Substring(e.SelectionStart - e.Start, e.SelectionLength));
    }

    [Fact]
    public void Heading_AddsHashesOnTheLine_AndDeepensWhenAlreadyAHeading()
    {
        Assert.Equal("# foo", ApplyToString("foo", 1, 0, MarkdownFormatKind.Heading));
        Assert.Equal("## foo", ApplyToString("# foo", 3, 0, MarkdownFormatKind.Heading));
        // second line of a multi-line doc gets its own heading, not the first line's
        Assert.Equal("a\n# b", ApplyToString("a\nb", 2, 0, MarkdownFormatKind.Heading));
    }

    [Fact]
    public void Quote_And_Bullet_PrefixEveryTouchedLine()
    {
        Assert.Equal("> a\n> b", ApplyToString("a\nb", 0, 3, MarkdownFormatKind.Quote));
        Assert.Equal("- a\n- b", ApplyToString("a\nb", 0, 3, MarkdownFormatKind.BulletList));
    }
}
