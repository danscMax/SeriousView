# New Tittle — Obsidian-inspired shell design

**Date:** 2026-07-14  
**Status:** Approved for implementation

## Goal

Replace the current Tittle shell with a calm, modern, document-first desktop UI inspired by
Obsidian's information architecture. Preserve the tested document engine and every shipped feature.
The result must feel like a new application, not a reskin of the existing window.

The original HTML viewer remains the parity oracle for Markdown fidelity. The existing Tittle Core,
document viewers, services, commands, persistence, and tests remain the implementation foundation.

## Product principles

1. **The document owns the window.** Chrome stays visually quiet and gives most space to content.
2. **Progressive disclosure.** Search, outline, bookmarks, recent files, and tools appear when needed.
3. **One clear home for each action.** Avoid duplicated controls across the title bar, toolbar, and
   status bar.
4. **Keyboard and mouse are peers.** Existing shortcuts and the command palette remain first-class.
5. **Native and offline.** No WebView is introduced; all current privacy and local-file behavior stay.
6. **No feature regression.** Existing commands remain reachable while the shell is replaced.

## Window structure

The new window has four persistent regions:

- **Workspace rail (44 px):** Open, Search, Outline, Bookmarks, and Settings. The active item uses the
  accent color and a subtle filled background.
- **Context sidebar (240–420 px, collapsible/resizable):** Content depends on the selected rail item.
  The outline is no longer permanently visible. The sidebar remembers its width and open state.
- **Document workspace:** Compact tabs on top and the active document below. Existing tab keep-alive
  behavior remains unchanged.
- **Status strip (24 px):** Only document state: encoding/EOL, cursor or page position, zoom, view mode,
  external-change/unsaved state. Secondary controls move into menus or contextual toolbars.

The Windows caption buttons share the top row with the workspace header. There are no stacked title,
omnibar, breadcrumb, and toolbar bands. Breadcrumbs become a compact in-document overlay/header that is
shown only when useful.

## Visual language

- Graphite neutral surfaces with low-contrast separators instead of boxed panels.
- Purple accent by default, mapped through the existing semantic theme tokens.
- 6/10/14 px spacing rhythm and 6 px control radius; no oversized cards or glossy effects.
- Bundled Inter for UI and reading text; the existing monospace font remains for source/code.
- Compact icon-only rail actions with labels in tooltips and accessible automation names.
- Hover, selected, focused, dirty, and error states are visually distinct without relying on color
  alone.
- Existing 14 themes remain supported. Each theme may override surfaces and accent, while layout and
  contrast rules stay identical.

This is inspired by Obsidian's calm workspace model, not a pixel-for-pixel clone. Tittle retains its
own iconography, terminology, theme catalog, and document tools.

## Main workflows

### Launch and welcome

- Launch with a file opens it immediately.
- Double-clicking `.md` or `.markdown` opens Tittle; a second file opens in a new tab in the existing
  instance.
- Launch without a file restores the previous session. If there is no session, the workspace shows a
  minimal welcome page with Open, recent files, and a drop target.

### Reading and editing

- Markdown opens in Reading mode by default.
- Reading, Source, and Split are one compact segmented control in the document header/status area.
- Search opens as a document-local floating bar without shifting unrelated chrome.
- Outline navigation opens the sidebar and keeps active-heading tracking, bookmarks, and unread marks.
- Source-only tools appear in a single contextual editor toolbar.

### Other formats

- PDF and images reuse the same shell and tabs, with format-specific zoom/page actions.
- CSV/config data receives a full-width table surface with a clear header and sorting affordances.
- Code and text retain minimap, outline, formatting, line operations, macros, and encoding controls.

## Architecture and migration

- Keep `MainWindowViewModel`, Core services, feature view models, and all document controls.
- Replace `MainWindow.axaml` composition and extract new shell controls under
  `Features/Shell/Workspace/`.
- Add presentation-only state for the active sidebar section. Do not duplicate document state.
- Preserve named integration points required by existing code-behind during the first migration pass,
  then reduce code-behind only where tests can cover the replacement.
- Extend existing semantic resources instead of hard-coding colors in views.
- Ship the migration in vertical slices: tokens and shell frame, workspace rail/sidebar, tabs/header,
  document/status integration, welcome/empty states, then polish.

## Error handling and compatibility

- Existing file-load InfoBars, crash logging, session recovery, and watcher behavior remain intact.
- Missing optional native PDF support keeps the existing external-open fallback.
- Layout settings migrate with safe defaults; unknown or old values never prevent startup.
- The redesign does not change document contents, saves, associations, or privacy settings.

## Verification

- Keep all existing unit and Avalonia.Headless tests green after every slice.
- Add headless tests for new sidebar state, rail commands, visibility rules, and settings migration.
- Extend `tools/HeadlessRender` with representative new-shell leaf controls in all 14 themes.
- Use the real-window Avalonia smoke driver for welcome, Markdown reading/source/split, CSV, code, PDF,
  image, tab switching, sidebar resizing, command palette, and double-click forwarding.
- Review screenshots at 100%, 125%, and 150% Windows scaling, plus narrow and wide window sizes.
- Complete a final accessibility, keyboard, visual-consistency, and feature-reachability audit before
  release.

## Explicit non-goals

- Rewriting the document engine or Core services.
- Copying Obsidian branding or plugin architecture.
- Removing advanced features merely to simplify the first screen.
- Moving to Avalonia 12 before the documented renderer ecosystem blockers are resolved.
