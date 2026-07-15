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
using Tittle.Features.Shell.Workspace;
using Tittle.Platform;
using Tittle.Shared;
using Xunit;

namespace Tittle.Tests.Features;

[Collection("MainWindow UI")]
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

        var automationIds = window.GetLogicalDescendants()
            .OfType<Control>()
            .Select(AutomationProperties.GetAutomationId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet();
        Assert.Contains("WorkspaceHeader", automationIds);
        Assert.Contains("WorkspaceRail", automationIds);
        Assert.Contains("WorkspaceSidebar", automationIds);
        Assert.Contains("DocumentHeader", automationIds);
        Assert.Contains("DocumentHost", automationIds);
        Assert.Contains("WorkspaceStatusStrip", automationIds);
        Assert.Contains("WorkspaceSplitter", automationIds);
        Assert.Single(window.GetLogicalDescendants().OfType<WorkspaceRail>());
        Assert.Single(window.GetLogicalDescendants().OfType<WorkspaceSidebar>());

        var titleGrid = window.FindControl<Grid>("TitleGrid");
        Assert.NotNull(titleGrid);
        Assert.Equal(40, titleGrid!.MinHeight);
        var statusStrip = window.GetLogicalDescendants().OfType<Border>()
            .Single(border => AutomationProperties.GetAutomationId(border) == "WorkspaceStatusStrip");
        Assert.Equal(24, statusStrip.Height);

        var segmented = window.GetLogicalDescendants()
            .OfType<Border>()
            .Single(b => b.Classes.Contains("segmented"));
        var segments = window.GetLogicalDescendants()
            .OfType<Button>()
            .Where(b => b.Classes.Contains("segment"))
            .ToList();
        Assert.Equal(new Thickness(1), segmented.BorderThickness);
        Assert.NotEmpty(segments);
        Assert.All(segments, button => Assert.Equal(new Thickness(10, 6), button.Padding));

        window.ApplyResponsiveHeaderForTest(560);
        var compactStatus = window.FindControl<Border>("WorkspaceStatusStrip");
        Assert.NotNull(compactStatus);
        Assert.Contains("compact-status", compactStatus!.Classes);
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

    [AvaloniaFact]
    public void ActiveWorkspaceControls_HaveNonColorCues()
    {
        var workspace = new Button { Content = "workspace" };
        workspace.Classes.Add("workspace-action");
        workspace.Classes.Add("active");
        var segment = new Button { Content = "segment" };
        segment.Classes.Add("segment");
        segment.Classes.Add("seg-on");
        var window = new Window
        {
            Content = new StackPanel { Children = { workspace, segment } },
            Width = 200,
            Height = 120,
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var workspacePresenter = workspace.GetVisualDescendants()
                .OfType<ContentPresenter>()
                .First(p => p.Name == "PART_ContentPresenter");
            Assert.True(workspacePresenter.BorderThickness.Left > workspacePresenter.BorderThickness.Right,
                "Active workspace action has no structural marker");
            Assert.Equal(FontWeight.SemiBold, segment.FontWeight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void WorkspaceActions_FollowSixTenFourteenRhythm()
    {
        var workspace = new Button();
        workspace.Classes.Add("workspace-action");
        var header = new Button();
        header.Classes.Add("header-action");
        var segment = new Button();
        segment.Classes.Add("segment");
        var window = new Window
        {
            Content = new StackPanel { Children = { workspace, header, segment } },
            Width = 200,
            Height = 160,
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new Thickness(10), workspace.Padding);
            Assert.Equal(36, workspace.Width);
            Assert.Equal(36, workspace.Height);
            Assert.Equal(new CornerRadius(6), workspace.CornerRadius);
            Assert.Equal(new Thickness(10, 6), header.Padding);
            Assert.Equal(new CornerRadius(6), header.CornerRadius);
            Assert.Equal(new Thickness(10, 6), segment.Padding);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MainWindow_CtrlShiftE_OpensFilesWorkspaceSection()
    {
        var vm = new MainWindowViewModel(
            new FakeFileDialogService(null), new FakeFileReader("# Heading"), new FakeThemeService(),
            new FakeRecentFilesStore(), new AppSettingsService(new FakeSettingsStore()),
            new FakeClipboardService(), new FakeShellService(), Array.Empty<string>());
        vm.OpenWorkspaceSectionCommand.Execute(WorkspaceSection.Outline);
        Assert.True(MainWindow.IsFilesWorkspaceShortcut(
            Key.E, KeyModifiers.Control | KeyModifiers.Shift));
        vm.OpenWorkspaceSectionCommand.Execute(WorkspaceSection.Files);
        Assert.True(vm.IsFilesSectionActive);
    }

    [AvaloniaFact]
    public void MainWindow_NarrowHeader_HidesNonessentialOmnibarTextAndRestoresIt()
    {
        Assert.Equal((false, 0d), MainWindow.ResponsiveHeaderLayout(560));
        Assert.Equal((true, 340d), MainWindow.ResponsiveHeaderLayout(960));
    }

    [AvaloniaFact]
    public void MainWindow_CollapsedSidebar_ReclaimsItsColumnAndRestoresWidth()
    {
        var vm = new MainWindowViewModel(
            new FakeFileDialogService(null), new FakeFileReader("# Heading"), new FakeThemeService(),
            new FakeRecentFilesStore(), new AppSettingsService(new FakeSettingsStore()),
            new FakeClipboardService(), new FakeShellService(),
            Array.Empty<string>());

        var rememberedWidth = LayoutOptions.DefaultOutlineWidth;
        Assert.True(LayoutOptions.ClampOutlineWidth(rememberedWidth)
                    >= LayoutOptions.MinOutlineWidth);

        vm.OpenWorkspaceSectionCommand.Execute(WorkspaceSection.Files);
        Assert.False(vm.IsWorkspaceSidebarVisible);

        vm.OpenWorkspaceSectionCommand.Execute(WorkspaceSection.Files);
        Assert.True(vm.IsWorkspaceSidebarVisible);
        Assert.True(LayoutOptions.ClampOutlineWidth(rememberedWidth)
                    >= LayoutOptions.MinOutlineWidth);
    }

    [AvaloniaFact]
    public void CommandPalette_RetainsOutlineAction()
    {
        var vm = new MainWindowViewModel(
            new FakeFileDialogService(null), new FakeFileReader("# Heading"), new FakeThemeService(),
            new FakeRecentFilesStore(), new AppSettingsService(new FakeSettingsStore()),
            new FakeClipboardService(), new FakeShellService(), Array.Empty<string>());

        Assert.Contains(vm.BuildPaletteItems(), item => item.Title == "Оглавление");
    }

}

[CollectionDefinition("MainWindow UI", DisableParallelization = true)]
public sealed class MainWindowUiCollection { }
