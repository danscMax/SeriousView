using System;
using System.Linq;
using Tittle.Core.Settings;
using Tittle.Features.Settings;
using Xunit;

namespace Tittle.Tests.Features;

public class SettingsViewTests
{
    // Every option the user can pick in the settings page must be reachable: a dropdown's list has to cover
    // the whole enum, or a setting becomes silently unselectable (the drift the theme/encoding lists hit
    // before). The combos' runtime routing (nested DataContext="{Binding Layout}") is verified by the
    // Layer-1 render (tools/HeadlessRender "settings") — headless TabControl realizes tabs lazily, so the
    // logical tree isn't a reliable oracle for the unselected pages.
    [Fact]
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
