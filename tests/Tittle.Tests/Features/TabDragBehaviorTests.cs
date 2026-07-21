using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Tittle.Features.Shell;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>Drives the real <see cref="TabDragBehavior"/> with simulated pointer input on a headless
/// ListBox (MainWindow itself can't render headless — the FluentAvalonia Symbols crash). Verifies a
/// horizontal drag actually reorders the model, and that a plain click does not.</summary>
public class TabDragBehaviorTests
{
    private static (Window window, ListBox list, ObservableCollection<string> items) BuildStrip()
    {
        var items = new ObservableCollection<string> { "A", "B", "C", "D", "E" };
        var list = new ListBox
        {
            ItemsSource = items,
            ItemsPanel = new FuncTemplate<Panel?>(() => new TabsPanel()),
            ItemTemplate = new FuncDataTemplate<string>((s, _) => new Border
            {
                Width = 100,
                Height = 30,
                Child = new TextBlock { Text = s },
            }),
        };
        // Commit like the app's MainWindow.CommitTabMove → MainWindowViewModel.MoveTab.
        _ = new TabDragBehavior(list, (item, target) =>
        {
            var from = items.IndexOf((string)item);
            if (from >= 0 && from != target)
                items.Move(from, target);
        });
        var window = new Window { Width = 800, Height = 120, Content = list };
        return (window, list, items);
    }

    private static Point CentreOf(ListBox list, Window window, int index)
    {
        var c = (Control)list.ContainerFromIndex(index)!;
        return c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), window)!.Value;
    }

    [AvaloniaFact]
    public void DragTabRight_ReordersModel()
    {
        var (window, list, items) = BuildStrip();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var start = CentreOf(list, window, 0);
        var overC = CentreOf(list, window, 2);
        var drop = new Point(overC.X + 1, start.Y); // just past tab C's centre → A should land at index 2

        window.MouseDown(start, MouseButton.Left);
        // The button stays held during the move (the OS sets this; headless needs it passed explicitly).
        window.MouseMove(drop, RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(drop, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new[] { "B", "C", "A", "D", "E" }, items);
        window.Close();
    }

    [AvaloniaFact]
    public void DragTabLeft_ReordersModel()
    {
        var (window, list, items) = BuildStrip();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var start = CentreOf(list, window, 3);      // grab D
        var overB = CentreOf(list, window, 1);
        var drop = new Point(overB.X - 1, start.Y); // just left of tab B's centre → D lands at index 1

        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(drop, RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();
        window.MouseUp(drop, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new[] { "A", "D", "B", "C", "E" }, items);
        window.Close();
    }

    [AvaloniaFact]
    public void PlainClick_DoesNotReorder()
    {
        var (window, list, items) = BuildStrip();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var p = CentreOf(list, window, 1);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left); // press + release, no movement past the threshold
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, items);
        window.Close();
    }
}
