using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Tittle.Features.Shell;
using Tittle.Features.Viewer;
using Tittle.Shared;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>Guards the resize-storm fix (A2): the heavy preview-reflow passes (heading-Y rebuild,
/// embedded code-editor height pinning, sorter/collapser attach) must NOT run once per extent-change
/// frame. The first layout primes them immediately; every later reflow coalesces onto a debounce.
/// Also guards the zoom-burst fix: PreviewScale changes coalesce into ONE trailing transform
/// application instead of one full document re-measure per step.</summary>
public class DocumentViewReflowTests
{
    // Image-free markdown with headings, a table and a code fence — exercises every heavy pass.
    private const string Sample = """
        # First

        Text under the first heading with **bold**.

        | A | B |
        |---|---|
        | 1 | 2 |

        ## Second

        ```cs
        var x = 1;
        ```
        """;

    [AvaloniaFact]
    public void Preview_FirstLayout_PrimesReflowImmediately()
    {
        var vm = DocumentTabViewModel.FromFile(Sample, "/docs/readme.md");
        var view = new DocumentView { DataContext = vm };
        var window = new Window { Width = 800, Height = 600, Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The opened document is complete in one frame — the first reflow ran synchronously.
        Assert.True(view.PreviewReflowPassCount >= 1);

        window.Close();
    }

    [AvaloniaFact]
    public void Preview_ResizeStorm_CoalescesReflow_NotPerFrame()
    {
        var vm = DocumentTabViewModel.FromFile(Sample, "/docs/readme.md");
        var view = new DocumentView { DataContext = vm };
        var window = new Window { Width = 800, Height = 600, Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var primed = view.PreviewReflowPassCount; // first layout's immediate pass
        Assert.True(primed >= 1);

        // A burst of extent changes (a resize drag) must not each run the heavy pass — they all
        // restart the debounce timer, which has not fired (the headless clock isn't advanced).
        for (var i = 0; i < 12; i++)
            view.SimulatePreviewExtentChangeForTest();

        Assert.Equal(primed, view.PreviewReflowPassCount); // zero extra synchronous passes
        Assert.True(view.PreviewReflowPending);            // all coalesced into one pending run

        window.Close();
    }

    // Zoom-coalescing harness: the shell attaches the shared EditorOptions to every tab (AdoptTab)
    // BEFORE the view binds; the bare-VM tests above have none, so wire one explicitly.
    private static (DocumentTabViewModel Vm, DocumentView View) CreateZoomHarness()
    {
        var vm = DocumentTabViewModel.FromFile(Sample, "/docs/readme.md");
        vm.Editor = new EditorOptions();
        var view = new DocumentView { DataContext = vm };
        var window = new Window { Width = 800, Height = 600, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (vm, view);
    }

    [AvaloniaFact]
    public void Preview_ZoomBurst_AppliesTrailingScaleExactlyOnce()
    {
        var (vm, view) = CreateZoomHarness();

        Assert.Equal(0, view.ZoomApplyCount);                    // open-layout apply is not counted
        Assert.Equal(1.0, view.PreviewAppliedScaleForTest, 3);

        // A burst of zoom steps (key auto-repeat) with NO pumping between them: 1.05 → 1.10 → 1.15.
        foreach (var size in new[] { 14.7, 15.4, 16.1 })
            vm.Editor!.FontSize = size;

        Assert.Equal(0, view.ZoomApplyCount);                      // nothing applied mid-burst
        Assert.Equal(1.0, view.PreviewAppliedScaleForTest, 3);     // transform did NOT move early

        view.SettlePreviewZoomForTest();                           // trailing edge

        Assert.Equal(1, view.ZoomApplyCount);                      // exactly ONE application per burst
        Assert.Equal(vm.Editor!.PreviewScale, view.PreviewAppliedScaleForTest, 3); // the LAST scale won
    }

    [AvaloniaFact]
    public void Preview_SingleZoomStep_AppliesExactlyOnceOnSettle()
    {
        var (vm, view) = CreateZoomHarness();

        vm.Editor!.FontSize = 17.5; // one Ctrl+= step beyond default

        Assert.Equal(0, view.ZoomApplyCount);
        view.SettlePreviewZoomForTest();

        Assert.Equal(1, view.ZoomApplyCount); // no added multi-fire for a single step
        Assert.Equal(vm.Editor.PreviewScale, view.PreviewAppliedScaleForTest, 3);

        view.SettlePreviewZoomForTest();      // settling again must not re-apply (pending cleared)
        Assert.Equal(1, view.ZoomApplyCount);
    }

    [AvaloniaFact]
    public void Preview_ZoomWhileHidden_IsDeferredToNextPreviewShow()
    {
        var (vm, view) = CreateZoomHarness();

        vm.ViewMode = DocumentViewMode.Source;   // preview pane hidden
        Dispatcher.UIThread.RunJobs();

        vm.Editor!.FontSize = 21;                // scale 1.5 arrives while hidden

        Assert.Equal(0, view.ZoomApplyCount);                  // never applied to an invisible pane
        Assert.Equal(1.0, view.PreviewAppliedScaleForTest, 3); // transform untouched

        vm.ViewMode = DocumentViewMode.Preview;  // the pane comes back → lazy apply, once

        Assert.Equal(1, view.ZoomApplyCount);
        Assert.Equal(vm.Editor.PreviewScale, view.PreviewAppliedScaleForTest, 3);
    }
}
