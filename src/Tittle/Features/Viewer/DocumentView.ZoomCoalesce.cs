using System;
using System.ComponentModel;
using Avalonia.Media;
using Avalonia.Threading;
using Tittle.Shared;

namespace Tittle.Features.Viewer;

// Zoom-burst coalescing for the preview. The reading zoom is a LAYOUT scale on PreviewZoom
// (LayoutTransformControl), so every step re-measures the whole non-virtualised Markdown.Avalonia
// document (~68 ms/step on a large doc — see Reflow.cs) — and key auto-repeat / Ctrl+wheel bursts
// fire steps at input rate, queuing seconds of layout work of which all but the last is discarded.
// So a PreviewScale change never touches the transform directly: it STORES the pending scale and
// restarts a short trailing timer; the tick applies the LAST scale once (= exactly one full
// re-measure per burst). Instant feedback is deliberately sacrificed for batching (≤120 ms added
// latency per single step).
//
// A change arriving while the preview pane is hidden (source mode / notice / table view) is stored
// without arming the timer and applied lazily on the next ShowPreviewPane transition (the ViewMode
// branch of OnVmPropertyChanged), so the re-measure never runs for an invisible control.
public partial class DocumentView
{
    // Trailing-edge window; mirrors the resize settle debounce in Reflow.cs.
    private const int ZoomSettleMilliseconds = 120;

    private readonly DispatcherTimer _zoomSettleTimer;
    private double? _pendingPreviewScale;

    /// <summary>The scale awaiting application (last writer wins), or null when settled.</summary>
    // Test seams (headless): ZoomApplyCount counts COALESCED applications (trailing tick + lazy
    // apply on show) — the synchronous open-layout apply is load work, not counted (like the primed
    // first reflow pass).
    internal int ZoomApplyCount { get; private set; }

    /// <summary>ScaleX/Y currently set on PreviewZoom's layout transform (test seam).</summary>
    internal double PreviewAppliedScaleForTest =>
        PreviewZoom.LayoutTransform is ScaleTransform st ? st.ScaleX : 1.0;

    internal void SettlePreviewZoomForTest() => OnZoomSettleTick(null, EventArgs.Empty);

    // Wired from OnDataContextChanged / unwired in Unsubscribe: EditorOptions is ONE instance shared
    // by every tab (AdoptTab), so each view tracks it for its own pane.
    private void WirePreviewZoomCoalescing()
    {
        if (_vm?.Editor is null)
            return; // bare-VM harnesses (and non-document content) carry no editor options
        _vm.Editor.PropertyChanged += OnEditorPropertyChanged;
        // Open layout: apply the CURRENT scale synchronously, once. The LayoutTransform binding was
        // REMOVED from DocumentView.axaml (PreviewZoom) — it applied every step instantly, which is
        // exactly what this machinery coalesces away. The transform moves ONLY from here; don't
        // re-add a binding on PreviewZoom.LayoutTransform.
        ApplyPreviewScale(_vm.Editor.PreviewScale);
    }

    private void UnwirePreviewZoomCoalescing()
    {
        if (_vm?.Editor is not null)
            _vm.Editor.PropertyChanged -= OnEditorPropertyChanged;
        _zoomSettleTimer.Stop();
        _pendingPreviewScale = null;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(EditorOptions.PreviewScale) || _vm?.Editor is null)
            return;
        var scale = _vm.Editor.PreviewScale;
        if (!_vm.ShowPreviewPane)
        {
            // Hidden preview: defer — consumed when the pane next becomes visible.
            _pendingPreviewScale = scale;
            return;
        }

        _pendingPreviewScale = scale; // last writer wins
        _zoomSettleTimer.Stop();
        _zoomSettleTimer.Start();
    }

    private void OnZoomSettleTick(object? sender, EventArgs e)
    {
        _zoomSettleTimer.Stop();
        if (_vm is null || !_vm.ShowPreviewPane)
            return; // mode flipped away mid-burst: stay pending for the next show
        ApplyPendingZoom();
    }

    // Called from the ViewMode branch of OnVmPropertyChanged: consume a deferred zoom when the
    // preview pane comes back (a zoom stored while hidden never armed the timer).
    private void ApplyPendingZoomIfPreviewVisible()
    {
        if (_vm is null || !_vm.ShowPreviewPane || _pendingPreviewScale is null)
            return;
        _zoomSettleTimer.Stop(); // consumed here; a still-running timer must not fire again
        ApplyPendingZoom();
    }

    // The single full re-measure of a burst happens here.
    private void ApplyPendingZoom()
    {
        if (_pendingPreviewScale is not { } scale)
            return;
        _pendingPreviewScale = null; // cleared BEFORE applying — no re-entrant double-apply
        ApplyPreviewScale(scale);
        ZoomApplyCount++;
    }

    // Fresh transform per application, mirroring ScaleTransformConverter (a Transform lives outside
    // the visual/logical tree, so it cannot bind — hence this explicit write).
    private void ApplyPreviewScale(double scale)
    {
        var s = scale > 0 ? scale : 1.0;
        PreviewZoom.LayoutTransform = new ScaleTransform(s, s);
    }
}
