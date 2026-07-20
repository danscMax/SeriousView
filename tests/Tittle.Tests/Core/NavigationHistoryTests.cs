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
}
