using System.Text.RegularExpressions;
using Xunit;

namespace Tittle.Tests.Features;

/// <summary>
/// Small ratchet tests for the visual canon. These are adoption checks: they prove feature views
/// keep routing through semantic tokens and shared styles instead of merely proving the resources
/// exist. The checks intentionally scan only source files under test, so a violation introduced in
/// a new feature fails immediately while the theme dictionaries remain the single colour source.
/// </summary>
public sealed class DesignSystemAdoptionTests
{
    private static readonly Regex HardCodedVisualValue = new(
        "(?:Color|Background|Foreground|BorderBrush)=\\\"(?:#[0-9A-Fa-f]{6,8}|White|Black|Red|Blue|Green|Gray|Grey|Yellow|Orange|Purple)\\\"|" +
        "Property=\\\"(?:Background|Foreground|BorderBrush)\\\"[^>]*Value=\\\"(?:#[0-9A-Fa-f]{6,8}|White|Black|Red|Blue|Green|Gray|Grey|Yellow|Orange|Purple)\\\"|" +
        "BoxShadow=\\\"(?!\\{)[^\\\"]+\\\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LiteralMotionDuration = new(
        "Duration=\\\"0:0:0\\.[0-9]+\\\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void FeatureViews_UseSemanticVisualTokens()
    {
        var featureFiles = FindProjectRoot()
            .GetDirectories("src", "Tittle", "Features")
            .GetFiles("*.axaml", SearchOption.AllDirectories)
            .ToArray();

        var violations = featureFiles
            .SelectMany(file => File.ReadLines(file.FullName)
                .Select((line, index) => (file, Line: index + 1, Text: line)))
            .Where(entry => HardCodedVisualValue.IsMatch(entry.Text))
            .Select(entry => $"{entry.file.FullName}:{entry.Line}: {entry.Text.Trim()}")
            .ToArray();

        Assert.True(violations.Length == 0,
            "Feature views must use DynamicResource tokens for visual values:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void FeatureViews_UseSharedMotionTokens()
    {
        var root = FindProjectRoot();
        var sourceFiles = root.GetDirectories("src", "Tittle")
            .GetFiles("*.axaml", SearchOption.AllDirectories)
            .Where(file => !file.FullName.Contains(Path.Combine("Themes", "Colors"), StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.FullName.EndsWith("Themes\\Tokens.axaml", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var violations = sourceFiles
            .SelectMany(file => File.ReadLines(file.FullName)
                .Select((line, index) => (file, Line: index + 1, Text: line)))
            .Where(entry => LiteralMotionDuration.IsMatch(entry.Text))
            .Select(entry => $"{entry.file.FullName}:{entry.Line}: {entry.Text.Trim()}")
            .ToArray();

        Assert.True(violations.Length == 0,
            "Animations must reference DurFast/DurBase tokens:\n" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void StatusButton_HasOneSharedStyleHome()
    {
        var root = FindProjectRoot();
        var controls = root.GetDirectories("src", "Tittle", "Themes").GetFiles("Controls.axaml").Single();
        Assert.Contains("<Style Selector=\"Button.statusbtn\"", File.ReadAllText(controls.FullName), StringComparison.Ordinal);

        var localStyles = root.GetDirectories("src", "Tittle", "Features")
            .GetFiles("*.axaml", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadLines(file.FullName)
                .Select((line, index) => (file, Line: index + 1, Text: line)))
            .Where(entry => entry.Text.Contains("<Style Selector=\"Button.statusbtn", StringComparison.Ordinal))
            .Select(entry => $"{entry.file.FullName}:{entry.Line}")
            .ToArray();

        Assert.Empty(localStyles);
    }

    private static DirectoryInfo FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Tittle.sln")))
                return directory;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Tittle solution root from the test output directory.");
    }
}

internal static class DirectoryInfoExtensions
{
    public static DirectoryInfo GetDirectories(this DirectoryInfo directory, params string[] names)
    {
        foreach (var name in names)
            directory = new DirectoryInfo(Path.Combine(directory.FullName, name));
        return directory;
    }
}
