using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Linq;

namespace Tittle.Features.Shell;

/// <summary>
/// Chrome/Firefox-style tab drag-reorder attached to the tab-strip <see cref="ListBox"/>. The grabbed tab
/// floats under the cursor (a <see cref="TranslateTransform"/>) while its neighbours slide aside to show
/// where it will land; the model reorder is committed ONCE on release (so containers aren't recreated
/// mid-gesture, which would drop the float transform and the pointer capture). A plain click still selects
/// and the close button still closes (presses on any in-tab button are ignored).
///
/// Extracted from MainWindow so the gesture can be exercised headlessly (MainWindow itself can't render
/// under headless Skia — the FluentAvalonia Symbols-font crash). The host supplies the commit callback,
/// which performs the actual reorder (and, in the app, brackets it to suppress the tab entrance fade).
/// </summary>
public sealed class TabDragBehavior
{
    private const double DragThreshold = 5;

    private readonly ListBox _strip;
    private readonly Action<object, int> _commitMove; // (item, targetIndex) — reorders the model

    private object? _dragItem;
    private Panel? _dragPanel;   // the strip's layout panel — the coordinate space for the gesture
    private double _dragPressX;  // cursor X (panel coords) at press, for the movement threshold
    private double _dragGrabOffset;
    private int _dragFromIndex;
    private int _dragTargetIndex;
    private bool _dragging;

    /// <summary>True while a drag has passed the movement threshold — the host reads this to skip the
    /// tab entrance fade on containers recreated by the commit.</summary>
    public bool IsDragging => _dragging;

    public TabDragBehavior(ListBox strip, Action<object, int> commitMove)
    {
        _strip = strip;
        _commitMove = commitMove;
        // Tunnel so we observe the gesture before the ListBox's own pointer handling.
        strip.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        strip.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
        strip.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel);
        strip.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Bubble);
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragItem = null;
        _dragging = false;
        if (!e.GetCurrentPoint(_strip).Properties.IsLeftButtonPressed)
            return;
        // Leave the close button (and any future in-tab button) to its own click.
        if (e.Source is Visual v && (v is Button || v.GetVisualAncestors().OfType<Button>().Any()))
            return;
        if (ItemFrom(e.Source) is not { } container
            || container.DataContext is not { } item
            || container.GetVisualParent() is not Panel panel)
            return;

        _dragItem = item;
        _dragPanel = panel;
        _dragFromIndex = _strip.IndexFromContainer(container);
        _dragTargetIndex = _dragFromIndex;
        var cursor = e.GetPosition(panel).X;
        _dragPressX = cursor;
        _dragGrabOffset = cursor - container.Bounds.Left;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_dragItem is null || _dragPanel is null)
            return;
        if (!e.GetCurrentPoint(_strip).Properties.IsLeftButtonPressed)
        {
            EndDrag(commit: true); // button already up (release fired elsewhere) — drop where we are
            return;
        }

        var cursor = e.GetPosition(_dragPanel).X;
        if (!_dragging)
        {
            if (Math.Abs(cursor - _dragPressX) < DragThreshold)
                return;
            _dragging = true;                // crossed the threshold — this is a drag, not a click
            e.Pointer.Capture(_strip);       // own the gesture: moves/release reach us even off the strip
        }

        UpdateDragVisual(cursor);
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        var wasDragging = _dragging;
        EndDrag(commit: true);
        if (wasDragging)
            e.Pointer.Capture(null);
    }

    // Capture stolen (window deactivation, a flyout, etc.) — cancel the gesture, don't commit a half-drag.
    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndDrag(commit: false);

    // Float the grabbed tab under the cursor and slide the tabs it has passed out of its way.
    private void UpdateDragVisual(double cursor)
    {
        if (_strip.ContainerFromIndex(_dragFromIndex) is not Control dragged)
            return;

        var dx = cursor - _dragGrabOffset - dragged.Bounds.Left;
        dragged.RenderTransform = new TranslateTransform(dx, 0);
        dragged.ZIndex = 1; // float above its neighbours

        var draggedCentre = dragged.Bounds.Center.X + dx;
        var width = dragged.Bounds.Width;
        _dragTargetIndex = ComputeDropIndex(draggedCentre);

        for (var i = 0; i < _strip.ItemCount; i++)
        {
            if (i == _dragFromIndex || _strip.ContainerFromIndex(i) is not Control c)
                continue;
            // A tab between the grab origin and the drop slot slides one tab-width toward the vacated slot.
            double shift = 0;
            if (_dragTargetIndex > _dragFromIndex && i > _dragFromIndex && i <= _dragTargetIndex)
                shift = -width;
            else if (_dragTargetIndex < _dragFromIndex && i >= _dragTargetIndex && i < _dragFromIndex)
                shift = width;
            c.RenderTransform = shift != 0 ? new TranslateTransform(shift, 0) : null;
        }
    }

    // Drop index = how many OTHER tabs have their (layout) centre left of the grabbed tab's visual centre.
    private int ComputeDropIndex(double draggedCentre)
    {
        var t = 0;
        for (var i = 0; i < _strip.ItemCount; i++)
        {
            if (i != _dragFromIndex && _strip.ContainerFromIndex(i) is Control c && c.Bounds.Center.X < draggedCentre)
                t++;
        }
        return t;
    }

    // Clear every float/slide transform, then commit the reorder once (containers recreate here, not mid-drag).
    private void EndDrag(bool commit)
    {
        if (_dragItem is null)
            return;

        for (var i = 0; i < _strip.ItemCount; i++)
        {
            if (_strip.ContainerFromIndex(i) is Control c)
            {
                c.RenderTransform = null;
                c.ZIndex = 0;
            }
        }

        var item = _dragItem;
        var target = _dragTargetIndex;
        var from = _dragFromIndex;
        var dragged = _dragging;
        _dragItem = null;
        _dragPanel = null;
        _dragging = false;

        if (commit && dragged && target != from)
            _commitMove(item, target);
    }

    private static ListBoxItem? ItemFrom(object? source)
        => source as ListBoxItem
           ?? (source as Visual)?.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
}
