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
}
