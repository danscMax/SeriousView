using Tittle.Core.Services;
using Xunit;

namespace Tittle.Tests.Core;

public class NavigationHistoryTests
{
    [Fact]
    public void RecordThenBackAndForward_WalksThePositions()
    {
        var h = new NavigationHistory();
        // At 0 → jump to 5 (record 0), at 5 → jump to 9 (record 5). Now at 9.
        h.Record(0);
        h.Record(5);

        Assert.True(h.CanBack);
        Assert.False(h.CanForward);

        Assert.Equal(5, h.Back(current: 9)); // → 5, forward=[9]
        Assert.Equal(0, h.Back(current: 5)); // → 0, forward=[9,5]
        Assert.Null(h.Back(current: 0));      // nothing before 0
        Assert.True(h.CanForward);

        Assert.Equal(5, h.Forward(current: 0)); // → 5
        Assert.Equal(9, h.Forward(current: 5)); // → 9
        Assert.Null(h.Forward(current: 9));      // nothing ahead
    }

    [Fact]
    public void RecordAfterBack_ClearsForwardBranch()
    {
        var h = new NavigationHistory();
        h.Record(0);
        h.Record(5);
        h.Back(9);            // at 5, forward=[9]
        Assert.True(h.CanForward);

        h.Record(5);          // new jump from 5 → forward branch discarded
        Assert.False(h.CanForward);
    }

    [Fact]
    public void IgnoresNegativePositions()
    {
        var h = new NavigationHistory();
        h.Record(-1);
        Assert.False(h.CanBack);
        Assert.Null(h.Back(-1));
    }

    [Fact]
    public void EmptyHistory_BackAndForwardReturnNull()
    {
        var h = new NavigationHistory();
        Assert.Null(h.Back(3));
        Assert.Null(h.Forward(3));
        Assert.False(h.CanBack);
        Assert.False(h.CanForward);
    }

    private const int MaxDepth = 128;

    [Fact]
    public void RecordExactlyMaxDepth_DropsNothing()
    {
        var h = new NavigationHistory();
        for (var i = 0; i < MaxDepth; i++)
            h.Record(i);

        Assert.Equal(MaxDepth, h.BackCountForTests);
        for (var expected = MaxDepth - 1; expected >= 0; expected--)
            Assert.Equal(expected, h.Back(expected)); // full drain in LIFO order — oldest survived
        Assert.False(h.CanBack);
    }

    [Fact]
    public void RecordBeyondCap_DropsOldestKeepsLifo()
    {
        var h = new NavigationHistory();
        for (var i = 1; i <= 130; i++)
            h.Record(i);

        // Depth capped at 128: entries 1 and 2 (oldest) were evicted.
        Assert.Equal(MaxDepth, h.BackCountForTests);
        for (var expected = 130; expected >= 3; expected--)
            Assert.Equal(expected, h.Back(expected));
        Assert.Null(h.Back(0));
        Assert.False(h.CanBack);
    }

    [Fact]
    public void BackCount_NeverExceedsMaxDepth_NoMatterHowManyRecords()
    {
        var h = new NavigationHistory();
        for (var i = 0; i < 10_000; i++)
            h.Record(i % 50);

        Assert.Equal(MaxDepth, h.BackCountForTests);
    }

    [Fact]
    public void BoundedStack_DropsOldestWhenFull()
    {
        // The cap mechanism shared by BOTH stacks (Record can clear forward mid-flight, so the
        // >capacity case is only reachable directly): push past capacity, oldest entries vanish.
        var s = new NavigationHistory.BoundedStack(MaxDepth);
        for (var i = 0; i < 200; i++)
            s.Push(i);

        Assert.Equal(MaxDepth, s.Count); // 72 oldest (0..71) silently dropped
        for (var expected = 199; expected >= 72; expected--)
            Assert.Equal(expected, s.Pop());
        Assert.Equal(0, s.Count);
    }

    [Fact]
    public void RepeatedBackForwardCycles_KeepBothStacksBounded()
    {
        var h = new NavigationHistory();
        for (var cycle = 0; cycle < 5; cycle++)
        {
            for (var i = 0; i < 40; i++)
                h.Record(i);

            var stash = cycle * 100;
            while (h.Back(stash++) is not null) { } // drain back → forward stashes
            Assert.InRange(h.ForwardCountForTests, 1, MaxDepth);

            while (h.Forward(stash++) is not null) { } // drain forward → back stashes
            Assert.True(h.BackCountForTests <= MaxDepth);
            Assert.False(h.CanForward);
        }
    }
}
