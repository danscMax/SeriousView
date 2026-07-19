namespace Tittle.Core.Services;

/// <summary>
/// Live "private mode" flag, shared (the holder-is-the-seam idiom, like the session holder) between the
/// settings toggle and the persistence stores that must NOT record history while it is on. Ported from
/// the original viewer's <c>state.privateMode</c> / <c>PRIVATE_SKIP_RE = /^md-(session|recents|visited)/</c>:
/// private mode suppresses <b>session + recent-files + visited marks</b>, but deliberately KEEPS
/// <b>settings and bookmarks</b> (a bookmark is an intentional user action, not passive history).
/// Mutable and read at each write point (not observable) — the gate is evaluated when Add/MarkVisited/
/// session-save is called, which always happens after this singleton is constructed.
/// </summary>
public sealed class PrivacyState
{
    /// <summary>When true, passive history writes are skipped. Initialized from the persisted
    /// <c>AppSettings.PrivateMode</c> at startup and flipped live by the settings toggle.</summary>
    public bool IsPrivate { get; set; }
}
