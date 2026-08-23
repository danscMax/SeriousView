using System.Linq;
using Tittle.Core.Text;
using Xunit;

namespace Tittle.Tests.Core;

public class DelimitedTableTests
{
    [Fact]
    public void Parse_SimpleCsv_HeaderAndRows()
    {
        var table = DelimitedTable.Parse("name,age\nАня,30\nБорис,25", ',');

        Assert.NotNull(table);
        Assert.Equal(new[] { "name", "age" }, table!.Header);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(new[] { "Аня", "30" }, table.Rows[0]);
        Assert.False(table.Truncated);
    }

    [Fact]
    public void Parse_QuotedEmptyValue_IsKept_BareBlankLineIsSkipped()
    {
        // Audit V13: a quoted "" is an EXPLICIT empty value and must survive (not be confused with a
        // bare blank separator line, which is still skipped).
        var table = DelimitedTable.Parse("name\nАня\n\"\"\n\nБорис", ',');

        Assert.NotNull(table);
        // Аня, the explicit empty "" value, then Борис — the bare blank line between is dropped.
        Assert.Equal(new[] { "Аня", "", "Борис" }, table!.Rows.Select(r => r[0]));
    }

    [Fact]
    public void Parse_QuotedFields_WithDelimitersEscapesAndNewlines()
    {
        var table = DelimitedTable.Parse("a,b\n\"x,y\",\"he said \"\"hi\"\"\"\n\"multi\nline\",2", ',');

        Assert.Equal(new[] { "x,y", "he said \"hi\"" }, table!.Rows[0]);
        Assert.Equal("multi\nline", table.Rows[1][0]);
    }

    [Fact]
    public void Parse_Tsv_UsesTabs()
    {
        var table = DelimitedTable.Parse("a\tb\n1\t2", '\t');

        Assert.Equal(new[] { "1", "2" }, table!.Rows[0]);
    }

    [Fact]
    public void Parse_RaggedRows_ArePaddedToTheHeader()
    {
        var table = DelimitedTable.Parse("a,b,c\n1,2\n1,2,3,4", ',');

        Assert.All(table!.Rows, r => Assert.Equal(3, r.Length));
        Assert.Equal("", table.Rows[0][2]);
    }

    [Fact]
    public void Parse_CapsAt10kRows_AndFlagsTruncation()
    {
        var text = "h\n" + string.Join("\n", Enumerable.Range(1, 10_500).Select(i => i.ToString()));

        var table = DelimitedTable.Parse(text, ',');

        Assert.Equal(10_000, table!.Rows.Count);
        Assert.True(table.Truncated);
    }

    [Fact]
    public void Parse_ExactlyMaxRowsDataRows_IsNotTruncated()
    {
        var text = "h\n" + string.Join("\n", Enumerable.Range(1, DelimitedTable.MaxRows));

        var table = DelimitedTable.Parse(text, ',');

        Assert.NotNull(table);
        Assert.Equal(DelimitedTable.MaxRows, table!.Rows.Count);
        Assert.Equal(DelimitedTable.MaxRows.ToString(), table.Rows[^1][0]);
        Assert.False(table.Truncated);
    }

    [Fact]
    public void Parse_MaxRowsPlus5DataRows_IsTruncated_KeepsFirstMaxRowsIntact()
    {
        var text = "h\n" + string.Join("\n", Enumerable.Range(1, DelimitedTable.MaxRows + 5));

        var table = DelimitedTable.Parse(text, ',');

        Assert.NotNull(table);
        Assert.True(table!.Truncated);
        Assert.Equal(DelimitedTable.MaxRows, table.Rows.Count);
        Assert.Equal("1", table.Rows[0][0]);
    }

    [Fact]
    public void Parse_BlankLinesNearCap_AreDropped_DoNotConsumeBudgetOrFlipTruncation()
    {
        // MaxRows - 5 data rows, ten bare blank lines right at the cap, then 5 more data rows:
        // the blanks are dropped, so the total stays exactly MaxRows and truncation must NOT fire.
        var text = "h\n"
            + string.Join("\n", Enumerable.Range(1, DelimitedTable.MaxRows - 5))
            + "\n\n\n\n\n\n\n\n\n\n"
            + string.Join("\n", new[] { "v1", "v2", "v3", "v4", "v5" });

        var table = DelimitedTable.Parse(text, ',');

        Assert.NotNull(table);
        Assert.False(table!.Truncated);
        Assert.Equal(DelimitedTable.MaxRows, table.Rows.Count);
        Assert.Equal("v5", table.Rows[^1][0]);
    }

    [Fact]
    public void Parse_QuotedEmptyRowAtCapBoundary_CountsAsGenuineRecord()
    {
        // An explicit quoted "" value is a real record: (MaxRows - 1) ordinary rows + "" + one
        // more row = MaxRows + 1 data rows, i.e. over the cap.
        var text = "h\n"
            + string.Join("\n", Enumerable.Range(1, DelimitedTable.MaxRows - 1))
            + "\n\"\"\nextra";

        var table = DelimitedTable.Parse(text, ',');

        Assert.NotNull(table);
        Assert.True(table!.Truncated);
        Assert.Equal(DelimitedTable.MaxRows, table.Rows.Count);
        Assert.Equal("", table.Rows[^1][0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void Parse_EmptyInput_ReturnsNull(string input)
        => Assert.Null(DelimitedTable.Parse(input, ','));
}
