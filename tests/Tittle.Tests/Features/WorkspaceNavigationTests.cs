using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Tittle.Core.Abstractions;
using Tittle.Core.Services;
using Tittle.Features.Shell;
using Tittle.Features.Shell.Workspace;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>Headless coverage for the Task 3 workspace rail/sidebar leaf controls.</summary>
public sealed class WorkspaceNavigationTests
{
    private static MainWindowViewModel CreateVm(string content = "", string[]? args = null,
        ViewStateStore? viewState = null, IRecentFilesStore? recent = null)
        => new(
            new FakeFileDialogService(null), new FakeFileReader(content), new FakeThemeService(),
            recent ?? new FakeRecentFilesStore(),
            new AppSettingsService(new FakeSettingsStore()), new FakeClipboardService(),
            new FakeShellService(), args ?? Array.Empty<string>(), viewState: viewState);

    [AvaloniaFact]
    public void WorkspaceRail_ConcreteActions_HaveMeaningfulAutomationNames()
    {
        var vm = CreateVm();
        var rail = new WorkspaceRail { DataContext = vm };
        var window = new Window { Width = 60, Height = 420, Content = rail };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var names = rail.GetVisualDescendants().OfType<Button>()
            .Select(Avalonia.Automation.AutomationProperties.GetName)
            .Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();

        Assert.Contains("Открыть файлы", names);
        Assert.Contains("Поиск", names);
        Assert.Contains("Оглавление", names);
        Assert.Contains("Закладки", names);
        Assert.Contains("Командная палитра", names);
        Assert.Contains("Настройки", names);
        Assert.Contains("Справка", names);
        window.Close();
    }

    [AvaloniaFact]
    public void WorkspaceRail_Actions_AreKeyboardReachableAndSized()
    {
        var vm = CreateVm();
        var rail = new WorkspaceRail { DataContext = vm };
        var window = new Window { Width = 60, Height = 420, Content = rail };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var actions = rail.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Classes.Contains("workspace-action"))
            .ToArray();

        Assert.Equal(7, actions.Length);
        Assert.Equal(Enumerable.Range(0, actions.Length), actions.Select(button => button.TabIndex));
        Assert.All(actions, button =>
        {
            Assert.True(button.Focusable);
            Assert.True(button.IsTabStop);
            Assert.True(button.Bounds.Width >= 24);
            Assert.True(button.Bounds.Height >= 24);
            Assert.False(string.IsNullOrWhiteSpace(Avalonia.Automation.AutomationProperties.GetName(button)));
        });

        window.Close();
    }

    [AvaloniaFact]
    public void WorkspaceRail_SectionAction_UpdatesSidebarSection()
    {
        var vm = CreateVm("# Heading");
        var rail = new WorkspaceRail { DataContext = vm };
        var window = new Window { Width = 60, Height = 420, Content = rail };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var outline = rail.GetVisualDescendants().OfType<Button>()
            .Single(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Оглавление");
        outline.Command?.Execute(outline.CommandParameter);

        Assert.True(vm.IsWorkspaceSidebarVisible);
        Assert.Equal(Tittle.Core.Settings.WorkspaceSection.Outline, vm.Layout.WorkspaceSection);
        window.Close();
    }

    [AvaloniaFact]
    public void WorkspaceSidebar_OutlineWithoutHeadings_ShowsActionableEmptyState()
    {
        var vm = CreateVm("plain text", new[] { "/docs/readme.txt" });
        vm.OpenWorkspaceSectionCommand.Execute(Tittle.Core.Settings.WorkspaceSection.Outline);
        var sidebar = new WorkspaceSidebar { DataContext = vm };
        var window = new Window { Width = 360, Height = 420, Content = sidebar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var text = string.Join(" ", sidebar.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));
        Assert.Contains("Нет заголовков", text);
        window.Close();
    }

    [AvaloniaFact]
    public void WorkspaceSidebar_OutlineWithHeadings_RendersHeadingItems()
    {
        var vm = CreateVm("# First\n\n## Second", new[] { "/docs/readme.md" });
        vm.OpenWorkspaceSectionCommand.Execute(Tittle.Core.Settings.WorkspaceSection.Outline);
        var sidebar = new WorkspaceSidebar { DataContext = vm };
        var window = new Window { Width = 360, Height = 420, Content = sidebar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var labels = sidebar.GetVisualDescendants().OfType<Button>().Select(b => b.Content?.ToString()).ToArray();
        Assert.Contains(labels, value => value == "First");
        Assert.Contains(labels, value => value == "Second");

        var names = sidebar.GetVisualDescendants().OfType<Button>()
            .Select(Avalonia.Automation.AutomationProperties.GetName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        Assert.Contains("First", names);
        Assert.Contains("Second", names);
        window.Close();
    }

    [AvaloniaFact]
    public void WorkspaceSidebar_RecentFiles_ExposeAutomationNames()
    {
        var recent = new FakeRecentFilesStore();
        // Forward slashes so Path.GetFileName yields "readme.md" on every OS (a "\"-separated path is a
        // single name on Linux/macOS, where "\" is an ordinary character — the test failed there otherwise).
        recent.Add("/docs/readme.md");
        var vm = CreateVm(recent: recent);
        var sidebar = new WorkspaceSidebar { DataContext = vm };
        var window = new Window { Width = 360, Height = 420, Content = sidebar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var names = sidebar.GetVisualDescendants().OfType<Button>()
            .Select(Avalonia.Automation.AutomationProperties.GetName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Assert.Contains("readme.md", names);
        window.Close();
    }

    [AvaloniaFact]
    public void BookmarkedOutline_ProjectsOnlyBookmarkedHeadings_AndNotifiesOnToggle()
    {
        var state = new ViewStateStore(new FakeSettingsStore());
        var vm = CreateVm("# First\n\n## Second\n\n### Third", new[] { "/docs/readme.md" }, state);
        var tab = vm.SelectedTab!;
        var notifications = new List<string?>();
        tab.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        tab.ToggleBookmarkCommand.Execute(tab.Outline[1]);

        var projection = Assert.Single(tab.BookmarkedOutline);
        Assert.Equal("Second", projection.Text);
        Assert.Contains("BookmarkedOutline", notifications);
    }
}
