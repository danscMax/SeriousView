using System.Collections.Generic;
using Tittle.Core.Settings;

namespace Tittle.Features.Settings;

/// <summary>One option in a settings dropdown: the enum value that gets stored, and the words the user
/// reads. Keeping the label beside the value (instead of a converter per enum) means the settings window
/// binds a ComboBox straight to the LayoutOptions property via SelectedValue.</summary>
/// <param name="Value">The enum value written to <see cref="Tittle.Shared.LayoutOptions"/>.</param>
/// <param name="Label">The user-facing wording.</param>
public sealed record SettingChoice(object Value, string Label);

/// <summary>The option lists behind the settings dropdowns. Ordered the way the user thinks about them
/// (least → most), NOT in enum-declaration order, so a dropdown reads as a scale.</summary>
public static class SettingChoices
{
    // Labels stay short enough to fit the dropdown: the row's description carries the explanation, so a
    // label never has to. ("Контекстная (в режиме «Исходник»)" truncated inside the control.)
    public static IReadOnlyList<SettingChoice> ToolbarModes { get; } =
    [
        new(ToolbarMode.Contextual, "Только в «Исходнике»"),
        new(ToolbarMode.Fixed, "Всегда показывать"),
        new(ToolbarMode.Off, "Выключена"),
    ];

    public static IReadOnlyList<SettingChoice> ReadingWidths { get; } =
    [
        new(ReadingWidth.Full, "Полная"),
        new(ReadingWidth.Comfort, "Средняя (760 px)"),
        new(ReadingWidth.Narrow, "Узкая (620 px)"),
    ];

    public static IReadOnlyList<SettingChoice> ReadingDensities { get; } =
    [
        new(ReadingDensity.Compact, "Плотно"),
        new(ReadingDensity.Normal, "Обычно"),
        new(ReadingDensity.Relaxed, "Просторно"),
    ];

    // Justify is deliberately NOT offered: ColorTextBlock.Avalonia's text engine mis-lays-out justified
    // WRAPPED lines (a non-last line collapses to a fragment). The enum value is kept for the day the
    // renderer supports it; Left + Center both render cleanly.
    public static IReadOnlyList<SettingChoice> TextAlignments { get; } =
    [
        new(TextAlign.Left, "По левому краю"),
        new(TextAlign.Center, "По центру"),
    ];

    public static IReadOnlyList<SettingChoice> SplitOrientations { get; } =
    [
        new(SplitOrientation.Horizontal, "Рядом"),
        new(SplitOrientation.Vertical, "Друг над другом"),
    ];

    public static IReadOnlyList<SettingChoice> ReadingFonts { get; } =
    [
        new(ReadingFont.Sans, "Без засечек (Inter)"),
        new(ReadingFont.Serif, "С засечками"),
        new(ReadingFont.Mono, "Моноширинный"),
    ];
}
