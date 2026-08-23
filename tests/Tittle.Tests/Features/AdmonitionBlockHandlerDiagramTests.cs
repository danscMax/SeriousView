using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MdEngine = Markdown.Avalonia.Markdown;
using SkiaSharp;
using Tittle.Core.Services;
using Tittle.Features.Viewer;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>Diagram render honesty: identical diagrams share ONE underlying fetch (single-flight,
/// sequential and concurrent), a failed fetch is retried on the next render instead of staying
/// cached, and the decoded image is built once and shared by every rebuilt control. Uses the
/// internal fetch-override seam — no network, no real disk cache. Each test embeds a UNIQUE body
/// because the handler's caches are static and shared across tests.</summary>
public class AdmonitionBlockHandlerDiagramTests
{
    // A tiny valid PNG so the decode step exercises the real Bitmap path (needs Skia headless).
    private static byte[] PngBytes()
    {
        using var bmp = new SKBitmap(6, 6);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private sealed class FakeFetcher(byte[] png, bool fail = false, TaskCompletionSource? gate = null)
    {
        public int Calls;

        public Task<DiagramImage> Fetch(string url, string type, string body)
        {
            Interlocked.Increment(ref Calls);
            if (gate is not null && Calls == 1)
                return gate.Task.ContinueWith(_ => new DiagramImage(png, IsSvg: false));
            return fail
                ? Task.FromException<DiagramImage>(new HttpRequestException("boom"))
                : Task.FromResult(new DiagramImage(png, IsSvg: false));
        }
    }

    private static AdmonitionBlockHandler MakeHandler(FakeFetcher fake)
        => new(new MdEngine(), () => "https://kroki.test", fake.Fetch);

    // "type|body", both percent-encoded — exactly what the preprocessor emits.
    private static string Encoded(string type, string body)
        => Uri.EscapeDataString(type) + "|" + Uri.EscapeDataString(body);

    // The placeholder swap happens asynchronously; pump the dispatcher until it settles.
    private static void PumpUntil(Control border, Func<bool> settled)
    {
        for (var i = 0; i < 300 && !settled(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
        Assert.True(settled(), "the diagram render did not settle in time");
    }

    // The error panel (unlike the loading panel) carries the failure message text.
    private static bool HasError(Border border)
        => border.Child is Panel p && p.Children.OfType<TextBlock>()
            .Any(t => t.Text?.Contains("Не удалось отрендерить диаграмму") == true);

    private static IEnumerable<string> AllTexts(Border border)
        => border.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    [AvaloniaFact]
    public void SameDiagramRenderedTwice_UnderlyingFetchRunsOnce()
    {
        var fake = new FakeFetcher(PngBytes());
        var handler = MakeHandler(fake);
        var encoded = Encoded("mermaid", $"graph TD;s-->e;{Guid.NewGuid():N}");

        var first = handler.ProvideControl("", "diagram", encoded);
        PumpUntil(first, () => first.Child is Image);

        var second = handler.ProvideControl("", "diagram", encoded);
        PumpUntil(second, () => second.Child is Image);

        Assert.Equal(1, fake.Calls);          // the real fetch ran once…
        Assert.Equal(1, handler.FetchCount);  // …and the seam agrees with it
        Assert.Same(((Image)first.Child).Source!, ((Image)second.Child).Source!);
    }

    [AvaloniaFact]
    public void FailedFetch_RetriesOnNextRender_AndShowsError()
    {
        var fake = new FakeFetcher(PngBytes(), fail: true);
        var handler = MakeHandler(fake);
        var body = $"digraph {{ failme_{Guid.NewGuid():N} }}";
        var encoded = Encoded("dot", body);

        var first = handler.ProvideControl("", "diagram", encoded);
        PumpUntil(first, () => HasError(first));
        Assert.Equal(1, handler.FetchCount);

        // The failed flight was dropped from the cache → this render fetches again.
        var second = handler.ProvideControl("", "diagram", encoded);
        PumpUntil(second, () => HasError(second));

        Assert.Equal(2, fake.Calls);
        Assert.Equal(2, handler.FetchCount);
        var texts = string.Join("\n", AllTexts(second));
        Assert.Contains("Не удалось отрендерить диаграмму", texts);
        Assert.Contains(body, texts); // source shown so content is never lost
    }

    [AvaloniaFact]
    public void RebuiltControls_ReuseOneDecodedImage()
    {
        var fake = new FakeFetcher(PngBytes());
        var handler = MakeHandler(fake);
        var encoded = Encoded("mermaid", $"graph TD;m-->n;{Guid.NewGuid():N}");

        var first = handler.ProvideControl("", "diagram", encoded);
        PumpUntil(first, () => first.Child is Image);
        var decoded = ((Image)first.Child).Source!;

        // Two more renders (every preview reflow rebuilds controls) must not re-decode.
        var second = handler.ProvideControl("", "diagram", encoded);
        PumpUntil(second, () => second.Child is Image);
        var third = handler.ProvideControl("", "diagram", encoded);
        PumpUntil(third, () => third.Child is Image);

        Assert.Same(decoded, ((Image)second.Child).Source);
        Assert.Same(decoded, ((Image)third.Child).Source);
        Assert.Equal(1, handler.FetchCount);
    }

    [AvaloniaFact]
    public void ConcurrentRendersOfSameDiagram_JoinOneFlight()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fake = new FakeFetcher(PngBytes(), gate: gate);
        var handler = MakeHandler(fake);
        var encoded = Encoded("mermaid", $"graph TD;a-->b;{Guid.NewGuid():N}");

        var a = handler.ProvideControl("", "diagram", encoded); // opens the flight, held by the gate
        var b = handler.ProvideControl("", "diagram", encoded); // joins the SAME in-flight fetch

        // The second render awaited the pending task instead of starting its own fetch.
        Assert.Equal(1, fake.Calls);
        Assert.Equal(1, handler.FetchCount);

        gate.TrySetResult();
        PumpUntil(a, () => a.Child is Image);
        PumpUntil(b, () => b.Child is Image);

        Assert.Equal(1, fake.Calls);
        Assert.Same(((Image)a.Child).Source!, ((Image)b.Child).Source!);
    }
}
