using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Tittle.Features.Shell.Workspace;

/// <summary>Compact primary navigation for the document workspace.</summary>
public partial class WorkspaceRail : UserControl
{
    public WorkspaceRail() => InitializeComponent();

    /// <summary>The palette is a window concern; the Task 4 shell host handles this request.</summary>
    public event EventHandler? CommandPaletteRequested;

    private void OnCommandPaletteClick(object? sender, RoutedEventArgs e) =>
        CommandPaletteRequested?.Invoke(this, EventArgs.Empty);
}
