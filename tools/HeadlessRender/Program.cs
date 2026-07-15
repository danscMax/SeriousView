using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Tittle;
using Tittle.Core.Abstractions;
using Tittle.Core.Documents;
using Tittle.Core.Services;
using Tittle.Core.Settings;
using Tittle.Core.Text;
using Tittle.Features.Donate;
using Tittle.Features.Help;
using Tittle.Features.Shell;
using Tittle.Features.Viewer;
using Tittle.Features.Welcome;
using Tittle.Platform;

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
};

// Standalone modal windows. Each IS a Window (the transparent ModalWindow card chrome is the thing
// under test), so it's captured directly rather than hosted inside a wrapper window. Only the
// self-contained, no-DI modals live here; the VM-backed ones (Stats/Layout/Macros/Palette) need
// stub view-models before they can join.
var modals = new (string Name, Func<Window> Build)[]
{
    ("help", () => new HelpWindow()),
    ("donate", () => new DonateWindow()),
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
            Capture($"{screenName}__{themeName}.png", () => new Window { Width = 960, Height = 720, Content = build() });
        foreach (var (modalName, buildModal) in modals)
            Capture($"{modalName}__{themeName}.png", buildModal);
    }
});

Console.WriteLine($"{ok}/{total} rendered -> {outDir}");
return ok == total ? 0 : 1;

static MainWindowViewModel BuildWelcomeVm()
{
    var recent = new RenderRecentFilesStore([@"E:\docs\readme.md", @"E:\notes\ideas.txt"]);
    return new MainWindowViewModel(
        new RenderFileDialog(), new RenderFileReader(), new RenderThemeService(), recent,
        new AppSettingsService(new RenderSettingsStore()), new RenderClipboard(), new RenderShell(), []);
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
}

sealed class RenderShell : IShellService
{
    public void RevealInExplorer(string filePath) { }
    public void OpenWithDefaultApp(string filePath) { }
}
