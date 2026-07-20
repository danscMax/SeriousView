using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Tittle.Core.Export;
using Tittle.Platform;
using Xunit;

namespace Tittle.Tests.Platform;

public sealed class ClipboardServiceTests
{
    [Fact]
    public void CreateHtmlTransfer_IncludesPlainTextAndNativeRichText()
    {
        const string html = "<html><body><strong>Rich</strong></body></html>";
        const string plainText = "Rich";

        using var transfer = ClipboardService.CreateHtmlTransfer(html, plainText);

        Assert.Single(transfer.Items);
        Assert.Equal(plainText, transfer.TryGetText());

        if (OperatingSystem.IsWindows())
        {
            var format = DataFormat.CreateBytesPlatformFormat("HTML Format");
            Assert.Equal(ClipboardHtml.BuildCfHtml(html), transfer.TryGetValue(format));
        }
        else if (OperatingSystem.IsMacOS())
        {
            var format = DataFormat.CreateStringPlatformFormat("public.html");
            Assert.Equal(html, transfer.TryGetValue(format));
        }
        else
        {
            var format = DataFormat.CreateStringPlatformFormat("text/html");
            Assert.Equal(html, transfer.TryGetValue(format));
        }
    }

    [AvaloniaFact]
    public async Task TryReadImagePng_NoImageOnClipboard_ReturnsNull_NoThrow()
    {
        // Verifies the read path's degradation: an empty/text clipboard → null (paste falls back to text),
        // and the try/catch guard means a clipboard read never throws. The positive path (a real screenshot
        // → PNG bytes) needs a live system clipboard image and is a hand-off (user) smoke.
        var window = new Window();
        window.Show();
        await window.Clipboard!.SetTextAsync("plain text, not an image");

        var png = await new ClipboardService(() => window).TryReadImagePngAsync();

        Assert.Null(png);
    }
}
