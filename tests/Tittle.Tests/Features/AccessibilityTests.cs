using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Tittle.Core.Abstractions;
using Tittle.Core.Services;
using Tittle.Core.Settings;
using Tittle.Features.Shell;
using Tittle.Platform;
using Xunit;

namespace Tittle.Tests.Features;

public class AccessibilityTests
{
    // The chrome's glyph buttons (↵ # A− A+ 📂 ⌘) and the go-to-line box read as their raw glyph (or
    // nothing) to a screen reader without an explicit name. Assert the accessible names are wired.
    // Read from the logical tree right after construction — no Show()/RunJobs(), so the FluentAvalonia
    // FontIcon render (which the headless font manager can't shape) never runs.
    [AvaloniaFact]
    public void ChromeGlyphControls_ExposeAccessibleNames()
    {
        var window = new MainWindow();

        var names = window.GetLogicalDescendants()
            .OfType<Control>()
            .Select(AutomationProperties.GetName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet();

        Assert.Contains("Перенос строк", names);   // status bar ↵
        Assert.Contains("Уменьшить шрифт", names);  // status bar A−
        Assert.Contains("Размер шрифта", names);    // status bar {FontSize}
        Assert.Contains("Открыть файл", names);     // omnibar 📂
        Assert.Contains("Палитра команд", names);   // omnibar ⌘
        Assert.Contains("Номер строки", names);     // go-to-line box
    }

    [AvaloniaFact]
    public void EveryTheme_ResolvesWorkspaceShellTokens()
    {
        var app = Application.Current!;
        var service = new ThemeService(new AppSettingsService(new FakeSettingsStore()));
        string[] keys =
        [
            "WorkspaceRailBackgroundBrush",
            "WorkspaceSidebarBackgroundBrush",
            "WorkspaceHeaderBackgroundBrush",
            "WorkspaceHoverBrush",
            "WorkspaceSelectedBrush",
            "WorkspaceSeparatorBrush",
        ];

        foreach (var theme in ThemeCatalog.All)
        {
            if (theme.Mode == ThemeMode.Auto)
                continue;

            service.SetMode(theme.Mode);
            foreach (var key in keys)
                Assert.True(app.TryGetResource(key, app.RequestedThemeVariant, out _),
                    $"{theme.Mode}: {key} is unresolved");
        }
    }

    [AvaloniaFact]
    public void WorkspaceActions_ShowVisibleKeyboardFocus()
    {
        foreach (var styleClass in new[] { "workspace-action", "header-action", "segment" })
        {
            var button = new Button { Content = styleClass };
            button.Classes.Add(styleClass);
            var window = new Window { Content = button, Width = 160, Height = 80 };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                button.Focus(NavigationMethod.Tab);
                Dispatcher.UIThread.RunJobs();

                var presenter = button.GetVisualDescendants()
                    .OfType<ContentPresenter>()
                    .First(p => p.Name == "PART_ContentPresenter");
                Assert.True(presenter.BorderThickness.Left > 0,
                    $"{styleClass} has no visible keyboard focus border");
                var focusBrush = Assert.IsAssignableFrom<ISolidColorBrush>(presenter.BorderBrush);
                Assert.True(focusBrush.Color.A > 0,
                    $"{styleClass} keyboard focus border is transparent");
            }
            finally
            {
                window.Close();
            }
        }
    }
}
