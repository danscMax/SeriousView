using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Tittle.Core.Settings;

namespace Tittle.Shared;

/// <summary>
/// Shell layout options driving the chrome — where the menu lives, the toolbar mode, and omnibar
/// visibility. Observable so the chrome re-renders live; mirrored to and from the persisted
/// <see cref="LayoutSettings"/>. One instance per window, mirroring <see cref="EditorOptions"/>.
/// Defaults are the M7.5 etalon (menu hidden behind ☰).
/// </summary>
public partial class LayoutOptions : ObservableObject
{
    [ObservableProperty]
    private MenuPlacement _menuPlacement = MenuPlacement.Hidden;

    [ObservableProperty]
    private ToolbarMode _toolbarMode = ToolbarMode.Contextual;

    [ObservableProperty]
    private bool _showOmnibar = true;

    [ObservableProperty]
    private bool _isWorkspaceSidebarOpen = true;

    [ObservableProperty]
    private WorkspaceSection _workspaceSection = WorkspaceSection.Files;

    partial void OnWorkspaceSectionChanged(WorkspaceSection value)
    {
        var normalized = NormalizeWorkspaceSection(value);
        if (normalized != value)
            WorkspaceSection = normalized;
    }

    public static WorkspaceSection NormalizeWorkspaceSection(WorkspaceSection value) =>
        Enum.IsDefined(value) ? value : WorkspaceSection.Files;

    [ObservableProperty]
    private ReadingWidth _readingWidth = ReadingWidth.Comfort;

    [ObservableProperty]
    private ReadingDensity _readingDensity = ReadingDensity.Normal;

    // Fine-grain reading typography. The density preset above is a shortcut: picking one writes concrete
    // numbers into LineSpacing/ParagraphSpacing (OnReadingDensityChanged), which the preview reads directly.
    // A manual tweak leaves the dropdown showing the last-picked preset — acceptable (preset + custom).
    public const double MinLineSpacing = 0, MaxLineSpacing = 40;
    public const double MinParagraphSpacing = 0, MaxParagraphSpacing = 48;
    public const double MinHeadingScale = 0.7, MaxHeadingScale = 1.8;

    [ObservableProperty]
    private double _lineSpacing = 5;

    partial void OnLineSpacingChanged(double value)
    {
        var clamped = Math.Clamp(double.IsNaN(value) ? 5 : value, MinLineSpacing, MaxLineSpacing);
        if (clamped != value)
            LineSpacing = clamped;
    }

    [ObservableProperty]
    private double _paragraphSpacing = 20;

    partial void OnParagraphSpacingChanged(double value)
    {
        var clamped = Math.Clamp(double.IsNaN(value) ? 20 : value, MinParagraphSpacing, MaxParagraphSpacing);
        if (clamped != value)
            ParagraphSpacing = clamped;
    }

    [ObservableProperty]
    private double _headingScale = 0.80;

    partial void OnHeadingScaleChanged(double value)
    {
        var clamped = Math.Clamp(double.IsNaN(value) ? 0.80 : value, MinHeadingScale, MaxHeadingScale);
        if (clamped != value)
            HeadingScale = clamped;
    }

    [ObservableProperty]
    private TextAlign _textAlignment = TextAlign.Left;

    [ObservableProperty]
    private ReadingFont _fontFamily = ReadingFont.Sans;

    [ObservableProperty]
    private bool _numberHeadings;

    // Picking a density preset writes its concrete spacing numbers (the fine-grain fields are the truth the
    // preview reads). Guarded so loading persisted settings (which set the numbers explicitly) isn't clobbered.
    private bool _suppressPresetApply;

    partial void OnReadingDensityChanged(ReadingDensity value)
    {
        if (_suppressPresetApply)
            return;
        var (line, para) = DensityPreset(value);
        LineSpacing = line;
        ParagraphSpacing = para;
    }

    /// <summary>The concrete (line spacing, paragraph spacing) numbers behind each density preset.
    /// Widened 2026-07-18 for the VS-Code paragraph rhythm; the single source both the preset dropdown
    /// and the fine-grain fields agree on.</summary>
    public static (double LineSpacing, double ParagraphSpacing) DensityPreset(ReadingDensity density) => density switch
    {
        ReadingDensity.Compact => (5, 8),
        ReadingDensity.Relaxed => (16, 24),
        _ => (10, 14),
    };

    [ObservableProperty]
    private SplitOrientation _splitOrientation = SplitOrientation.Horizontal;

    /// <summary>Source-pane fraction of the split view. Shared by the splitter (view) and persistence.</summary>
    public const double MinSplitRatio = 0.15, MaxSplitRatio = 0.85, DefaultSplitRatio = 0.5;

    /// <summary>Clamp a split ratio into the allowed range (NaN → default).</summary>
    public static double ClampSplitRatio(double r) =>
        Math.Clamp(double.IsNaN(r) ? DefaultSplitRatio : r, MinSplitRatio, MaxSplitRatio);

    [ObservableProperty]
    private double _splitRatio = DefaultSplitRatio;

    // Keep the ratio sane even if a settings file is hand-edited out of range.
    partial void OnSplitRatioChanged(double value)
    {
        var clamped = ClampSplitRatio(value);
        if (clamped != value)
            SplitRatio = clamped; // re-set lands in range; an equal value is a no-op (no loop)
    }

    /// <summary>Outline/TOC sidebar width range (px). Shared by the splitter (view) and persistence.</summary>
    public const double MinOutlineWidth = 180, MaxOutlineWidth = 480, DefaultOutlineWidth = 240;

    /// <summary>Clamp a width into the allowed range (NaN → default).</summary>
    public static double ClampOutlineWidth(double w) =>
        Math.Clamp(double.IsNaN(w) ? DefaultOutlineWidth : w, MinOutlineWidth, MaxOutlineWidth);

    [ObservableProperty]
    private double _outlineWidth = DefaultOutlineWidth;

    // Keep the persisted/observable width sane even if a settings file is hand-edited out of range.
    partial void OnOutlineWidthChanged(double value)
    {
        var clamped = ClampOutlineWidth(value);
        if (clamped != value)
            OutlineWidth = clamped; // re-set lands in range; an equal value is a no-op (no loop)
    }

    public LayoutSettings ToSettings() => new()
    {
        MenuPlacement = MenuPlacement,
        ToolbarMode = ToolbarMode,
        ShowOmnibar = ShowOmnibar,
        IsWorkspaceSidebarOpen = IsWorkspaceSidebarOpen,
        WorkspaceSection = WorkspaceSection,
        OutlineWidth = OutlineWidth,
        ReadingWidth = ReadingWidth,
        ReadingDensity = ReadingDensity,
        LineSpacing = LineSpacing,
        ParagraphSpacing = ParagraphSpacing,
        HeadingScale = HeadingScale,
        TextAlignment = TextAlignment,
        FontFamily = FontFamily,
        NumberHeadings = NumberHeadings,
        SplitOrientation = SplitOrientation,
        SplitRatio = SplitRatio,
    };

    /// <summary>Build options from persisted settings, or the etalon defaults when none are saved.
    /// The density-preset auto-apply is suppressed DURING load so a saved CUSTOM spacing isn't overwritten
    /// by the preset that ReadingDensity would otherwise re-apply; it's re-enabled so later interactive
    /// preset picks work.</summary>
    public static LayoutOptions FromSettings(LayoutSettings? s)
    {
        if (s is null)
            return new LayoutOptions();

        var o = new LayoutOptions { _suppressPresetApply = true };
        o.MenuPlacement = s.MenuPlacement;
        o.ToolbarMode = s.ToolbarMode;
        o.ShowOmnibar = s.ShowOmnibar;
        o.IsWorkspaceSidebarOpen = s.IsWorkspaceSidebarOpen;
        o.WorkspaceSection = NormalizeWorkspaceSection(s.WorkspaceSection);
        o.OutlineWidth = s.OutlineWidth;
        o.ReadingWidth = s.ReadingWidth;
        o.ReadingDensity = s.ReadingDensity;
        o.LineSpacing = s.LineSpacing;
        o.ParagraphSpacing = s.ParagraphSpacing;
        o.HeadingScale = s.HeadingScale;
        o.TextAlignment = s.TextAlignment;
        o.FontFamily = s.FontFamily;
        o.NumberHeadings = s.NumberHeadings;
        o.SplitOrientation = s.SplitOrientation;
        o.SplitRatio = s.SplitRatio;
        o._suppressPresetApply = false;
        return o;
    }
}
