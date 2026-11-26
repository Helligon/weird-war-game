using System.Text.RegularExpressions;
using WeirdWar.Core;

namespace WeirdWar.Core.Tests;

public class ParameterTableTests
{
    [Fact]
    public void DLV_014_returns_the_value_set_for_a_parameter_id()
    {
        var table = ParameterTable.Parse("P-SUP-01 = 3\n");

        Assert.Equal(3, table.GetInt("P-SUP-01"));
        Assert.Equal("3", table.Get("P-SUP-01"));
    }

    [Fact]
    public void DLV_014_missing_parameter_is_an_error_naming_the_id()
    {
        var table = ParameterTable.Parse("P-SUP-01 = 3\n");

        var ex = Assert.Throws<MissingParameterException>(() => table.Get("P-RES-01"));
        Assert.Equal("P-RES-01", ex.ParameterId);
        Assert.Contains("P-RES-01", ex.Message);
        Assert.Throws<MissingParameterException>(() => table.GetInt("P-RES-01"));
        Assert.Throws<MissingParameterException>(() => table.GetDouble("P-RES-01"));
        Assert.Throws<MissingParameterException>(() => table.IsPlaceholder("P-RES-01"));
    }

    [Fact]
    public void DLV_014_empty_file_has_no_defaults()
    {
        Assert.Throws<MissingParameterException>(() => ParameterTable.Parse("").Get("P-SUP-01"));
    }

    [Theory]
    [InlineData("P-SUP-01")]
    [InlineData("P-SUP-01 3")]
    [InlineData("P-SUP-01 =")]
    [InlineData("SUP-01 = 3")]
    [InlineData("p-sup-01 = 3")]
    [InlineData("P-SUP-1 = 3")]
    public void DLV_014_malformed_lines_are_rejected(string line)
    {
        Assert.Throws<ParameterFormatException>(() => ParameterTable.Parse(line));
    }

    [Fact]
    public void DLV_014_duplicate_ids_are_rejected_naming_the_id()
    {
        var ex = Assert.Throws<ParameterFormatException>(() => ParameterTable.Parse("P-SUP-01 = 3\nP-SUP-01 = 4\n"));
        Assert.Contains("P-SUP-01", ex.Message);
    }

    [Fact]
    public void DLV_014_comments_and_blank_lines_are_ignored_and_values_parse_invariantly()
    {
        var table = ParameterTable.Parse("# note\n\r\n  P-SUP-01 = 0.75  # decided\r\n");

        Assert.Equal(0.75, table.GetDouble("P-SUP-01"));
        Assert.False(table.IsPlaceholder("P-SUP-01"));
    }

    [Fact]
    public void DLV_014_non_numeric_value_read_as_number_is_an_error()
    {
        var table = ParameterTable.Parse("P-SUP-01 = abc\n");

        Assert.Throws<ParameterFormatException>(() => table.GetInt("P-SUP-01"));
        Assert.Throws<ParameterFormatException>(() => table.GetDouble("P-SUP-01"));
    }

    [Fact]
    public void DLV_014_placeholder_marker_is_recognised()
    {
        var table = ParameterTable.Parse("P-RES-01 = 5 # placeholder (open)\nP-RES-02 = 1 # placeholder (proposed)\nP-RES-03 = 2 # just a note\n");

        Assert.True(table.IsPlaceholder("P-RES-01"));
        Assert.True(table.IsPlaceholder("P-RES-02"));
        Assert.False(table.IsPlaceholder("P-RES-03"));
    }

    [Fact]
    public void DLV_014_shipped_file_keys_are_all_parameter_ids()
    {
        var table = ParameterTable.LoadShipped();

        Assert.NotEmpty(table.Ids);
        Assert.All(table.Ids, id => Assert.Matches(new Regex("^P-[A-Z]{2,4}-[0-9]{2}$"), id));
    }

    [Fact]
    public void DLV_014_shipped_file_holds_decided_P_SUP_01_and_placeholder_open_P_RES_01()
    {
        var table = ParameterTable.LoadShipped();

        Assert.Equal(0.75, table.GetDouble("P-SUP-01"));
        Assert.False(table.IsPlaceholder("P-SUP-01"));
        Assert.True(table.Contains("P-RES-01"));
        Assert.True(table.IsPlaceholder("P-RES-01"));
    }
}
