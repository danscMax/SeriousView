using System;
using Avalonia.Media;

namespace Tittle.Shared;

/// <summary>Colour helpers for applying a user-chosen accent (ported <c>state.accentColor</c>).
/// Accents are <c>#RRGGBB</c>; an empty string means "use the theme's own accent".</summary>
public static class AccentPalette
{
    // The selectable swatch brushes live here (data, one source) rather than as hex literals in the
    // settings AXAML — keeps the view free of hard-coded colours (the visual-canon adoption guard) while
    // still letting a colour picker show real colours. Each hex is also the SetAccent command parameter.
    public static IBrush Blue { get; } = Swatch("#3B82F6");
    public static IBrush Purple { get; } = Swatch("#8B5CF6");
    public static IBrush Green { get; } = Swatch("#22C55E");
    public static IBrush Red { get; } = Swatch("#EF4444");
    public static IBrush Orange { get; } = Swatch("#F97316");
    public static IBrush Pink { get; } = Swatch("#EC4899");

    private static IBrush Swatch(string hex) => new SolidColorBrush(Color.Parse(hex));

    /// <summary>Parse a <c>#RRGGBB</c> accent, or null for empty/invalid (→ the theme default).</summary>
    public static Color? TryParse(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;
        return Color.TryParse(hex, out var c) ? c : null;
    }

    /// <summary>A lighter shade of the accent for hover states — blend toward white by
    /// <paramref name="amount"/> (0..1).</summary>
    public static Color Lighten(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte Mix(byte v) => (byte)Math.Round(v + (255 - v) * amount);
        return Color.FromArgb(c.A, Mix(c.R), Mix(c.G), Mix(c.B));
    }
}
