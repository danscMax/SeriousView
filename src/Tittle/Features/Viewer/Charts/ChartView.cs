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
        };
    }

    private static ISeries ToSeries(ChartSeries s, ChartKind kind)
    {
        var values = s.Values.ToArray();
        var color = Parse(s.Color);
        return kind switch
        {
            ChartKind.Line => new LineSeries<double>
            {
                Values = values,
                Name = s.Name,
                Fill = null,
                Stroke = color is { } lc ? new SolidColorPaint(lc, 2) : null,
                GeometryStroke = color is { } gc ? new SolidColorPaint(gc, 2) : null,
            },
            ChartKind.Area => new LineSeries<double>
            {
                Values = values,
                Name = s.Name,
                Fill = color is { } ac ? new SolidColorPaint(ac.WithAlpha(70)) : null,
            },
            ChartKind.Scatter => new ScatterSeries<double> { Values = values, Name = s.Name },
            _ => new ColumnSeries<double>
            {
                Values = values,
                Name = s.Name,
                Fill = color is { } cc ? new SolidColorPaint(cc) : null,
            },
        };
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
