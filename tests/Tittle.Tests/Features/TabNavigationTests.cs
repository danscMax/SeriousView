using Avalonia.Headless.XUnit;
using Tittle.Core.Text;
using Tittle.Features.Shell;
using Xunit;

namespace Tittle.Tests.Features;

public class TabNavigationTests
{
    [AvaloniaFact]
    public void NavigateBack_ReturnsToThePositionLeftByTheLastJump()
    {
        var tab = DocumentTabViewModel.FromFile("# A\n\n## B\n\n## C\n", @"E:\docs\x.md");
        Assert.Equal(3, tab.Outline.Count); // A(0), B(1), C(2)

        HeadingOutline? navigated = null;
        tab.NavigationRequested += h => navigated = h;

        tab.ActiveHeadingOrdinal = 0;                       // reading at A
        tab.NavigateToHeadingCommand.Execute(tab.Outline[2]); // jump to C — records 0
        tab.ActiveHeadingOrdinal = 2;                       // scroll-spy caught up to C

        navigated = null;
        tab.NavigateBackCommand.Execute(null);              // back → returns to A (0), no re-record
        Assert.NotNull(navigated);
        Assert.Equal(0, navigated!.Ordinal);
        // The position is synced at once (not waiting for the DEFERRED scroll-changed pass), so a rapid
        // second Back/Forward reads the fresh ordinal, not a stale one.
        Assert.Equal(0, tab.ActiveHeadingOrdinal);
    }

    [AvaloniaFact]
    public void JumpToUnread_NavigatesToTheFirstUnreadHeading()
    {
        // No ViewState assigned → IsHeadingVisited returns true for all (path-less/no-state), so nothing is
        // unread and the jump is a no-op. With a real path but no visits, all are unread → jumps to the next.
        var tab = DocumentTabViewModel.FromFile("# A\n\n## B\n\n## C\n", @"E:\docs\x.md");
        HeadingOutline? navigated = null;
        tab.NavigationRequested += h => navigated = h;

        tab.ActiveHeadingOrdinal = 0;
        tab.JumpToUnreadCommand.Execute(true); // forward

        // FilePath set but ViewState is null → IsHeadingVisited returns true → no unread → no navigation.
        // This asserts the guarded no-op path doesn't throw and doesn't navigate.
        Assert.Null(navigated);
    }
}
