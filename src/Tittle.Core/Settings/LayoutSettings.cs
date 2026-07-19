namespace Tittle.Core.Settings;

/// <summary>Where the application menu lives in the shell chrome.</summary>
public enum MenuPlacement
{
    /// <summary>Hidden behind a ☰ button (the M7.5 default — quietest header).</summary>
    Hidden,

    /// <summary>Classic horizontal menu bar under the title.</summary>
    Bar,

    /// <summary>Compact menu hosted inside the native title bar.</summary>
    TitleBar,
}

/// <summary>How (and whether) the contextual editor toolbar is shown.</summary>
public enum ToolbarMode
{
    /// <summary>No toolbar.</summary>
    Off,

    /// <summary>Thin icon row shown only in Source mode (the default).</summary>
    Contextual,

    /// <summary>Always-on Notepad++-style toolbar.</summary>
    Fixed,
}

/// <summary>The contextual pane selected in the workspace sidebar.</summary>
public enum WorkspaceSection
{
    /// <summary>Open and recent files.</summary>
    Files,

    /// <summary>The active document's heading/symbol outline.</summary>
    Outline,

    /// <summary>Bookmarked headings in the active document.</summary>
    Bookmarks,
}

/// <summary>Reading-column width preset for the markdown preview (ported reading presets).</summary>
public enum ReadingWidth
{
    /// <summary>Full window width.</summary>
    Full,

    /// <summary>A comfortable centered column (~760 px) — the default: more readable, and resizing
    /// past the cap doesn't re-wrap the document (so it stays smooth, unlike Full).</summary>
    Comfort,

    /// <summary>A narrow book-like column (~620 px).</summary>
    Narrow,
}

/// <summary>Text density (line spacing) of the markdown preview — how airy the prose reads.
/// A preset; picking one writes concrete numbers into the fine-grain spacing fields
/// (<see cref="LayoutSettings.LineSpacing"/> / <see cref="LayoutSettings.ParagraphSpacing"/>).</summary>
public enum ReadingDensity
{
    /// <summary>Tight line spacing — more text on screen.</summary>
    Compact,

    /// <summary>The default — comfortable spacing.</summary>
    Normal,

    /// <summary>Roomy, airy line spacing — easiest to scan.</summary>
    Relaxed,
}

/// <summary>Preview body font family (ported <c>state.fontFamily</c>). Core-side enum; the view maps it
/// to a real Avalonia <c>FontFamily</c> (see <c>Shared/PreviewFonts</c>).</summary>
public enum ReadingFont
{
    /// <summary>Bundled Inter sans-serif — the default (the variable-font bold fix pins static Inter).</summary>
    Sans,

    /// <summary>A serif face (book-like) — system Georgia/Cambria/Times fallback.</summary>
    Serif,

    /// <summary>A monospace face — Cascadia Code/Consolas fallback.</summary>
    Mono,
}

/// <summary>How the preview's prose is justified. Core-side twin of Avalonia's TextAlignment (Core has no
/// Avalonia dependency); mapped to the real property in the view.</summary>
public enum TextAlign
{
    /// <summary>Ragged right — the default.</summary>
    Left,

    /// <summary>Even both edges (book-like). NOT exposed in settings — ColorTextBlock.Avalonia mis-lays-out
    /// justified wrapped lines; kept for the day the renderer supports it.</summary>
    Justify,

    /// <summary>Centred.</summary>
    Center,
}

/// <summary>Orientation of the split view (source + preview shown together).</summary>
public enum SplitOrientation
{
    /// <summary>Side by side: source left, preview right (the default — wide screens).</summary>
    Horizontal,

    /// <summary>Stacked: source on top, preview below.</summary>
    Vertical,
}

/// <summary>
/// Shell layout / chrome customization. Most chrome is driven by these knobs rather than being
/// hard-coded. NB: <see cref="MenuPlacement"/>'s <c>Bar</c>/<c>TitleBar</c> values are reserved for a
/// future menu-placement preset and are NOT yet rendered — only <c>Hidden</c> is wired (the ☰ default).
/// Null on <see cref="AppSettings"/> means "all defaults" (the etalon layout). Init-properties (not
/// positional) so new knobs can be added in later milestones without breaking call sites or the
/// persisted JSON shape.
/// </summary>
public sealed record LayoutSettings
{
    /// <summary>Where the menu lives. Default: hidden behind ☰. (Bar/TitleBar reserved, not yet built.)</summary>
    public MenuPlacement MenuPlacement { get; init; } = MenuPlacement.Hidden;

    /// <summary>Editor toolbar mode. Default: contextual (Source mode only).</summary>
    public ToolbarMode ToolbarMode { get; init; } = ToolbarMode.Contextual;

    /// <summary>Show the omnibar (path · 📂 · ⌘). Default: on.</summary>
    public bool ShowOmnibar { get; init; } = true;

    /// <summary>Whether the contextual workspace sidebar is expanded. Default: open.</summary>
    public bool IsWorkspaceSidebarOpen { get; init; } = true;

    /// <summary>The section shown in the contextual workspace sidebar. Default: files.</summary>
    public WorkspaceSection WorkspaceSection { get; init; } = WorkspaceSection.Files;

    /// <summary>Width of the outline/TOC sidebar in pixels (user-resizable via a splitter).
    /// Default: 240.</summary>
    public double OutlineWidth { get; init; } = 240;

    /// <summary>Reading-column preset for the preview. Default: comfortable centered column.</summary>
    public ReadingWidth ReadingWidth { get; init; } = ReadingWidth.Comfort;

    /// <summary>Text density (line spacing) of the preview. Default: comfortable.</summary>
    public ReadingDensity ReadingDensity { get; init; } = ReadingDensity.Normal;

    /// <summary>Extra px between wrapped lines inside a block (the fine-grain twin of the density preset).
    /// Default: the owner-tuned reading etalon (5).</summary>
    public double LineSpacing { get; init; } = 5;

    /// <summary>Px gap between top-level blocks (paragraphs, lists, tables …). Default: the etalon (20 — a
    /// clear VS-Code-style gap between paragraphs).</summary>
    public double ParagraphSpacing { get; init; } = 20;

    /// <summary>Multiplier on section-heading font sizes (their size relative to body text). 1.0 = the
    /// renderer's own sizes; default 0.80 keeps headings restrained next to the body.</summary>
    public double HeadingScale { get; init; } = 0.80;

    /// <summary>How the preview's prose is justified. Default: left (ragged right).</summary>
    public TextAlign TextAlignment { get; init; } = TextAlign.Left;

    /// <summary>Preview body font family (ported). Default: bundled Inter sans.</summary>
    public ReadingFont FontFamily { get; init; } = ReadingFont.Sans;

    /// <summary>Number the preview's headings hierarchically (1, 1.1, …). Ported. Default: off.</summary>
    public bool NumberHeadings { get; init; }

    /// <summary>User-chosen accent colour as <c>#RRGGBB</c>, overriding the theme's accent everywhere.
    /// Empty (the default) = use each theme's own accent. Ported <c>state.accentColor</c>.</summary>
    public string AccentColor { get; init; } = "";

    /// <summary>Orientation of the split view (source + preview together). Default: horizontal.</summary>
    public SplitOrientation SplitOrientation { get; init; } = SplitOrientation.Horizontal;

    /// <summary>Source-pane fraction (0..1) of the split view, user-resizable via a splitter.
    /// Default: 0.5 (even). Clamped to [0.15, 0.85] on use.</summary>
    public double SplitRatio { get; init; } = 0.5;
}
