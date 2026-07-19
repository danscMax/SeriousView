using System.Collections.Generic;
using System.Linq;
using Tittle.Core.Abstractions;
using Tittle.Core.Editing;
using Tittle.Features.Macros;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>The macro wiring extracted out of the shell view-model (record → stop → save, slot/gesture
/// replay) — previously untested because it only ran through the live VM + keyboard tunnel.</summary>
public class MacroControllerTests
{
    private sealed class InMemoryMacroStore : IMacroStore
    {
        public List<Macro> Saved = new();
        public int Loads;
        public IReadOnlyList<Macro> Load() { Loads++; return Saved; }
        public void Save(IReadOnlyList<Macro> macros) => Saved = macros.ToList();
    }

    private static (MacroController c, InMemoryMacroStore store, FakeEditorActions actions, List<string> status)
        Build(IEnumerable<Macro>? seed = null)
    {
        var store = new InMemoryMacroStore();
        if (seed is not null)
            store.Saved = seed.ToList();
        var actions = new FakeEditorActions("abc");
        var status = new List<string>();
        var c = new MacroController(store, () => actions, status.Add);
        c.LoadDeferred(); // App calls this after the window shows; the tests want the loaded state
        return (c, store, actions, status);
    }

    [Fact]
    public void Ctor_DoesNotReadTheStore_UntilLoadDeferred()
    {
        // The library read is off the pre-paint path: the ctor must not touch the store; LoadDeferred
        // (fired after the window shows) loads it and is idempotent.
        var store = new InMemoryMacroStore
        {
            Saved = { new Macro("m", RepeatMode.Once, 1, System.Array.Empty<IEditorIntent>()) },
        };
        var actions = new FakeEditorActions("abc");

        var c = new MacroController(store, () => actions, _ => { });
        Assert.Equal(0, store.Loads);   // nothing read at construction
        Assert.False(c.HasMacro);
        Assert.Empty(c.Macros);

        c.LoadDeferred();
        Assert.Equal(1, store.Loads);
        Assert.True(c.HasMacro);
        Assert.Single(c.Macros);

        c.LoadDeferred();               // idempotent — no second read
        Assert.Equal(1, store.Loads);
    }

    [Fact]
    public void Record_NoSteps_DoesNotSaveAndReports()
    {
        var (c, store, _, status) = Build();

        c.ToggleMacroRecordingCommand.Execute(null); // start
        c.ToggleMacroRecordingCommand.Execute(null); // stop — nothing captured

        Assert.False(c.HasMacro);
        Assert.False(c.IsRecordingMacro);
        Assert.Empty(store.Saved);
        Assert.Contains(status, s => s.Contains("отменена"));
    }

    [Fact]
    public void Record_WithStep_SavesAndFlagsHasMacro()
    {
        var (c, store, _, status) = Build();

        c.ToggleMacroRecordingCommand.Execute(null); // start
        c.RecordIntent(new InsertTextIntent("Z"));
        c.ToggleMacroRecordingCommand.Execute(null); // stop — one step captured

        Assert.True(c.HasMacro);
        Assert.Single(store.Saved);
        Assert.Single(c.Macros);
        Assert.Contains(status, s => s.Contains("записан"));
    }

    [Fact]
    public void PlayMacroBySlot_InRange_RepliesToActions_OutOfRange_NoOp()
    {
        var (c, _, actions, _) = Build();
        c.ToggleMacroRecordingCommand.Execute(null);
        c.RecordIntent(new InsertTextIntent("Z"));
        c.ToggleMacroRecordingCommand.Execute(null);

        c.PlayMacroBySlot(1);
        Assert.True(actions.ReplaceCalls > 0); // intent applied to the active editor

        var before = actions.ReplaceCalls;
        c.PlayMacroBySlot(5); // out of range
        Assert.Equal(before, actions.ReplaceCalls);
    }

    [Fact]
    public void PlayMacroByGesture_MatchesOnShortcut()
    {
        var bound = new Macro("M", RepeatMode.Once, 1,
            new IEditorIntent[] { new InsertTextIntent("Z") }, "Ctrl+Shift+M");
        var (c, _, actions, _) = Build(new[] { bound });

        Assert.True(c.PlayMacroByGesture("Ctrl+Shift+M"));
        Assert.True(actions.ReplaceCalls > 0);

        var before = actions.ReplaceCalls;
        Assert.False(c.PlayMacroByGesture("Ctrl+X"));
        Assert.Equal(before, actions.ReplaceCalls);
    }
}
