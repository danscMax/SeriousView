using System.Collections.Generic;
using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

public class UnreadNavigationTests
{
    // visited set → predicate
    private static System.Func<int, bool> Visited(params int[] v)
    {
        var set = new HashSet<int>(v);
        return i => set.Contains(i);
    }

    [Fact]
    public void Forward_FindsFirstUnreadAfterCurrent()
    {
        // 5 headings, 0-2 visited; from current=1, next unread forward = 3.
        Assert.Equal(3, UnreadNavigation.NextUnread(5, Visited(0, 1, 2), current: 1, forward: true));
    }

    [Fact]
    public void Backward_FindsFirstUnreadBeforeCurrent()
    {
        // 5 headings, 2-4 visited; from current=4, previous unread = 1.
        Assert.Equal(1, UnreadNavigation.NextUnread(5, Visited(2, 3, 4), current: 4, forward: false));
    }

    [Fact]
    public void NoUnreadAhead_ReturnsMinusOne_NoWrap()
    {
        // Everything after current is visited → −1 (does not wrap to the start).
        Assert.Equal(-1, UnreadNavigation.NextUnread(3, Visited(2), current: 1, forward: true));
        Assert.Equal(-1, UnreadNavigation.NextUnread(3, Visited(0), current: 1, forward: false));
    }

    [Fact]
    public void NoActiveHeading_ForwardStartsAtZero_BackwardFindsNothing()
    {
        Assert.Equal(0, UnreadNavigation.NextUnread(3, Visited(), current: -1, forward: true));
        Assert.Equal(-1, UnreadNavigation.NextUnread(3, Visited(), current: -1, forward: false));
    }

    [Fact]
    public void EmptyOutline_ReturnsMinusOne()
    {
        Assert.Equal(-1, UnreadNavigation.NextUnread(0, Visited(), current: -1, forward: true));
    }
}
