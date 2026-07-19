namespace Tittle.Core.Settings;

/// <summary>One-click reading presets ported from the original viewer's ШАБЛОНЫ (Reading / Developer /
/// Compact / Printable). Each bundles a font family, reading-column width, text density and preview
/// font size — the source's fontFamily / contentWidth / lineHeight / fontSize, mapped onto Tittle's
/// existing knobs. Applying one is reversible via the individual settings.</summary>
public enum ReadingTemplate
{
    /// <summary>Comfortable long-form reading: serif, narrow column, roomy spacing, larger text.</summary>
    Reading,

    /// <summary>Code-focused: monospace, full width, normal spacing, compact text.</summary>
    Developer,

    /// <summary>Dense: sans, medium column, tight spacing, small text.</summary>
    Compact,

    /// <summary>Print-like: serif, medium column, normal spacing.</summary>
    Printable,
}

/// <param name="Font">Preview body font family.</param>
/// <param name="Width">Reading-column width preset.</param>
/// <param name="Density">Text density preset (writes line/paragraph spacing).</param>
/// <param name="FontSize">Preview/editor font size (drives PreviewScale = size / 14).</param>
public sealed record ReadingTemplatePreset(ReadingFont Font, ReadingWidth Width, ReadingDensity Density, double FontSize);

public static class ReadingTemplates
{
    /// <summary>The concrete knob values behind each template (adapted from the source's px/ratio values).</summary>
    public static ReadingTemplatePreset For(ReadingTemplate template) => template switch
    {
        ReadingTemplate.Reading => new(ReadingFont.Serif, ReadingWidth.Narrow, ReadingDensity.Relaxed, 17),
        ReadingTemplate.Developer => new(ReadingFont.Mono, ReadingWidth.Full, ReadingDensity.Normal, 14),
        ReadingTemplate.Compact => new(ReadingFont.Sans, ReadingWidth.Comfort, ReadingDensity.Compact, 13),
        ReadingTemplate.Printable => new(ReadingFont.Serif, ReadingWidth.Comfort, ReadingDensity.Normal, 15),
        _ => new(ReadingFont.Sans, ReadingWidth.Comfort, ReadingDensity.Normal, 14),
    };
}
