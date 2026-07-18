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
/// must actually re-space the wrapped text AND re-gap the blocks. Density now drives two things:
/// LineSpacingFor (extra px between WRAPPED LINES inside a block) and BlockSpacingFor (the bottom Margin
/// between TOP-LEVEL blocks — the paragraph rhythm added 2026-07-18 for the VS-Code look). Written while
/// chasing a "density does nothing" report; the path itself measures correct.</summary>
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

    /// <summary>The block rhythm: a top-level paragraph's bottom Margin (the gap to the next block) must
    /// widen with density — the paragraph spacing the render lacked entirely before 2026-07-18.</summary>
    [AvaloniaFact]
    public void ReadingDensity_Reflow_WidensTheGapBetweenBlocks()
    {
        var (window, view, vm) = Open(ReadingDensity.Compact);

        // Max bottom Margin over the NON-heading text blocks = the top-level paragraph's block gap.
        double BlockGap() => view.GetVisualDescendants()
            .OfType<ColorTextBlock.Avalonia.CTextBlock>()
            .Where(t => !t.Classes.Any(c => c.StartsWith("Heading")))
            .Select(t => t.Margin.Bottom)
            .DefaultIfEmpty(0)
            .Max();

        view.RunPreviewReflowPassesForTest();
        var compactGap = BlockGap();
        Assert.True(compactGap > 0, "even Compact must gap paragraphs (no more butting blocks)");

        vm.Layout!.ReadingDensity = ReadingDensity.Relaxed;
        view.RunPreviewReflowPassesForTest();
        Dispatcher.UIThread.RunJobs();

        Assert.True(
            BlockGap() > compactGap,
            $"relaxed density must widen the block gap (compact was {compactGap})");

        window.Close();
    }

    /// <summary>Text alignment propagates to the preview's text blocks on reflow.</summary>
    [AvaloniaFact]
    public void TextAlignment_Reflow_AppliesToTextBlocks()
    {
        var (window, view, vm) = Open(ReadingDensity.Normal);

        vm.Layout!.TextAlignment = TextAlign.Center;
        view.RunPreviewReflowPassesForTest();
        Dispatcher.UIThread.RunJobs();

        var blocks = view.GetVisualDescendants().OfType<ColorTextBlock.Avalonia.CTextBlock>().ToList();
        Assert.NotEmpty(blocks);
        Assert.All(blocks, b => Assert.Equal(Avalonia.Media.TextAlignment.Center, b.TextAlignment));

        window.Close();
    }

    /// <summary>Heading scale multiplies the section-heading font size against its captured base (so 1.0 is
    /// the renderer's own size and a larger scale grows it).</summary>
    [AvaloniaFact]
    public void HeadingScale_Reflow_ResizesHeadings()
    {
        var (window, view, vm) = Open(ReadingDensity.Normal);

        double HeadingSize() => view.GetVisualDescendants()
            .OfType<ColorTextBlock.Avalonia.CTextBlock>()
            .Where(b => b.Classes.Any(c => c.StartsWith("Heading")))
            .Select(b => b.FontSize)
            .DefaultIfEmpty(0)
            .Max();

        var baseSize = HeadingSize();
        Assert.True(baseSize > 0, "sample must render a heading");

        vm.Layout!.HeadingScale = 1.5;
        view.RunPreviewReflowPassesForTest();
        Dispatcher.UIThread.RunJobs();

        Assert.True(HeadingSize() > baseSize + 1,
            $"HeadingScale 1.5 must grow the heading (base was {baseSize})");

        window.Close();
    }
}
