using Avalonia.Media;
using Tittle.Core.Settings;

namespace Tittle.Shared;

/// <summary>Maps the Core <see cref="ReadingFont"/> preset to a real Avalonia <see cref="FontFamily"/>
/// for the markdown preview body. Sans is the bundled static Inter (the variable-font bold fix, kept as
/// the explicit family so inline italic resolves); serif and mono use system stacks with fallbacks.
/// The chosen family is published as the app resource <c>PreviewFontFamily</c>, which the preview's
/// <c>CTextBlock</c> style consumes via <c>DynamicResource</c>.</summary>
public static class PreviewFonts
{
    /// <summary>App resource key the preview body font is published under.</summary>
    public const string ResourceKey = "PreviewFontFamily";

    // Reference the bundled Inter collection exactly as the XAML does (fonts:Inter#Inter).
    public static readonly FontFamily Sans = new("fonts:Inter#Inter");
    public static readonly FontFamily Serif = new("Georgia, Cambria, Times New Roman, serif");
    public static readonly FontFamily Mono = new("Cascadia Code, Consolas, Courier New, monospace");

    public static FontFamily Resolve(ReadingFont font) => font switch
    {
        ReadingFont.Serif => Serif,
        ReadingFont.Mono => Mono,
        _ => Sans,
    };
}
