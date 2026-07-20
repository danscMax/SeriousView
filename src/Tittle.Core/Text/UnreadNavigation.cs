using System;

namespace Tittle.Core.Text;

/// <summary>Pure logic for jumping to the next / previous UNREAD heading (ported ]/[ keys). Works over the
/// heading outline by index (in Tittle the outline index equals the heading ordinal), with a visited
/// predicate from the reading state.</summary>
public static class UnreadNavigation
{
    /// <summary>Index of the next (forward) or previous (backward) heading that is NOT visited, relative to
    /// <paramref name="current"/>; −1 when there is none (no wrap). <paramref name="current"/> = −1 (no active
    /// heading) makes a forward scan start at 0 and a backward scan find nothing.</summary>
    public static int NextUnread(int count, Func<int, bool> isVisited, int current, bool forward)
    {
        if (forward)
        {
            for (var i = Math.Max(current + 1, 0); i < count; i++)
                if (!isVisited(i))
                    return i;
        }
        else
        {
            for (var i = Math.Min(current - 1, count - 1); i >= 0; i--)
                if (!isVisited(i))
                    return i;
        }
        return -1;
    }
}
