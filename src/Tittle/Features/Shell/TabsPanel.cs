using System;
using Avalonia;
using Avalonia.Controls;

namespace Tittle.Features.Shell;

/// <summary>
/// Horizontal tab layout that shrinks tabs to share the available width (Chrome/Firefox-style) instead of
/// letting them overflow into a horizontal scrollbar. With few tabs each keeps its natural width, left-packed;
/// once their combined natural width exceeds the strip every tab gets an equal share, so the tabs ALWAYS fit
/// and no scrollbar ever appears. Used as the tab-strip ListBox's ItemsPanel — non-virtualizing (every
/// container is realized), which the drag-reorder hit-testing relies on.
/// </summary>
public sealed class TabsPanel : Panel
{
    private double[] _widths = Array.Empty<double>();

    protected override Size MeasureOverride(Size availableSize)
    {
        var n = Children.Count;
        if (n == 0)
            return default;

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
        double totalWidth;
        if (double.IsInfinity(availableSize.Width) || naturalTotal <= availableSize.Width)
        {
            // Everything fits (or width is unconstrained): keep each tab's natural width.
            for (var i = 0; i < n; i++)
                _widths[i] = Children[i].DesiredSize.Width;
            totalWidth = naturalTotal;
        }
        else
        {
            // Crowded: every tab gets an equal share so the whole strip always fits (Chrome-style shrink).
            // Re-measure each at its share so the header ellipsizes to the narrower slot; the container clips
            // (VsCodeTab sets ClipToBounds) if a share is narrower than the content's hard minimum.
            var share = availableSize.Width / n;
            for (var i = 0; i < n; i++)
            {
                _widths[i] = share;
                Children[i].Measure(new Size(share, availableSize.Height));
            }
            totalWidth = availableSize.Width;
        }

        return new Size(totalWidth, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0d;
        for (var i = 0; i < Children.Count; i++)
        {
            var w = i < _widths.Length ? _widths[i] : Children[i].DesiredSize.Width;
            Children[i].Arrange(new Rect(x, 0, w, finalSize.Height));
            x += w;
        }
        return finalSize;
    }
}
