using Tittle.Core.Settings;
using Xunit;

namespace Tittle.Tests.Core;

public class ReadingTemplatesTests
{
    [Fact]
    public void Reading_IsSerifNarrowRelaxedLarger()
    {
        var p = ReadingTemplates.For(ReadingTemplate.Reading);
        Assert.Equal(ReadingFont.Serif, p.Font);
        Assert.Equal(ReadingWidth.Narrow, p.Width);
        Assert.Equal(ReadingDensity.Relaxed, p.Density);
        Assert.Equal(17, p.FontSize);
    }

    [Fact]
    public void Developer_IsMonoFullWidth()
    {
        var p = ReadingTemplates.For(ReadingTemplate.Developer);
        Assert.Equal(ReadingFont.Mono, p.Font);
        Assert.Equal(ReadingWidth.Full, p.Width);
        Assert.Equal(14, p.FontSize);
    }

    [Fact]
    public void Compact_IsDense()
    {
        var p = ReadingTemplates.For(ReadingTemplate.Compact);
        Assert.Equal(ReadingDensity.Compact, p.Density);
        Assert.Equal(13, p.FontSize);
    }
}
