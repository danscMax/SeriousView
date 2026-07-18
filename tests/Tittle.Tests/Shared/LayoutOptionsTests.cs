using Tittle.Core.Settings;
using Tittle.Shared;
using Xunit;

namespace Tittle.Tests.Shared;

public class LayoutOptionsTests
{
    [Fact]
    public void FromSettings_Null_GivesEtalonDefaults()
    {
        var o = LayoutOptions.FromSettings(null);

        Assert.Equal(MenuPlacement.Hidden, o.MenuPlacement);       // menu behind ☰
        Assert.Equal(ToolbarMode.Contextual, o.ToolbarMode);
        Assert.True(o.ShowOmnibar);
        Assert.True(o.IsWorkspaceSidebarOpen);
        Assert.Equal(WorkspaceSection.Files, o.WorkspaceSection);
        Assert.Equal(240, o.OutlineWidth); // etalon outline sidebar width
        Assert.Equal(ReadingWidth.Comfort, o.ReadingWidth); // comfortable centered column by default
        Assert.Equal(SplitOrientation.Horizontal, o.SplitOrientation); // side-by-side by default
        Assert.Equal(0.5, o.SplitRatio); // even split by default
    }

    [Fact]
    public void FromSettings_RoundTripsThroughToSettings()
    {
        var s = new LayoutSettings
        {
            MenuPlacement = MenuPlacement.Bar,
            ToolbarMode = ToolbarMode.Fixed,
            ShowOmnibar = false,
            IsWorkspaceSidebarOpen = false,
            WorkspaceSection = WorkspaceSection.Bookmarks,
            OutlineWidth = 320,
            ReadingWidth = ReadingWidth.Narrow,
            ReadingDensity = ReadingDensity.Relaxed, // preset set, but the CUSTOM numbers below must survive
            LineSpacing = 22,                        // (not clobbered by the Relaxed preset's 16 on load)
            ParagraphSpacing = 33,
            HeadingScale = 1.35,
            TextAlignment = TextAlign.Center,
            SplitOrientation = SplitOrientation.Vertical,
            SplitRatio = 0.7,
        };

        Assert.Equal(s, LayoutOptions.FromSettings(s).ToSettings());
    }

    [Fact]
    public void PickingADensityPreset_WritesTheConcreteSpacingNumbers()
    {
        var o = new LayoutOptions();

        o.ReadingDensity = ReadingDensity.Relaxed;
        Assert.Equal(16, o.LineSpacing);
        Assert.Equal(24, o.ParagraphSpacing);

        o.ReadingDensity = ReadingDensity.Compact;
        Assert.Equal(5, o.LineSpacing);
        Assert.Equal(8, o.ParagraphSpacing);
    }

    [Theory]
    [InlineData(-5, 0)]      // below min → clamped
    [InlineData(100, 40)]    // above max → clamped
    [InlineData(12, 12)]     // in range → unchanged
    public void LineSpacing_IsClampedToRange(double set, double expected)
    {
        Assert.Equal(expected, new LayoutOptions { LineSpacing = set }.LineSpacing);
    }

    [Theory]
    [InlineData(0.1, 0.7)]   // below min → clamped
    [InlineData(9, 1.8)]     // above max → clamped
    [InlineData(1.25, 1.25)] // in range → unchanged
    public void HeadingScale_IsClampedToRange(double set, double expected)
    {
        Assert.Equal(expected, new LayoutOptions { HeadingScale = set }.HeadingScale);
    }

    [Fact]
    public void FromSettings_InvalidWorkspaceSection_FallsBackToFiles()
    {
        var settings = new LayoutSettings { WorkspaceSection = (WorkspaceSection)999 };

        Assert.Equal(WorkspaceSection.Files, LayoutOptions.FromSettings(settings).WorkspaceSection);
    }

    [Fact]
    public void ReadingWidthConverter_MapsPresetsToColumnAndAlignment()
    {
        var c = ReadingWidthConverter.Instance;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        Assert.Equal(double.PositiveInfinity, c.Convert(ReadingWidth.Full, typeof(double), null, culture));
        Assert.Equal(760d, c.Convert(ReadingWidth.Comfort, typeof(double), null, culture));
        Assert.Equal(620d, c.Convert(ReadingWidth.Narrow, typeof(double), null, culture));
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Stretch,
            c.Convert(ReadingWidth.Full, typeof(object), "align", culture));
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Center,
            c.Convert(ReadingWidth.Narrow, typeof(object), "align", culture));
    }

    [Theory]
    [InlineData(100, 180)]   // below min → clamped up
    [InlineData(500, 480)]   // above max → clamped down
    [InlineData(300, 300)]   // in range → unchanged
    public void OutlineWidth_IsClampedToRange(double set, double expected)
    {
        var o = new LayoutOptions { OutlineWidth = set };

        Assert.Equal(expected, o.OutlineWidth);
    }

    [Theory]
    [InlineData(0.05, 0.15)]  // below min → clamped up
    [InlineData(0.95, 0.85)]  // above max → clamped down
    [InlineData(0.5, 0.5)]    // in range → unchanged
    public void SplitRatio_IsClampedToRange(double set, double expected)
    {
        var o = new LayoutOptions { SplitRatio = set };

        Assert.Equal(expected, o.SplitRatio);
    }
}
