using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Tittle.Features.Shell;

/// <summary>
/// Horizontal tab layout that shrinks tabs to share the available width (Chrome/Firefox-style) instead of
/// letting them overflow into a horizontal scrollbar. With few tabs each keeps its natural width, left-packed.
/// As tabs are added they shrink equally down to <see cref="MinTabWidth"/> so they stay legible; once even
/// that no longer fits, the strip scrolls sideways on the mouse wheel (a partially-clipped edge tab is the
/// "more tabs" affordance) — no scrollbar ever appears. Used as the tab-strip ListBox's ItemsPanel;
/// non-virtualizing (every container is realized), which the drag-reorder hit-testing relies on.
/// </summary>
public sealed class TabsPanel : Panel
{
    /// <summary>Smallest a tab shrinks to before the strip scrolls instead — keeps the icon + a few
    /// characters + the close button legible.</summary>
    private const double MinTabWidth = 104;

    private double[] _widths = Array.Empty<double>();
    private double _contentWidth;   // total width of all tabs (may exceed the viewport → scroll)
    private double _offset;         // horizontal scroll offset when the tabs overflow

    public TabsPanel() => ClipToBounds = true; // clip scrolled-out / squeezed tabs at the strip edge

    protected override Size MeasureOverride(Size availableSize)
    {
        var n = Children.Count;
        if (n == 0)
        {
            _widths = Array.Empty<double>();
            _contentWidth = 0;
            return default;
        }

        // Natural widths (unconstrained in X) + the tallest child for the strip height.
        var naturalTotal = 0d;
        var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            naturalTotal += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        _widths = new double[n];
        if (double.IsInfinity(availableSize.Width) || naturalTotal <= availableSize.Width)
        {
            // Everything fits (or width is unconstrained): keep each tab's natural width.
            for (var i = 0; i < n; i++)
                _widths[i] = Children[i].DesiredSize.Width;
            _contentWidth = naturalTotal;
        }
        else
        {
            // Crowded: shrink every tab to an equal share, but not below MinTabWidth — beyond that the strip
            // scrolls instead of squeezing tabs into illegibility. Re-measure at the slot so headers ellipsize.
            var share = Math.Max(MinTabWidth, availableSize.Width / n);
            for (var i = 0; i < n; i++)
            {
                _widths[i] = share;
                Children[i].Measure(new Size(share, availableSize.Height));
            }
            _contentWidth = share * n;
        }

        // Fit within the viewport in X (overflow is reached by scrolling, not by growing the strip).
        var width = double.IsInfinity(availableSize.Width) ? _contentWidth : Math.Min(_contentWidth, availableSize.Width);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Clamp the scroll offset to the current overflow (it shrinks to 0 once the tabs fit again).
        var maxOffset = Math.Max(0, _contentWidth - finalSize.Width);
        _offset = Math.Clamp(_offset, 0, maxOffset);

        var x = -_offset;
        for (var i = 0; i < Children.Count; i++)
        {
            var w = i < _widths.Length ? _widths[i] : Children[i].DesiredSize.Width;
            Children[i].Arrange(new Rect(x, 0, w, finalSize.Height));
            x += w;
        }
        return finalSize;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_contentWidth <= Bounds.Width + 0.5)
            return; // everything fits — let the wheel bubble (e.g. to the document)

        var delta = e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X;
        var maxOffset = _contentWidth - Bounds.Width;
        var next = Math.Clamp(_offset - delta * 48, 0, maxOffset);
        if (Math.Abs(next - _offset) > 0.01)
        {
            _offset = next;
            InvalidateArrange();
        }
        e.Handled = true;
    }
}
