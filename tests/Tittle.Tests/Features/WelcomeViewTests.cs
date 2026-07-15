using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Tittle.Core.Documents;
using Tittle.Core.Services;
using Tittle.Features.Shell;
using Tittle.Features.Viewer;
using Tittle.Features.Welcome;
using Xunit;

namespace Tittle.Tests.Features;

public sealed class WelcomeViewTests
{
    private static MainWindowViewModel CreateVm(FakeRecentFilesStore? recent = null) =>
        new(new FakeFileDialogService(null), new FakeFileReader("sample"), new FakeThemeService(),
            recent ?? new FakeRecentFilesStore(), new AppSettingsService(new FakeSettingsStore()),
            new FakeClipboardService(), new FakeShellService(), Array.Empty<string>());

    [AvaloniaFact]
    public void Welcome_ExposesStartActionsDropTargetKeyboardHintAndRecentFiles()
    {
        var recent = new FakeRecentFilesStore();
        recent.Add("/docs/readme.md");
        var vm = CreateVm(recent);
        var view = new WelcomeView { DataContext = vm };
        var window = new Window { Width = 760, Height = 560, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var names = view.GetVisualDescendants().OfType<Control>()
            .Select(AutomationProperties.GetName).Where(name => !string.IsNullOrWhiteSpace(name)).ToHashSet();
        Assert.Contains("Открыть файл", names);
        Assert.Contains("Открыть пример", names);
        Assert.Contains("Область перетаскивания файла", names);

        var text = string.Join(" ", view.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text));
        Assert.Contains("Перетащите файл", text);
        Assert.Contains("Ctrl+O", text);
        Assert.Contains("readme.md", text);

        var sample = view.GetVisualDescendants().OfType<Button>()
            .Single(button => AutomationProperties.GetName(button) == "Открыть пример");
        sample.Command?.Execute(sample.CommandParameter);
        Assert.True(vm.HasTabs);
        window.Close();
    }

    [AvaloniaFact]
    public void NoticeState_UsesConsistentWorkspacePadding()
    {
        var vm = DocumentTabViewModel.FromLoad(FileLoadResult.Binary(2048), "/docs/data.bin");
        var view = new DocumentView { DataContext = vm };
        var window = new Window { Width = 760, Height = 560, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var notice = view.GetVisualDescendants().OfType<Border>()
            .Single(border => AutomationProperties.GetAutomationId(border) == "DocumentNotice");
        Assert.Equal(new Thickness(14), notice.Padding);
        window.Close();
    }
}
