using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Tittle;
using Tittle.Core.Abstractions;
using Tittle.Core.Documents;
using Tittle.Core.Services;
using Tittle.Core.Settings;
using Tittle.Core.Editing;
using Tittle.Core.Text;
using Tittle.Features.Donate;
using Tittle.Features.Help;
using Tittle.Features.Macros;
using Tittle.Features.Palette;
using Tittle.Features.Settings;
using Tittle.Features.Shell;
using Tittle.Features.Stats;
using Tittle.Features.Viewer;
using Tittle.Features.Welcome;
using Tittle.Platform;
using Tittle.Shared;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;

// Layer-1 render oracle: render leaf controls to PNG in every theme (the cheap, deterministic
// both-theme visual check). Output dir is arg[0] (default: plans/avalonia-smoke/screenshots).
// Add screen builders to SCREENS; never build MainWindow (chrome Symbols-font crash under Skia).

var outDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "plans", "avalonia-smoke", "screenshots");
outDir = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDir);

AppBuilder.Configure<App>()
    .UseSkia()
    .WithBundledInterFont() // the preview style references fonts:Inter#Inter — register it here too
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

// (theme name, ThemeVariant) — render every one. Base Light/Dark catch most theme regressions;
// add the custom variants (AppThemeVariants.DeepBlue, …) here if a palette needs eyes.
var themes = new (string Name, ThemeVariant Variant)[]
{
    ("dark", ThemeVariant.Dark),
    ("light", ThemeVariant.Light),
    // Custom variants ported from the original viewer (commit 3833f54) — never visually audited.
    ("midnight", AppThemeVariants.Midnight),
    ("ocean", AppThemeVariants.Ocean),
    ("deepblue", AppThemeVariants.DeepBlue),
    ("nord", AppThemeVariants.Nord),
    ("dracula", AppThemeVariants.Dracula),
    ("solarizeddark", AppThemeVariants.SolarizedDark),
    ("solarizeddim", AppThemeVariants.SolarizedDim),
    ("gruvboxdark", AppThemeVariants.GruvboxDark),
    ("highcontrast", AppThemeVariants.HighContrast),
    ("sepia", AppThemeVariants.Sepia),
    ("solarizedlight", AppThemeVariants.SolarizedLight),
    ("gruvboxlight", AppThemeVariants.GruvboxLight),
};

// (screen name, builder). Leaf controls only. Each builder is hermetic (no file/network side effects
// beyond reading a sample doc). Realistic size (>=900px wide) so nothing falsely truncates.
var sampleMd = File.ReadAllText(Path.Combine(outDir, "..", "..", "ux-audit", "rich.md"));
var screens = new (string Name, Func<Control> Build)[]
{
    ("docview", () => new DocumentView { DataContext = DocumentTabViewModel.FromFile(sampleMd, @"E:\docs\rich.md") }),
    ("welcome", () => new WelcomeView { DataContext = BuildWelcomeVm() }),
    // Block HTML → markdown: a raw <table> should render as a real table in the preview (ported).
    ("htmltable", () => new DocumentView
    {
        DataContext = DocumentTabViewModel.FromFile(
            "# HTML-таблица\n\n<table>\n<tr><th>Месяц</th><th>Продажи</th></tr>\n"
            + "<tr><td>Янв</td><td>100</td></tr>\n<tr><td>Фев</td><td>120</td></tr>\n</table>\n",
            @"E:\docs\htmltable.md"),
    }),
    // Verify the user accent recolours the chrome: setting Layout.AccentColor fires ApplyAccent, which
    // overrides the app-level AccentBrush; the settings "Готово" (accent) button should render RED. The
    // override it leaves is cleared before every screen (ClearAccentOverride in the render loop), so this
    // screen's position in the array no longer matters — no neighbour inherits the red accent.
    ("settings-accent", () =>
    {
        var vm = BuildWelcomeVm();
        vm.Layout.AccentColor = "#E5484D";
        return new SettingsView { DataContext = vm };
    }),
    ("welcome-drag", () =>
    {
        var vm = BuildWelcomeVm();
        vm.IsDragActive = true; // show the ported drag-drop overlay
        return new WelcomeView { DataContext = vm };
    }),
    ("notice", () => new DocumentView
    {
        DataContext = DocumentTabViewModel.FromLoad(FileLoadResult.Binary(2048), @"E:\docs\archive.bin"),
    }),
    ("csv", () =>
    {
        var tab = DocumentTabViewModel.FromFile("Name,Value\nAlpha,10\nBeta,20", @"E:\docs\table.csv");
        tab.CsvAsTableEnabled = true;
        return new DocumentView { DataContext = tab };
    }),
    // Settings is now an inline page (a "tab"), not a modal — bound to the shell VM (Layout + Diagrams).
    ("settings", () => new SettingsView { DataContext = BuildWelcomeVm() }),
    // The Чтение tab (index 1) holds the ported ШАБЛОНЫ / АКЦЕНТ / Шрифт / РАЗМЕР controls — select it
    // once the tabs template up so the render shows them, not the default Раскладка tab.
    ("settings-reading", () =>
    {
        var v = new SettingsView { DataContext = BuildWelcomeVm() };
        v.Loaded += (_, _) =>
        {
            if (v.GetVisualDescendants().OfType<TabControl>().FirstOrDefault() is { } tabs)
                tabs.SelectedIndex = 1;
        };
        return v;
    }),
    // Native ```chart (LiveCharts2) in the preview — confirms the chart actually draws, not source-fallback.
    ("chart", () => new DocumentView
    {
        DataContext = DocumentTabViewModel.FromFile(
            "# График\n\n```chart:bar\nМесяц,Продажи,Затраты\nЯнв,100,60\nФев,120,90\nМар,90,70\n```\n\n"
            + "```chart:radar\nОсь,A,B\nСкорость,80,60\nСила,70,90\nЛовкость,90,50\nУм,60,80\n```\n",
            @"E:\docs\chart.md"),
    }),
    // Scatter with real Chart.js {x,y} points — must land at their true X (not category indices 0,1,2…).
    ("scatter", () => new DocumentView
    {
        DataContext = DocumentTabViewModel.FromFile(
            "# Scatter\n\n```chart:scatter\n{\"data\":{\"datasets\":[{\"label\":\"P\","
            + "\"data\":[{\"x\":1,\"y\":2},{\"x\":5,\"y\":9},{\"x\":9,\"y\":3},{\"x\":14,\"y\":12},{\"x\":20,\"y\":7}]}]}}\n```\n",
            @"E:\docs\scatter.md"),
    }),
    // Many tabs → the strip must SHRINK them to share the width (Chrome-style), not show a horizontal
    // scrollbar. Also confirms the rest background is the transparent-surface base (no white patch).
    ("tabstrip", () =>
    {
        var names = new[]
        {
            "readme.md", "a-very-long-filename-that-would-be-wide.md", "notes.txt", "a.md", "data.csv",
            "config.json", "CHANGELOG.md", "index.html", "style.css", "main.cpp", "script.py", "b.md", "c.md", "final.md",
        };
        var tabs = names.Select(n => DocumentTabViewModel.FromFile($"# {n}", $@"E:\docs\{n}")).ToList();
        var list = new ListBox
        {
            ItemsSource = tabs,
            SelectedIndex = 0,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            ItemContainerTheme = (ControlTheme)Application.Current!.FindResource("VsCodeTab")!,
            ItemsPanel = new FuncTemplate<Panel?>(() => new Tittle.Features.Shell.TabsPanel()),
            ItemTemplate = new FuncDataTemplate<DocumentTabViewModel>((vm, _) => new TextBlock
            {
                Text = vm.Header,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                MaxWidth = 200,
                TextTrimming = TextTrimming.CharacterEllipsis,
            }),
        };
        list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        list.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        return new Border
        {
            Height = 44,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Background = Application.Current!.FindResource("WorkspaceHeaderBackgroundBrush") as IBrush,
            Child = list,
        };
    }),
};

// Standalone modal windows. Each IS a Window (the transparent ModalWindow card chrome is the thing
// under test), so it's captured directly rather than hosted inside a wrapper window. Only the
// self-contained, no-DI modals live here; the VM-backed ones (Stats/Layout/Macros/Palette) need
// stub view-models before they can join.
var modals = new (string Name, Func<Window> Build)[]
{
    ("help", () => new HelpWindow()),
    ("donate", () => new DonateWindow()),
    ("stats", () => new StatsWindow { DataContext = TextStatistics.Compute(sampleMd) }),
    ("palette", () => new CommandPaletteWindow { DataContext = new CommandPaletteViewModel(SamplePaletteItems()) }),
    ("macros", () => new MacroManagerWindow { DataContext = new MacroManagerViewModel(new RenderMacroLibrary()) }),
};

int ok = 0, total = 0;
Dispatcher.UIThread.Invoke(() =>
{
    // Capture one window to a PNG. A leaf screen is hosted in a sized wrapper window; a modal is its
    // own ModalWindow rendered as-is. Shared so both paths warm + capture + save + count identically.
    void Capture(string name, Func<Window> makeWindow)
    {
        total++;
        try
        {
            var window = makeWindow();
            window.Show();
            for (var i = 0; i < 6; i++) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(80); }
            var frame = window.CaptureRenderedFrame();
            window.Close();
            if (frame is null) { Console.WriteLine($"FAIL {name}: null frame"); return; }
            frame.Save(Path.Combine(outDir, name));
            Console.WriteLine($"  ok  {name} ({frame.PixelSize.Width}x{frame.PixelSize.Height})");
            ok++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL {name}: {ex.GetType().Name} {ex.Message}");
        }
    }

    foreach (var (themeName, variant) in themes)
    {
        Application.Current!.RequestedThemeVariant = variant;
        foreach (var (screenName, build) in screens)
        {
            // Hermetic: the settings-accent screen writes an app-level AccentBrush override that persists
            // globally; clear it before EVERY screen so none inherits a prior screen's accent regardless of
            // array order (the override is only re-applied by a screen that builds its own MainWindowViewModel).
            ClearAccentOverride();
            Capture($"{screenName}__{themeName}.png", () => new Window { Width = 960, Height = 720, Content = build() });
        }
        foreach (var (modalName, buildModal) in modals)
            Capture($"{modalName}__{themeName}.png", buildModal);
    }
});

Console.WriteLine($"{ok}/{total} rendered -> {outDir}");
return ok == total ? 0 : 1;

// Remove the app-level accent override (mirror of MainWindowViewModel.ApplyAccent's clear branch) so each
// screen renders from a known-clean accent state.
static void ClearAccentOverride()
{
    if (Application.Current is not { } app)
        return;
    app.Resources.Remove("AccentBrush");
    app.Resources.Remove("AccentHoverBrush");
    var fa = app.Styles.OfType<FluentAvalonia.Styling.FluentAvaloniaTheme>().FirstOrDefault();
    if (fa is not null)
        fa.CustomAccentColor = null;
}

static MainWindowViewModel BuildWelcomeVm()
{
    var recent = new RenderRecentFilesStore([@"E:\docs\readme.md", @"E:\notes\ideas.txt"]);
    return new MainWindowViewModel(
        new RenderFileDialog(), new RenderFileReader(), new RenderThemeService(), recent,
        new AppSettingsService(new RenderSettingsStore()), new RenderClipboard(), new RenderShell(), []);
}

// A handful of representative command-palette entries (a no-op command; the palette renders titles + shortcuts).
static IReadOnlyList<PaletteItem> SamplePaletteItems()
{
    var noop = new RelayCommand(() => { });
    return
    [
        new PaletteItem("Открыть файл…", noop, "Ctrl+O"),
        new PaletteItem("Палитра команд", noop, "Ctrl+K"),
        new PaletteItem("Найти в документе", noop, "Ctrl+F"),
        new PaletteItem("Тема: Тёмная", noop),
        new PaletteItem("Экспорт в HTML…", noop),
        new PaletteItem("Статистика документа", noop),
    ];
}

sealed class RenderFileDialog : IFileDialogService
{
    public Task<IReadOnlyList<string>> PickFilesAsync() => Task.FromResult<IReadOnlyList<string>>([]);
    public Task<string?> SaveFileAsync(string suggestedFileName) => Task.FromResult<string?>(null);
}

sealed class RenderFileReader : IFileReader
{
    public Task<FileLoadResult> LoadAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(FileLoadResult.ForText("", "UTF-8", "", 0));

    public Task<string> ReloadTextAsync(string path, string encodingName,
        CancellationToken cancellationToken = default) => Task.FromResult("");
}

sealed class RenderThemeService : IThemeService
{
    public ThemeMode Mode => ThemeMode.Dark;
    public event EventHandler? Changed;
    public void SetMode(ThemeMode mode) => Changed?.Invoke(this, EventArgs.Empty);
    public void Cycle() { }
    public void ApplyCurrent() { }
}

sealed class RenderRecentFilesStore(IEnumerable<string> items) : IRecentFilesStore
{
    private readonly List<string> _items = items.ToList();
    public IReadOnlyList<string> Items => _items;
    public event EventHandler? Changed;
    public void Add(string path)
    {
        _items.Remove(path);
        _items.Insert(0, path);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear() { _items.Clear(); Changed?.Invoke(this, EventArgs.Empty); }

    public Task PruneMissingAsync() => Task.CompletedTask;
}

sealed class RenderSettingsStore : ISettingsStore
{
    private readonly Dictionary<string, object?> _values = new();
    public T? Load<T>(string key) => _values.TryGetValue(key, out var value) ? (T?)value : default;
    public void Save<T>(string key, T value) => _values[key] = value;
}

sealed class RenderClipboard : IClipboardService
{
    public Task SetTextAsync(string text) => Task.CompletedTask;
    public Task SetHtmlAsync(string html, string plainText) => Task.CompletedTask;
    public Task<byte[]?> TryReadImagePngAsync() => Task.FromResult<byte[]?>(null);
}

sealed class RenderShell : IShellService
{
    public void RevealInExplorer(string filePath) { }
    public void OpenWithDefaultApp(string filePath) { }
}

// Stub macro library so the manager dialog renders a populated, representative list of rows.
sealed class RenderMacroLibrary : IMacroLibrary
{
    public IReadOnlyList<Macro> Macros { get; } =
    [
        new Macro("Обернуть в кавычки", RepeatMode.Once, 1, Array.Empty<IEditorIntent>(), "Ctrl+Shift+1"),
        new Macro("Удалить пустые строки", RepeatMode.UntilNoMatch, 1, Array.Empty<IEditorIntent>()),
        new Macro("Пронумеровать список", RepeatMode.Times, 5, Array.Empty<IEditorIntent>()),
    ];
    public void ReplaceMacroLibrary(IReadOnlyList<Macro> macros) { }
    public void ReplayMacro(Macro macro) { }
}
