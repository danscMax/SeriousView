# New Tittle Obsidian-inspired shell — implementation plan

> Execute this plan directly on `main`. Each product slice is committed and pushed separately.

**Goal:** Replace Tittle's existing multi-band shell with a document-first workspace while preserving
all current viewers, editing features, persistence, shortcuts, and single-instance file opening.

**Architecture:** Keep the current Core, `MainWindowViewModel`, `DocumentTabViewModel`, document views,
and platform services. Add a small persisted workspace-pane state, compose new leaf shell controls, and
replace only `MainWindow.axaml` layout. Retain the tab keep-alive `ItemsControl` and named controls used by
existing code-behind.

**Stack:** .NET 9, Avalonia 11.3, FluentAvalonia, CommunityToolkit.Mvvm, xUnit,
Avalonia.Headless, `tools/HeadlessRender`.

---

## Task 1: Persisted workspace navigation state

**Files:**

- Modify: `src/Tittle.Core/Settings/LayoutSettings.cs`
- Modify: `src/Tittle/Shared/LayoutOptions.cs`
- Modify: `src/Tittle/Features/Shell/MainWindowViewModel.cs`
- Modify: `tests/Tittle.Tests/Shared/LayoutOptionsTests.cs`
- Modify: `tests/Tittle.Tests/Features/MainWindowViewModelTests.cs`

**Steps:**

1. Add failing tests for the default sidebar state, serialization round-trip, invalid state fallback,
   opening another workspace section, and toggling the active section closed.
2. Add `WorkspaceSection` (`Files`, `Outline`, `Bookmarks`) plus `IsWorkspaceSidebarOpen` and
   `WorkspaceSection` to layout settings/options.
3. Add VM commands that select/toggle a section; preserve `ToggleOutlineCommand` as a compatibility path
   that selects `Outline`.
4. Raise derived visibility notifications when the active tab changes or loses an outline.
5. Run focused tests:
   `dotnet test tests/Tittle.Tests --filter "FullyQualifiedName~LayoutOptionsTests|FullyQualifiedName~MainWindowViewModelTests"`.

## Task 2: Obsidian-inspired semantic visual tokens

**Files:**

- Modify: `src/Tittle/Themes/Tokens.axaml`
- Modify: `src/Tittle/Themes/Colors/Dark.axaml`
- Modify: `src/Tittle/Themes/Colors/Light.axaml`
- Modify: `src/Tittle/Themes/Controls.axaml`
- Modify: `src/Tittle/Themes/Tabs.axaml`
- Modify: `tests/Tittle.Tests/Features/AccessibilityTests.cs`

**Steps:**

1. Add a failing resource/accessibility test for workspace rail/sidebar/header tokens and visible focus.
2. Add semantic brushes for rail, sidebar, header, elevated hover, and separators. Base custom themes on
   inherited Dark/Light tokens, so all 14 variants remain complete.
3. Change the base dark accent to a restrained purple and flatten glossy/gradient chrome.
4. Add reusable `workspace-action`, `workspace-action.active`, `header-action`, and compact segmented
   button styles with keyboard focus rings.
5. Restyle tabs as quiet rounded workspace tabs with a filled active state and no VS Code underline.
6. Run the focused accessibility test.

## Task 3: Workspace rail and sidebar leaf controls

**Files:**

- Create: `src/Tittle/Features/Shell/Workspace/WorkspaceRail.axaml`
- Create: `src/Tittle/Features/Shell/Workspace/WorkspaceRail.axaml.cs`
- Create: `src/Tittle/Features/Shell/Workspace/WorkspaceSidebar.axaml`
- Create: `src/Tittle/Features/Shell/Workspace/WorkspaceSidebar.axaml.cs`
- Create: `src/Tittle/Features/Shell/Workspace/BookmarkPanel.axaml`
- Create: `src/Tittle/Features/Shell/Workspace/BookmarkPanel.axaml.cs`
- Modify: `src/Tittle/Features/Shell/DocumentTabViewModel.cs`
- Create: `tests/Tittle.Tests/Features/WorkspaceNavigationTests.cs`

**Steps:**

1. Add failing headless tests for rail automation names, sidebar section selection, an empty outline,
   a populated outline, and bookmark filtering.
2. Expose a read-only bookmarked-outline projection on the active tab and notify it when bookmarks change.
3. Build the 44 px rail with Open/Files, Search, Outline, Bookmarks, command palette, Settings, and Help.
4. Build the contextual sidebar header, recent-files panel, existing outline host, bookmarks panel, and
   appropriate empty states.
5. Run `WorkspaceNavigationTests` plus existing `OutlinePanelTests`.

## Task 4: Replace the main window composition

**Files:**

- Modify: `src/Tittle/Features/Shell/MainWindow.axaml`
- Modify: `src/Tittle/Features/Shell/MainWindow.axaml.cs`
- Modify: `tests/Tittle.Tests/Features/AccessibilityTests.cs`
- Modify: `tests/Tittle.Tests/Features/MainWindowViewModelTests.cs`

**Steps:**

1. Add failing wiring assertions for the new rail, sidebar, header, tabs, document host, and status strip.
2. Replace the stacked DockPanel shell with a three-column workspace: rail, contextual sidebar/splitter,
   document workspace.
3. Build one 40 px title/header row containing the app menu, active path, key actions, and caption reserve.
4. Move tabs into the document workspace directly below the header. Keep `TabStrip`, `OmnibarBox`,
   `GoToLineBox`, `TitleGrid`, and `BodyGrid` integration points or update code-behind in the same slice.
5. Keep error/update InfoBars and the tab keep-alive `ItemsControl` unchanged in behavior.
6. Collapse the contextual toolbar into the document header and simplify the status strip to document
   state plus format-specific controls.
7. Run all headless tests and build Debug.

## Task 5: Welcome, empty, and non-Markdown states

**Files:**

- Modify: `src/Tittle/Features/Welcome/WelcomeView.axaml`
- Modify: `src/Tittle/Features/Welcome/WelcomeView.axaml.cs` if necessary
- Modify: CSV/PDF/image shell-facing views only where spacing or empty-state integration requires it
- Modify: `tools/HeadlessRender/Program.cs`
- Add/modify corresponding headless tests

**Steps:**

1. Add failing headless tests for the welcome actions and representative empty states.
2. Replace the old hero/card welcome page with a quiet centered document-start surface: Open, recent
   files, drag target, keyboard hint.
3. Give CSV, PDF, image, code, and notice states consistent workspace padding and header behavior.
4. Add representative leaf-control renders to the headless gallery.
5. Run focused tests and render all 14 themes.

## Task 6: Keyboard, accessibility, and responsive layout

**Files:**

- Modify: `src/Tittle/Features/Shell/MainWindow.axaml.cs`
- Modify: workspace controls and related tests

**Steps:**

1. Add tests for rail/sidebar keyboard reachability and automation labels.
2. Preserve every existing shortcut; add `Ctrl+Shift+E` for Files and keep the existing outline command
   available through the palette.
3. Ensure sidebar collapse restores document width and a narrow window hides nonessential header text.
4. Verify logical tab order, focus-visible states, tooltips, high-contrast theme, and 200% text scaling.
5. Run the full test suite.

## Task 7: File association and installer verification

**Files:**

- Inspect/modify only as needed: existing build/install scripts and packaging metadata
- Add a non-destructive association verification script/test if no current automated check exists

**Steps:**

1. Verify installed Tittle owns `.md` and `.markdown` double-click without recursive deletion of shared
   extension keys.
2. Verify a second file launch is forwarded to the existing process and opens a new tab.
3. Verify uninstall preserves a later user-selected default application.
4. Build the distributable artifact with the project's standard scripts.

## Task 8: Visual QA and final self-review

**Files:**

- Modify implementation/tests only for issues found by verification
- Record durable findings in the design/implementation docs when useful

**Steps:**

1. Run `dotnet format Tittle.sln --verify-no-changes`, `dotnet build Tittle.sln -c Release`, and
   `dotnet test Tittle.sln -c Release`.
2. Render all 14 themes and inspect every PNG for clipping, weak hierarchy, stray borders, contrast,
   spacing, and theme leakage.
3. Drive the real window through welcome, Markdown reading/source/split, search, outline, bookmarks,
   tabs, CSV, PDF, image, command palette, settings, drag/drop, and second-instance forwarding.
4. Repeat at narrow/wide sizes and 100/125/150% scaling.
5. Perform a feature-reachability audit against the command palette and old menu, a privacy/security
   review, and a code diff review. Fix every confirmed issue, rerun the relevant gate, then commit/push.
