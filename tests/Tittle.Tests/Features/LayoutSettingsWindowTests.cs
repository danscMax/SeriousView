using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Tittle.Core.Settings;
using Tittle.Features.Settings;
using Tittle.Shared;
using Xunit;

namespace Tittle.Tests.Features;

public class LayoutSettingsWindowTests
{
    // Exercises the real window end-to-end: each dropdown reflects the current value from the shared
    // LayoutOptions, and picking another option writes it straight back (ComboBox SelectedValue +
    // SelectedValueBinding over SettingChoices — no per-enum converter).
    // Read from the logical tree (no Show/render) — rendering trips FluentAvalonia's Symbols glyph font
    // under the headless font manager; bindings are live from InitializeComponent.
    [AvaloniaFact]
    public void SettingsDropdowns_ReflectAndSet_TheSharedLayout()
    {
        var layout = new LayoutOptions { ToolbarMode = ToolbarMode.Contextual };
        var window = new LayoutSettingsWindow { DataContext = layout };

        ComboBox Pick(string automationName) => window.GetLogicalDescendants()
            .OfType<ComboBox>()
            .Single(c => AutomationProperties.GetName(c) == automationName);

        // Every choice in the window is a dropdown; each starts on the layout's current value.
        Assert.Equal(ToolbarMode.Contextual, Pick("Панель инструментов").SelectedValue);
        Assert.Equal(ReadingWidth.Comfort, Pick("Ширина колонки чтения").SelectedValue);
        Assert.Equal(ReadingDensity.Normal, Pick("Разрежённость текста").SelectedValue);
        Assert.Equal(SplitOrientation.Horizontal, Pick("Ориентация разделённого вида").SelectedValue);

        // Picking another option writes it back through the two-way SelectedValue seam.
        Pick("Панель инструментов").SelectedValue = ToolbarMode.Off;
        Assert.Equal(ToolbarMode.Off, layout.ToolbarMode);

        Pick("Ширина колонки чтения").SelectedValue = ReadingWidth.Narrow;
        Assert.Equal(ReadingWidth.Narrow, layout.ReadingWidth);

        Pick("Разрежённость текста").SelectedValue = ReadingDensity.Relaxed;
        Assert.Equal(ReadingDensity.Relaxed, layout.ReadingDensity);

        Pick("Ориентация разделённого вида").SelectedValue = SplitOrientation.Vertical;
        Assert.Equal(SplitOrientation.Vertical, layout.SplitOrientation);
    }

    // Every option the user can pick must be reachable: a dropdown's list has to cover the whole enum,
    // or a setting becomes silently unselectable (the kind of drift the theme/encoding lists hit before).
    [AvaloniaFact]
    public void EveryChoiceList_CoversItsWholeEnum()
    {
        Assert.Equal(
            Enum.GetValues<ToolbarMode>().Cast<object>().ToHashSet(),
            SettingChoices.ToolbarModes.Select(c => c.Value).ToHashSet());
        Assert.Equal(
            Enum.GetValues<ReadingWidth>().Cast<object>().ToHashSet(),
            SettingChoices.ReadingWidths.Select(c => c.Value).ToHashSet());
        Assert.Equal(
            Enum.GetValues<ReadingDensity>().Cast<object>().ToHashSet(),
            SettingChoices.ReadingDensities.Select(c => c.Value).ToHashSet());
        Assert.Equal(
            Enum.GetValues<SplitOrientation>().Cast<object>().ToHashSet(),
            SettingChoices.SplitOrientations.Select(c => c.Value).ToHashSet());

        Assert.All(
            SettingChoices.ToolbarModes
                .Concat(SettingChoices.ReadingWidths)
                .Concat(SettingChoices.ReadingDensities)
                .Concat(SettingChoices.SplitOrientations),
            choice => Assert.False(string.IsNullOrWhiteSpace(choice.Label)));
    }
}
