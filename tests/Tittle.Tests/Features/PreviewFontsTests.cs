using Tittle.Core.Settings;
using Tittle.Shared;
using Xunit;

namespace Tittle.Tests.Features;

public class PreviewFontsTests
{
    [Fact]
    public void Resolve_MapsEachPreset_AndConstructsWithoutThrowing()
    {
        // Touching the static families also exercises `new FontFamily("fonts:Inter#Inter")` (the bundled
        // collection reference) — a bad URI would throw here rather than at first render.
        Assert.Same(PreviewFonts.Sans, PreviewFonts.Resolve(ReadingFont.Sans));
        Assert.Same(PreviewFonts.Serif, PreviewFonts.Resolve(ReadingFont.Serif));
        Assert.Same(PreviewFonts.Mono, PreviewFonts.Resolve(ReadingFont.Mono));
    }

    [Fact]
    public void LayoutOptions_RoundTripsFontFamily()
    {
        var opts = new LayoutOptions { FontFamily = ReadingFont.Mono };
        var restored = LayoutOptions.FromSettings(opts.ToSettings());
        Assert.Equal(ReadingFont.Mono, restored.FontFamily);
    }
}
