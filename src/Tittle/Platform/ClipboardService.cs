using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Tittle.Core.Abstractions;

namespace Tittle.Platform;

/// <summary>
/// <see cref="IClipboardService"/> backed by Avalonia's <c>TopLevel.Clipboard</c>. The TopLevel is
/// resolved lazily through the same <see cref="Func{TResult}"/> the file picker uses, so it is available
/// after the window is shown. A null clipboard (no window yet / headless) makes the call a no-op.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private readonly Func<TopLevel?> _topLevel;

    public ClipboardService(Func<TopLevel?> topLevel) => _topLevel = topLevel;

    public async Task SetTextAsync(string text)
    {
        var clipboard = _topLevel()?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(text);
    }

    public async Task SetHtmlAsync(string html, string plainText)
    {
        var clipboard = _topLevel()?.Clipboard;
        if (clipboard is null)
            return;

        var data = CreateHtmlTransfer(html, plainText);
        await clipboard.SetDataAsync(data);
    }

    internal static DataTransfer CreateHtmlTransfer(string html, string plainText)
    {
        // Avalonia 11.3's DataTransfer keeps the clipboard contract strongly typed and
        // works for both synchronous and asynchronous platform clipboard backends.
        var data = new DataTransfer();
        var item = new DataTransferItem();
        item.SetText(plainText);
        if (OperatingSystem.IsWindows())
            // Windows pastes rich text from the CF_HTML envelope (byte offsets, UTF-8 payload).
            item.Set(
                DataFormat.CreateBytesPlatformFormat("HTML Format"),
                Core.Export.ClipboardHtml.BuildCfHtml(html));
        else if (OperatingSystem.IsMacOS())
            // macOS platform formats use Uniform Type Identifiers rather than MIME names.
            item.Set(DataFormat.CreateStringPlatformFormat("public.html"), html);
        else
            item.Set(DataFormat.CreateStringPlatformFormat("text/html"), html);

        data.Add(item);
        return data;
    }
}
