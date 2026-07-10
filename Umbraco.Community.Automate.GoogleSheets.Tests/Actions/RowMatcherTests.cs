using Shouldly;
using Umbraco.Community.Automate.GoogleSheets.Actions;
using Xunit;

namespace Umbraco.Community.Automate.GoogleSheets.Tests.Actions;

public class RowMatcherTests
{
    private static readonly List<List<string>> Rows =
    [
        ["Name", "Email"],
        ["Alice", "alice@example.com"],
        ["Bob", "bob@example.com"],
    ];

    [Fact]
    public void FindRowIndex_default_skips_row_zero()
    {
        // "Name" is only present in the header row (index 0); with the default
        // hasHeaderRow: true, it must not be matched.
        RowMatcher.FindRowIndex(Rows, 0, "Name", StringComparison.Ordinal).ShouldBe(-1);
    }

    [Fact]
    public void FindRowIndex_hasHeaderRow_false_allows_matching_row_zero()
    {
        RowMatcher.FindRowIndex(Rows, 0, "Name", StringComparison.Ordinal, hasHeaderRow: false).ShouldBe(0);
    }

    [Fact]
    public void FindRowIndex_returns_first_matching_data_row()
    {
        RowMatcher.FindRowIndex(Rows, 0, "Bob", StringComparison.Ordinal).ShouldBe(2);
    }

    [Fact]
    public void FindRowIndex_returns_minus_one_when_no_match()
    {
        RowMatcher.FindRowIndex(Rows, 0, "Charlie", StringComparison.Ordinal).ShouldBe(-1);
    }

    [Fact]
    public void FindRowIndex_treats_short_rows_as_empty_for_out_of_range_column()
    {
        // Row 0 has a value in column 1, so it doesn't match an empty search value.
        // Row 1 is too short to have column 1 at all — treated as empty, so it matches.
        List<List<string>> jaggedRows = [["Header", "x"], ["OnlyOneCell"]];

        RowMatcher.FindRowIndex(jaggedRows, 1, "", StringComparison.Ordinal, hasHeaderRow: false).ShouldBe(1);
    }

    [Theory]
    [InlineData(FindRowMatchMode.Contains, "lice", 1)]
    [InlineData(FindRowMatchMode.StartsWith, "Ali", 1)]
    [InlineData(FindRowMatchMode.EndsWith, "ice", 1)]
    [InlineData(FindRowMatchMode.Exact, "Alice", 1)]
    public void FindRowIndex_respects_match_mode(FindRowMatchMode matchMode, string value, int expected)
    {
        RowMatcher.FindRowIndex(Rows, 0, value, StringComparison.Ordinal, matchMode).ShouldBe(expected);
    }

    [Fact]
    public void FindRowIndex_respects_case_insensitive_comparison()
    {
        RowMatcher.FindRowIndex(Rows, 0, "bob", StringComparison.OrdinalIgnoreCase).ShouldBe(2);
        RowMatcher.FindRowIndex(Rows, 0, "bob", StringComparison.Ordinal).ShouldBe(-1);
    }
}
