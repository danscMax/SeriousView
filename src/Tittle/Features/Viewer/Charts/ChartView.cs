using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Tittle.Core.Text;

namespace Tittle.Features.Viewer.Charts;

/// <summary>Builds a native LiveCharts2 control from a parsed <see cref="ChartSpec"/> (ported ```chart).
/// Cartesian kinds (bar/line/area/scatter) → a <see cref="CartesianChart"/>; pie/doughnut → a
/// <see cref="PieChart"/>. Axis/legend text follows the chrome foreground token so it reads in both
/// themes (the same approach as MathView.TextColor).</summary>
public static class ChartView
{
    public static Control Build(ChartSpec spec)
    {
        var text = ChromeTextPaint();
        Control control = spec.Kind is ChartKind.Pie or ChartKind.Doughnut
            ? BuildPie(spec, text)
            : BuildCartesian(spec, text);
        control.Height = 320;
        control.Margin = new Thickness(0, 4, 0, 4);
        return control;
    }

    private static PieChart BuildPie(ChartSpec spec, SolidColorPaint text)
    {
        var series = spec.Series.FirstOrDefault();
        var values = series?.Values ?? Array.Empty<double>();
        var inner = spec.Kind == ChartKind.Doughnut ? 60d : 0d;

        var slices = values.Select((v, i) => (ISeries)new PieSeries<double>
        {
            Values = new[] { v },
            Name = i < spec.Labels.Count ? spec.Labels[i] : $"#{i + 1}",
            InnerRadius = inner,
        }).ToArray();

        return new PieChart
        {
            Series = slices,
            LegendPosition = LegendPosition.Right,
            LegendTextPaint = text,
            // A document chart is static: draw the final state at once (no entry animation → no flash of
            // an empty chart, and it renders deterministically).
            AnimationsSpeed = TimeSpan.Zero,
            EasingFunction = null,
        };
    }

    private static CartesianChart BuildCartesian(ChartSpec spec, SolidColorPaint text)
    {
        var series = spec.Series.Select(s => ToSeries(s, spec.Kind)).ToArray();
        return new CartesianChart
        {
            Series = series,
            XAxes = new[] { new Axis { Labels = spec.Labels.ToArray(), LabelsPaint = text } },
            YAxes = new[] { new Axis { LabelsPaint = text } },
            LegendPosition = spec.Series.Count > 1 ? LegendPosition.Top : LegendPosition.Hidden,
            LegendTextPaint = text,
            // Static document chart: draw the final state immediately (no entry animation → no empty-chart
            // flash, deterministic render).
            AnimationsSpeed = TimeSpan.Zero,
            EasingFunction = null,
        };
    }

    private static ISeries ToSeries(ChartSeries s, ChartKind kind)
    {
        // Only assign a paint when the source gave an explicit colour. Leaving a paint UNSET lets
        // LiveCharts2 apply its per-series theme palette; setting Fill = null instead paints nothing
        // (transparent columns / no line) — the bug that made colour-less CSV charts render empty.
        var values = s.Values.ToArray();
        var color = Parse(s.Color);
        switch (kind)
        {
            case ChartKind.Line:
            {
                var line = new LineSeries<double> { Values = values, Name = s.Name, Fill = null }; // no area under the line
                if (color is { } lc)
                {
                    line.Stroke = new SolidColorPaint(lc, 2);
                    line.GeometryStroke = new SolidColorPaint(lc, 2);
                }
                return line;
            }
            case ChartKind.Area:
            {
                var area = new LineSeries<double> { Values = values, Name = s.Name };
                if (color is { } ac)
                    area.Fill = new SolidColorPaint(ac.WithAlpha(70));
                return area;
            }
            case ChartKind.Scatter:
                return new ScatterSeries<double> { Values = values, Name = s.Name };
            default:
            {
                var col = new ColumnSeries<double> { Values = values, Name = s.Name };
                if (color is { } cc)
                    col.Fill = new SolidColorPaint(cc);
                return col;
            }
        }
    }

    private static SKColor? Parse(string? hex)
    {
        if (hex is not null && Color.TryParse(hex, out var c))
            return new SKColor(c.R, c.G, c.B, c.A);
        return null;
    }

    // Axis/legend text uses the chrome foreground token so it stays legible in every theme.
    private static SolidColorPaint ChromeTextPaint()
    {
        var color = new SKColor(0x88, 0x88, 0x88);
        if (Application.Current?.TryFindResource("ChromeForegroundBrush", out var res) == true
            && res is ISolidColorBrush b)
            color = new SKColor(b.Color.R, b.Color.G, b.Color.B, b.Color.A);
        return new SolidColorPaint(color);
    }
}
