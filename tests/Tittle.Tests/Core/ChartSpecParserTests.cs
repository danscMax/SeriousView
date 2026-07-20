using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

public class ChartSpecParserTests
{
    [Fact]
    public void ParsesChartJsJson_TypeLabelsDatasets()
    {
        var json = """
        { "type": "bar",
          "data": { "labels": ["Jan","Feb"],
                    "datasets": [ { "label": "Sales", "data": [100,120], "backgroundColor": "#3B82F6" } ] } }
        """;

        var spec = ChartSpecParser.Parse(json, null);

        Assert.NotNull(spec);
        Assert.Equal(ChartKind.Bar, spec!.Kind);
        Assert.Equal(new[] { "Jan", "Feb" }, spec.Labels);
        var s = Assert.Single(spec.Series);
        Assert.Equal("Sales", s.Name);
        Assert.Equal(new[] { 100d, 120d }, s.Values);
        Assert.Equal("#3B82F6", s.Color);
    }

    [Fact]
    public void ParsesCsv_HeadersAndRows_MultiSeries()
    {
        var csv = "Month,Sales,Costs\nJan,100,60\nFeb,120,70";

        var spec = ChartSpecParser.Parse(csv, "line");

        Assert.NotNull(spec);
        Assert.Equal(ChartKind.Line, spec!.Kind); // from the fence hint
        Assert.Equal(new[] { "Jan", "Feb" }, spec.Labels);
        Assert.Equal(2, spec.Series.Count);
        Assert.Equal("Sales", spec.Series[0].Name);
        Assert.Equal(new[] { 100d, 120d }, spec.Series[0].Values);
        Assert.Equal("Costs", spec.Series[1].Name);
        Assert.Equal(new[] { 60d, 70d }, spec.Series[1].Values);
    }

    [Fact]
    public void JsonTypeUsedWhenNoHint_HintWinsWhenBoth()
    {
        Assert.Equal(ChartKind.Pie, ChartSpecParser.Parse("""{"type":"pie","data":{"datasets":[{"data":[1,2]}]}}""", null)!.Kind);
        Assert.Equal(ChartKind.Line, ChartSpecParser.Parse("""{"type":"pie","data":{"datasets":[{"data":[1,2]}]}}""", "line")!.Kind);
    }

    [Fact]
    public void GarbageOrEmpty_ReturnsNull()
    {
        Assert.Null(ChartSpecParser.Parse("", null));
        Assert.Null(ChartSpecParser.Parse("   ", null));
        Assert.Null(ChartSpecParser.Parse("{ not json", null));
        Assert.Null(ChartSpecParser.Parse("{ \"data\": {} }", null)); // no datasets → null
        Assert.Null(ChartSpecParser.Parse("OnlyHeader", null));        // CSV needs >= 2 rows
    }

    [Fact]
    public void Csv_NonNumericCellBecomesZero_NotACrash()
    {
        var spec = ChartSpecParser.Parse("X,Y\na,foo\nb,5", null);
        Assert.NotNull(spec);
        Assert.Equal(new[] { 0d, 5d }, spec!.Series[0].Values);
    }

    [Fact]
    public void JsonColour_KeepsHex_IgnoresRgbaAndNamed()
    {
        Assert.Equal("#EF4444", ChartSpecParser.Parse("""{"data":{"datasets":[{"data":[1],"backgroundColor":"#EF4444"}]}}""", null)!.Series[0].Color);
        Assert.Null(ChartSpecParser.Parse("""{"data":{"datasets":[{"data":[1],"backgroundColor":"rgba(1,2,3,0.5)"}]}}""", null)!.Series[0].Color);
    }
}
