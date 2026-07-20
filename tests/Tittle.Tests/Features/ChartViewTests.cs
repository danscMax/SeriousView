using Avalonia.Headless.XUnit;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Tittle.Core.Text;
using Tittle.Features.Viewer.Charts;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>The ChartSpec → LiveCharts2 control path instantiates for every chart family without throwing
/// (visual rendering itself is confirmed in the Layer-3 preview pass).</summary>
public class ChartViewTests
{
    private static ChartSpec Spec(ChartKind kind) =>
        new(kind, new[] { "a", "b", "c" }, new[]
        {
            new ChartSeries("S1", new[] { 1d, 2d, 3d }, "#3B82F6"),
            new ChartSeries("S2", new[] { 3d, 2d, 1d }, null),
        });

    [AvaloniaTheory]
    [InlineData(ChartKind.Bar)]
    [InlineData(ChartKind.Line)]
    [InlineData(ChartKind.Area)]
    [InlineData(ChartKind.Scatter)]
    public void Build_CartesianKinds_ProduceACartesianChart(ChartKind kind)
    {
        var control = ChartView.Build(Spec(kind));
        Assert.IsType<CartesianChart>(control);
        Assert.Equal(320, control.Height);
    }

    [AvaloniaTheory]
    [InlineData(ChartKind.Pie)]
    [InlineData(ChartKind.Doughnut)]
    public void Build_PieKinds_ProduceAPieChart(ChartKind kind)
    {
        var control = ChartView.Build(Spec(kind));
        Assert.IsType<PieChart>(control);
    }
}
