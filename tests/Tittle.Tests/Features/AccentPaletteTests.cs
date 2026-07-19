using Avalonia.Media;
using Tittle.Shared;
using Xunit;

namespace Tittle.Tests.Features;

public class AccentPaletteTests
{
    [Fact]
    public void TryParse_EmptyOrInvalid_ReturnsNull()
    {
        Assert.Null(AccentPalette.TryParse(""));
        Assert.Null(AccentPalette.TryParse("   "));
        Assert.Null(AccentPalette.TryParse(null));
        Assert.Null(AccentPalette.TryParse("not-a-color"));
    }

    [Fact]
    public void TryParse_Hex_ReturnsColor()
    {
        var c = AccentPalette.TryParse("#3B82F6");
        Assert.NotNull(c);
        Assert.Equal((byte)0x3B, c!.Value.R);
        Assert.Equal((byte)0x82, c.Value.G);
        Assert.Equal((byte)0xF6, c.Value.B);
    }

    [Fact]
    public void Lighten_BlendsTowardWhite()
    {
        var lighter = AccentPalette.Lighten(Color.FromRgb(0, 0, 0), 0.5);
        Assert.Equal((byte)128, lighter.R); // 0 + 255*0.5 = 127.5 → 128
        Assert.Equal((byte)255, AccentPalette.Lighten(Color.FromRgb(10, 10, 10), 1.0).R); // full → white
    }
}
