namespace Tittle.Core.Abstractions;

/// <summary>
/// Writes text to the system clipboard. The implementation (Avalonia's <c>TopLevel.Clipboard</c>) lives
/// in the UI layer; Core only knows this contract. Used by the tab context menu's copy-path / copy-name.
/// </summary>
public interface IClipboardService
{
    /// <summary>Put <paramref name="text"/> on the clipboard. A no-op when no clipboard is available.</summary>
    Task SetTextAsync(string text);

    /// <summary>Put rich HTML on the clipboard with a plain-text fallback (ported
    /// copy-as-rich-text): paste targets that understand HTML (Word, mail) take the formatted
    /// document, everything else takes <paramref name="plainText"/>. A no-op without a clipboard.</summary>
    Task SetHtmlAsync(string html, string plainText);

    /// <summary>Read an image from the clipboard as PNG bytes (ported paste-image), or null when there is
    /// no image / no clipboard. Enables pasting a screenshot into the source editor as a data-URI.</summary>
    Task<byte[]?> TryReadImagePngAsync();
}
