using System;
using System.Linq;

namespace Tittle.Core.Text;

/// <summary>The formatting actions the editor toolbar offers. Inline ops wrap the selection; the last
/// three are line-prefix ops. No alignment op — markdown has no paragraph-alignment syntax.</summary>
public enum MarkdownFormatKind
{
    Bold,
    Italic,
    Code,
    Link,
    Heading,
    Quote,
    BulletList,
}

/// <summary>Markdown-syntax formatting for the editor toolbar (bold/italic/code/link, and the line-prefix
/// ops heading/quote/bullet). Pure: given the document text and the current selection it returns the edit
/// to apply — WHAT to replace and where the selection should land after — so the dispatcher (and macros)
/// stay UI-free. Markdown has no paragraph-ALIGNMENT syntax, so there is deliberately no alignment op.</summary>
public static class MarkdownFormatting
{
    /// <summary>An edit: replace <see cref="Length"/> chars at <see cref="Start"/> with <see cref="NewText"/>,
    /// then set the selection to (<see cref="SelectionStart"/>, <see cref="SelectionLength"/>) — a length of 0
    /// is a bare caret.</summary>
    public readonly record struct Edit(int Start, int Length, string NewText, int SelectionStart, int SelectionLength);

    public static Edit Apply(string text, int selStart, int selLen, MarkdownFormatKind kind)
    {
        selStart = Math.Clamp(selStart, 0, text.Length);
        selLen = Math.Clamp(selLen, 0, text.Length - selStart);

        return kind switch
        {
            MarkdownFormatKind.Bold => Inline(text, selStart, selLen, "**", "жирный"),
            MarkdownFormatKind.Italic => Inline(text, selStart, selLen, "*", "курсив"),
            MarkdownFormatKind.Code => Inline(text, selStart, selLen, "`", "код"),
            MarkdownFormatKind.Link => Link(text, selStart, selLen),
            MarkdownFormatKind.Heading => Heading(text, selStart, selLen),
            MarkdownFormatKind.Quote => LinePrefix(text, selStart, selLen, "> "),
            MarkdownFormatKind.BulletList => LinePrefix(text, selStart, selLen, "- "),
            _ => new Edit(selStart, selLen, text.Substring(selStart, selLen), selStart, selLen),
        };
    }

    // Wrap the selection in a paired inline marker; with no selection insert markers around a placeholder
    // word and select it, so the user types over it (the VS-Code / Typora behaviour).
    private static Edit Inline(string text, int s, int len, string marker, string placeholder)
    {
        var body = len > 0 ? text.Substring(s, len) : placeholder;
        var newText = marker + body + marker;
        return new Edit(s, len, newText, s + marker.Length, body.Length);
    }

    private static Edit Link(string text, int s, int len)
    {
        var label = len > 0 ? text.Substring(s, len) : "текст";
        var newText = "[" + label + "](url)";
        // Select the "url" placeholder so the user fills it in immediately.
        var urlStart = s + 1 + label.Length + 2; // past "](" (the '[' + label + "](")
        return new Edit(s, len, newText, urlStart, 3);
    }

    // Increase the heading level of the line the caret sits on: no #s → "# "; 1..5 #s → add one; 6 → no-op.
    private static Edit Heading(string text, int s, int len)
    {
        var lineStart = LineStart(text, s);
        var hashes = 0;
        while (lineStart + hashes < text.Length && text[lineStart + hashes] == '#')
            hashes++;

        if (hashes == 0)
            return new Edit(lineStart, 0, "# ", s + 2, len);   // insert "# " before the line, shift caret
        if (hashes < 6)
            return new Edit(lineStart, 0, "#", s + 1, len);    // deepen by one level
        return new Edit(s, len, text.Substring(s, len), s, len); // already H6 — no change
    }

    // Prefix every whole line the selection touches (quote / bullet). The new block stays selected.
    private static Edit LinePrefix(string text, int s, int len, string prefix)
    {
        var (blockStart, blockLen) = SelectedLinesSpan(text, s, len);
        var block = text.Substring(blockStart, blockLen);
        var prefixed = string.Join("\n", block.Split('\n').Select(line => prefix + line));
        return new Edit(blockStart, blockLen, prefixed, blockStart, prefixed.Length);
    }

    private static int LineStart(string text, int offset)
        => offset <= 0 ? 0 : text.LastIndexOf('\n', Math.Min(offset, text.Length) - 1) + 1;

    // Char span covering every whole line the selection touches (mirrors the dispatcher's line-op span).
    private static (int Start, int Length) SelectedLinesSpan(string text, int selStart, int selLen)
    {
        var s = Math.Clamp(selStart, 0, text.Length);
        var e = Math.Clamp(selStart + Math.Max(0, selLen), s, text.Length);
        var lineStart = s == 0 ? 0 : text.LastIndexOf('\n', s - 1) + 1;
        var probe = selLen > 0 ? Math.Max(s, e - 1) : e;
        var nl = probe < text.Length ? text.IndexOf('\n', probe) : -1;
        var lineEnd = nl < 0 ? text.Length : nl;
        return (lineStart, lineEnd - lineStart);
    }
}
