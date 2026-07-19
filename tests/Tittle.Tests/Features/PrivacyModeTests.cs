using System;
using System.Collections.Generic;
using System.IO;
using Tittle.Core.Abstractions;
using Tittle.Core.Services;
using Tittle.Platform;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>Privacy port: private mode suppresses passive history (session/recents/visited) while
/// keeping settings + bookmarks; «Очистить данные» wipes every history store except settings.</summary>
public class PrivacyModeTests
{
    private sealed class FakeStore : ISettingsStore
    {
        public Dictionary<string, object?> Data { get; } = new();
        public T? Load<T>(string key) => Data.TryGetValue(key, out var v) ? (T?)v : default;
        public void Save<T>(string key, T value) => Data[key] = value;
    }

    // Distinct unique dirs: the store's temp-filter root and the doc dir must NOT be under one another,
    // else the doc reads as a throwaway temp file and Add skips it (unrelated to the privacy gate).
    private static string Uniq(string tag) => Path.Combine(Path.GetTempPath(), tag + Guid.NewGuid().ToString("N"));

    [Fact]
    public void RecentFiles_Add_IsNoOp_WhenPrivate()
    {
        var store = new FakeStore();
        var recent = new RecentFilesStore(store, new PrivacyState { IsPrivate = true }, Uniq("root"));

        recent.Add(Path.Combine(Uniq("docs"), "doc.md"));

        Assert.Empty(recent.Items);
        Assert.False(store.Data.ContainsKey("recent")); // nothing persisted
    }

    [Fact]
    public void RecentFiles_Add_Records_WhenNotPrivate()
    {
        var store = new FakeStore();
        var recent = new RecentFilesStore(store, new PrivacyState { IsPrivate = false }, Uniq("root"));
        var p = Path.Combine(Uniq("docs"), "doc.md");

        recent.Add(p);

        Assert.Contains(p, recent.Items);
    }

    [Fact]
    public void RecentFiles_Clear_Wipes()
    {
        var store = new FakeStore();
        var docs = Uniq("docs");
        var recent = new RecentFilesStore(store, new PrivacyState(), Uniq("root"));
        recent.Add(Path.Combine(docs, "a.md"));
        recent.Add(Path.Combine(docs, "b.md"));
        Assert.NotEmpty(recent.Items);

        recent.Clear();

        Assert.Empty(recent.Items);
        Assert.Empty((List<string>)store.Data["recent"]!); // persisted empty
    }

    [Fact]
    public void ViewState_MarkVisited_IsNoOp_WhenPrivate()
    {
        var store = new ViewStateStore(new FakeStore(), new PrivacyState { IsPrivate = true });

        var recorded = store.MarkVisited("/docs/a.md", 3);

        Assert.False(recorded);
        Assert.False(store.IsVisited("/docs/a.md", 3));
    }

    [Fact]
    public void ViewState_Bookmark_StillPersists_WhenPrivate()
    {
        // Private mode suppresses PASSIVE history (visited) but keeps deliberate bookmarks.
        var backing = new FakeStore();
        var privacy = new PrivacyState { IsPrivate = true };
        var store = new ViewStateStore(backing, privacy);

        Assert.True(store.ToggleBookmark("/docs/a.md", 5));
        store.Flush();

        var reloaded = new ViewStateStore(backing, privacy);
        Assert.True(reloaded.IsBookmarked("/docs/a.md", 5));
    }

    [Fact]
    public void ViewState_Clear_WipesVisitedAndBookmarks_OnDiskToo()
    {
        var backing = new FakeStore();
        var store = new ViewStateStore(backing); // not private
        store.MarkVisited("/docs/a.md", 1);
        store.ToggleBookmark("/docs/a.md", 2);
        store.Flush();

        store.Clear();

        Assert.False(store.IsVisited("/docs/a.md", 1));
        Assert.False(store.IsBookmarked("/docs/a.md", 2));
        var reloaded = new ViewStateStore(backing);
        Assert.False(reloaded.IsBookmarked("/docs/a.md", 2)); // disk wiped too
    }
}
