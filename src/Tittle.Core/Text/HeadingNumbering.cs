using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tittle.Core.Text;

/// <summary>Prepends hierarchical section numbers (1, 1.1, 1.1.1 …) to ATX headings — the ported
/// <c>numberHeadings</c> display option. Pure and fence-guarded; display-only (runs in the preview
/// preprocessor, never the raw file or the outline). Setext (underline) headings are left as-is —
/// the ATX case is the common one the port targets.</summary>
public static partial class HeadingNumbering
{
    /// <summary>Number ATX headings in place. <paramref name="isFenced"/> reports whether a line sits
    /// inside a code fence (skip those). Leading all-zero levels are trimmed, so a document that starts
    /// at H2 numbers as "1", "1.1", … rather than "0.1".</summary>
    public static void Apply(List<string> lines, Func<int, bool> isFenced)
    {
        var counters = new int[6];
        for (var i = 0; i < lines.Count; i++)
        {
            if (isFenced(i))
                continue;

            var m = AtxHeading().Match(lines[i]);
            if (!m.Success)
                continue;

            var level = m.Groups["hashes"].Value.Length; // 1..6
            counters[level - 1]++;
            for (var deeper = level; deeper < 6; deeper++)
                counters[deeper] = 0;

            lines[i] = $"{m.Groups["hashes"].Value} {BuildNumber(counters, level)} {m.Groups["text"].Value}";
        }
    }

    private static string BuildNumber(int[] counters, int level)
    {
        // Skip leading zero levels (a doc that doesn't start at H1) so the number reads naturally.
        var start = 0;
        while (start < level && counters[start] == 0)
            start++;
        return string.Join(".", counters.Take(level).Skip(start));
    }

    // ATX heading: 1–6 leading '#', at least one space, then the heading text (CommonMark requires the
    // space, so `#nospace` is not a heading; 7+ '#' can't satisfy `{1,6}` + a following space either).
    [GeneratedRegex(@"^(?<hashes>#{1,6})[ \t]+(?<text>.*)$")]
    private static partial Regex AtxHeading();
}
