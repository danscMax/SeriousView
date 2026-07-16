using Avalonia;
using Avalonia.Controls;

namespace Tittle.Features.Settings;

/// <summary>
/// One settings line: <c>Label</c> + optional <c>Description</c> on the left, the control (this
/// control's <c>Content</c>) on the right. The shared primitive the settings window is built from, so
/// the label/description/control rhythm lives in ONE template instead of a hand-rolled Grid per row
/// (the shape the old window's copy-pasted groups had drifted into).
/// Modelled on SweetWhisper's SettingRow, which solved the same duplication.
/// The template is in <c>Themes/Settings.axaml</c>; it uses the same tokens as the rest of the chrome.
/// </summary>
public class SettingRow : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SettingRow, string?>(nameof(Label));

    /// <summary>Secondary line under the label: what the setting actually does, in the user's words.
    /// Null/empty hides it, so a self-evident row stays a single line.</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingRow, string?>(nameof(Description));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
