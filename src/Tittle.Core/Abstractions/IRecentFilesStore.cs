using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tittle.Core.Abstractions;

/// <summary>Persistent most-recently-opened file list.</summary>
public interface IRecentFilesStore
{
    IReadOnlyList<string> Items { get; }

    void Add(string path);

    /// <summary>Wipe the recent-files list (the «Очистить данные вьюера» privacy command).</summary>
    void Clear();

    /// <summary>Drop entries whose file no longer exists, off the UI thread, then persist + raise
    /// <see cref="Changed"/> if anything was pruned. Called AFTER the window is shown so a recent
    /// entry on a disconnected network share can't stall startup on a blocking File.Exists.</summary>
    Task PruneMissingAsync();

    event EventHandler? Changed;
}
