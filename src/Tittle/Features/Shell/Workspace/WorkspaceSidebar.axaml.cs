using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using Tittle.Features.Viewer;

namespace Tittle.Features.Shell.Workspace;

/// <summary>Context panel whose content follows the selected workspace-rail section.</summary>
public partial class WorkspaceSidebar : UserControl
{
    public WorkspaceSidebar()
    {
        InitializeComponent();

        // The TOC / bookmarks panels are hidden by default (the Files section is active at startup), so
        // create them LAZILY the first time their host becomes visible instead of inflating them up front
        // during startup. Keying off each host's IsVisible (bound to IsOutlinePaneVisible /
        // IsBookmarksSectionActive) fires on the CURRENT value AND when it flips true later — covering both
        // a persisted Outline/Bookmarks section at launch and a live section switch / async document load.
        OutlineHost.GetObservable(IsVisibleProperty).Subscribe(new AnonymousObserver<bool>(visible =>
        {
            if (visible && OutlineHost.Content is null)
                OutlineHost.Content = new OutlinePanel();
        }));
        BookmarkHost.GetObservable(IsVisibleProperty).Subscribe(new AnonymousObserver<bool>(visible =>
        {
            if (visible && BookmarkHost.Content is null)
                BookmarkHost.Content = new BookmarkPanel();
        }));
    }
}
