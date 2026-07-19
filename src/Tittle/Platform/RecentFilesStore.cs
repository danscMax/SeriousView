using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tittle.Core.Abstractions;
using Tittle.Core.Services;

namespace Tittle.Platform;

/// <summary>MRU recent-files list persisted through <see cref="ISettingsStore"/>.</summary>
public sealed class RecentFilesStore : IRecentFilesStore
{
    private const string Key = "recent";

    private readonly ISettingsStore _settings;
    private RecentFilesList _list;
    private readonly string _tempRoot;

    public event EventHandler? Changed;

    /// <param name="tempRoot">OS temp folder whose files are excluded from "Recent"; defaults to
    /// <see cref="Path.GetTempPath"/>. Overridable so tests can point at a scratch directory.</param>
    public RecentFilesStore(ISettingsStore settings, string? tempRoot = null)
    {
        _settings = settings;
        _tempRoot = tempRoot ?? Path.GetTempPath();
        // Seed from the raw list, filtering ONLY throwaway temp-folder paths (a cheap, pure-string
        // check). Existence pruning — a File.Exists per entry — is deliberately NOT done here: it runs
        // synchronously inside the VM/window construction, before first paint, where a recent entry on a
        // disconnected network share would block the UI thread for the OS timeout (seconds). That work is
        // deferred to PruneMissingAsync, fired after the window is shown (like the update check).
        var loaded = _settings.Load<List<string>>(Key) ?? new List<string>();
        var seed = loaded.Where(p => !RecentFilePathPolicy.IsUnderTempFolder(p, _tempRoot)).ToList();
        _list = new RecentFilesList(seed);
        if (seed.Count != loaded.Count)
            _settings.Save(Key, seed); // persist the (IO-free) temp prune immediately
    }

    public IReadOnlyList<string> Items => _list.Paths;

    /// <inheritdoc />
    public async Task PruneMissingAsync()
    {
        var snapshot = _list.Paths.ToList();
        // File.Exists per entry off the UI thread; the continuation resumes on the caller's context
        // (the UI thread, when fired from App after the window shows), so Changed fires there — safe for
        // the VM's ObservableProperty handler, exactly like the synchronous Add path.
        var alive = await Task.Run(() => snapshot.Where(File.Exists).ToList());
        if (alive.Count == snapshot.Count)
            return; // nothing dead — no refresh, no write

        _list = new RecentFilesList(alive);
        _settings.Save(Key, _list.Paths.ToList());
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Add(string path)
    {
        // Never record a throwaway file opened from the OS temp folder.
        if (RecentFilePathPolicy.IsUnderTempFolder(path, _tempRoot))
            return;

        _list.Add(path);
        _settings.Save(Key, _list.Paths.ToList());
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
