using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Tittle.Core.Settings;
using Tittle.Features.Shell;
using Tittle.Features.Viewer;
using Tittle.Shared;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>Pins the live "Разреженность текста" (ReadingDensity) path end to end: changing the shared
/// LayoutOptions must schedule a preview reflow (the Layout subscription stays live), and that reflow
/// must actually re-space the wrapped text. Note what density does and does NOT do: LineSpacingFor maps
/// Compact/Normal/Relaxed to 0/4/9 extra px BETWEEN WRAPPED LINES inside a block — it does not change the
/// gaps between paragraphs or headings, so on a document whose lines never wrap it is invisible by
/// design. Written while chasing a "density does nothing" report; the path itself measures correct.</summary>
public class DocumentViewReadingDensityTests
{
    private const string Sample = """
        # Heading

        A deliberately long paragraph of prose that wraps across several lines at a constrained width,
        so the per-line reading-density spacing measurably changes the wrapped block's height when the
        setting is toggled live rather than only on a fresh open of the document. More words keep it
        wrapping onto additional lines regardless of the exact font metrics under the headless renderer.
        """;

    private static (Window Window, DocumentView View, DocumentTabViewModel Vm) Open(ReadingDensity density)
    {
        var vm = DocumentTabViewModel.FromFile(Sample, "/docs/readme.md");
        vm.Layout = new LayoutOptions { ReadingDensity = density };
        var view = new DocumentView { DataContext = vm };
        var window = new Window { Width = 520, Height = 640, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, view, vm);
    }

    /// <summary>The trigger: a live density change on the SHARED layout must schedule a reflow, i.e. the
    /// view's Layout subscription (wired only when tab.Layout is non-null at wire-up) is live.</summary>
    [AvaloniaFact]
    public void ReadingDensity_ChangedLive_SchedulesAPreviewReflow()
    {
        var (window, view, vm) = Open(ReadingDensity.Compact);

        var scheduledBefore = view.PreviewReflowScheduleCount;
        vm.Layout!.ReadingDensity = ReadingDensity.Relaxed;

        Assert.True(
            view.PreviewReflowScheduleCount > scheduledBefore,
            "a live ReadingDensity change must schedule a preview reflow (the Layout subscription must be live)");

        window.Close();
    }

    /// <summary>The application: once the pass runs, the wrapped text must actually re-space.</summary>
    [AvaloniaFact]
    public void ReadingDensity_Reflow_RespacesWrappedText()
    {
        var (window, view, vm) = Open(ReadingDensity.Compact);

        double TotalTextHeight() => view.GetVisualDescendants()
            .OfType<ColorTextBlock.Avalonia.CTextBlock>()
            .Sum(t => t.Bounds.Height);

        var compactHeight = TotalTextHeight();
        Assert.True(compactHeight > 0); // sanity: the preview actually laid out wrapped text

        vm.Layout!.ReadingDensity = ReadingDensity.Relaxed;
        view.RunPreviewReflowPassesForTest();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.True(
            TotalTextHeight() > compactHeight,
            $"relaxed spacing must grow the wrapped text (was {compactHeight})");

        window.Close();
    }
}
