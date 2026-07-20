using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Tittle.Core.Text;

/// <summary>Chart kind for a ```chart block (ported Chart.js types; the common ones first).</summary>
public enum ChartKind
{
    Bar,
    Line,
    Area,
    Pie,
    Doughnut,
    Scatter,
    Radar,
    PolarArea,
}

/// <summary>An (x, y) point for a scatter dataset (Chart.js <c>{x, y}</c>).</summary>
public sealed record ChartPoint(double X, double Y);

/// <param name="Name">Series/dataset label.</param>
/// <param name="Values">Y values, one per category label.</param>
/// <param name="Color">Optional #RRGGBB (from Chart.js backgroundColor/borderColor); null = auto-palette.</param>
/// <param name="Points">Real (x, y) points when the dataset is a Chart.js <c>{x, y}</c> array (scatter);
/// null for a plain-number dataset. When set, a scatter chart plots at the real X instead of the index.</param>
public sealed record ChartSeries(string Name, IReadOnlyList<double> Values, string? Color, IReadOnlyList<ChartPoint>? Points = null);

/// <summary>A parsed ```chart block — the neutral model the LiveCharts2 view is built from. Independent
/// of any charting library (Core has no Avalonia/Skia dependency).</summary>
public sealed record ChartSpec(ChartKind Kind, IReadOnlyList<string> Labels, IReadOnlyList<ChartSeries> Series);

/// <summary>Parses a ```chart body — either a Chart.js JSON config or a CSV table — into a
/// <see cref="ChartSpec"/>. Ported <c>parseChartSource</c>. Pure; returns null on an empty or
/// unrecognisable body so the caller can fall back to showing the source.</summary>
public static class ChartSpecParser
{
    /// <param name="body">The fenced block body.</param>
    /// <param name="typeHint">The type from the fence info string (```chart:line → "line"), or null.</param>
    public static ChartSpec? Parse(string? body, string? typeHint)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var trimmed = body.TrimStart();
        return trimmed.StartsWith('{')
            ? ParseJson(body, typeHint)
            : ParseCsv(body, typeHint);
    }

    /// <summary>Map a Chart.js type string to a <see cref="ChartKind"/> (default <see cref="ChartKind.Bar"/>).</summary>
    public static ChartKind KindFrom(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "line" => ChartKind.Line,
        "area" => ChartKind.Area,
        "pie" => ChartKind.Pie,
        "doughnut" or "donut" => ChartKind.Doughnut,
        "scatter" => ChartKind.Scatter,
        "radar" => ChartKind.Radar,
        "polararea" or "polar" => ChartKind.PolarArea,
        _ => ChartKind.Bar,
    };

    private static ChartSpec? ParseJson(string body, string? typeHint)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var type = typeHint;
            if (type is null && root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
                type = t.GetString();

            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                return null;

            var labels = new List<string>();
            if (data.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
                labels.AddRange(labelsEl.EnumerateArray().Select(LabelText));

            var series = new List<ChartSeries>();
            if (data.TryGetProperty("datasets", out var datasets) && datasets.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var ds in datasets.EnumerateArray())
                {
                    i++;
                    var name = ds.TryGetProperty("label", out var l) && l.ValueKind == JsonValueKind.String
                        ? l.GetString() ?? $"Ряд {i}"
                        : $"Ряд {i}";
                    var items = ds.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array
                        ? d.EnumerateArray().ToList()
                        : new List<JsonElement>();
                    var values = items.Select(NumberOf).ToList();
                    // Keep the real (x, y) points when EVERY item is an {x, y} object (Chart.js scatter).
                    var pts = items.Select(PointOf).ToList();
                    IReadOnlyList<ChartPoint>? points = pts.Count > 0 && pts.All(p => p is not null)
                        ? pts.Select(p => p!).ToList()
                        : null;
                    var color = FirstColor(ds);
                    series.Add(new ChartSeries(name, values, color, points));
                }
            }

            if (series.Count == 0)
                return null;

            return new ChartSpec(KindFrom(type), labels, series);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ChartSpec? ParseCsv(string body, string? typeHint)
    {
        var rows = body.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(l => l.Split(',').Select(c => c.Trim()).ToArray())
            .ToList();

        if (rows.Count < 2)
            return null;

        var header = rows[0];
        if (header.Length < 2)
            return null;

        // header[0] = the category column title (ignored); header[1..] = one series name each.
        var seriesCount = header.Length - 1;
        var labels = new List<string>();
        var values = new List<double>[seriesCount];
        for (var s = 0; s < seriesCount; s++)
            values[s] = new List<double>();

        foreach (var row in rows.Skip(1))
        {
            labels.Add(row.Length > 0 ? row[0] : "");
            for (var s = 0; s < seriesCount; s++)
                values[s].Add(s + 1 < row.Length && double.TryParse(row[s + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0);
        }

        var series = Enumerable.Range(0, seriesCount)
            .Select(s => new ChartSeries(header[s + 1], values[s], null))
            .ToList();

        return new ChartSpec(KindFrom(typeHint), labels, series);
    }

    private static string LabelText(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString() ?? "",
        JsonValueKind.Number => e.GetRawText(),
        _ => e.GetRawText(),
    };

    private static double NumberOf(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Number when e.TryGetDouble(out var d) => d,
        // scatter points are {x,y}: take y so a number series still renders
        JsonValueKind.Object when e.TryGetProperty("y", out var y) && y.TryGetDouble(out var yd) => yd,
        _ => 0,
    };

    // A Chart.js {x,y} point → ChartPoint; null for a plain number or an object missing a numeric x/y.
    private static ChartPoint? PointOf(JsonElement e)
        => e.ValueKind == JsonValueKind.Object
           && e.TryGetProperty("x", out var x) && x.TryGetDouble(out var xd)
           && e.TryGetProperty("y", out var y) && y.TryGetDouble(out var yd)
            ? new ChartPoint(xd, yd)
            : null;

    // Chart.js backgroundColor / borderColor may be a string or an array — take the first #hex.
    private static string? FirstColor(JsonElement dataset)
    {
        foreach (var key in new[] { "backgroundColor", "borderColor" })
        {
            if (!dataset.TryGetProperty(key, out var c))
                continue;
            if (c.ValueKind == JsonValueKind.String)
                return Normalize(c.GetString());
            if (c.ValueKind == JsonValueKind.Array)
                foreach (var item in c.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String)
                        return Normalize(item.GetString());
        }
        return null;
    }

    // Keep only #RRGGBB / #RGB values; ignore rgba()/named colours (the auto-palette covers those).
    private static string? Normalize(string? s)
        => s is not null && s.StartsWith('#') && (s.Length == 7 || s.Length == 4) ? s : null;
}
