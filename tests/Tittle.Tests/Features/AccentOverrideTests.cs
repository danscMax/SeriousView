using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>Proves the mechanism the user-accent feature relies on: a window-level resource override wins
/// over the app's per-theme <c>AccentBrush</c> (defined in Application ThemeDictionaries), because the
/// window's own resources are consulted before the lookup reaches Application. If this ever regresses,
/// the accent swatches would silently stop recolouring the chrome.</summary>
public class AccentOverrideTests
{
    [AvaloniaFact]
    public void WindowResourceOverride_WinsOverThemeDictAccent()
    {
        var window = new Window { RequestedThemeVariant = ThemeVariant.Dark };

        // Baseline: the app's Dark theme dictionary supplies AccentBrush (it is not overridden yet).
        Assert.True(window.TryFindResource("AccentBrush", ThemeVariant.Dark, out var themeBrush));
        Assert.NotNull(themeBrush);

        var red = new SolidColorBrush(Colors.Red);
        window.Resources["AccentBrush"] = red;

        Assert.True(window.TryFindResource("AccentBrush", ThemeVariant.Dark, out var resolved));
        Assert.Same(red, resolved); // the window override wins over the theme-dict entry
    }

    [AvaloniaFact]
    public void AppResourceOverride_WinsOverThemeDictAccent_AndReachesEveryWindow()
    {
        // The user accent must recolour ALL top-levels (command palette, donate window), not just
        // MainWindow — so it must be applied at the Application level and still win over the theme dict.
        var app = Avalonia.Application.Current!;
        var had = app.Resources.TryGetValue("AccentBrush", out var previous);
        try
        {
            var red = new SolidColorBrush(Colors.Red);
            app.Resources["AccentBrush"] = red;

            var window = new Window { RequestedThemeVariant = ThemeVariant.Dark };
            Assert.True(window.TryFindResource("AccentBrush", ThemeVariant.Dark, out var resolved));
            Assert.Same(red, resolved); // an app-level direct entry wins over the theme-dictionary entry
        }
        finally
        {
            if (had)
                app.Resources["AccentBrush"] = previous;
            else
                app.Resources.Remove("AccentBrush");
        }
    }
}
