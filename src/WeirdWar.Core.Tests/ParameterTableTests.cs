using System.Globalization;
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
    [InlineData("P-SUP-011 = 3")]
    [InlineData("XP-SUP-01 = 3")]
    [InlineData("P-SUP-01 = 3 # Placeholder (open)")]
    [InlineData("P-SUP-01 = 3 # open placeholder")]
    [InlineData("P-SUP-01 = 3 # not a placeholder (open)")]
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
    public void DLV_014_comments_and_blank_lines_are_ignored()
    {
        var table = ParameterTable.Parse("# note\n\r\n  P-SUP-01 = 0.75  # decided\r\n");

        Assert.Equal(0.75, table.GetDouble("P-SUP-01"));
        Assert.False(table.IsPlaceholder("P-SUP-01"));
    }

    [Fact]
    public void TEC_008_values_parse_the_same_under_a_comma_decimal_culture()
    {
        var oldCulture = CultureInfo.CurrentCulture;
        var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            var table = ParameterTable.Parse("P-SUP-01 = 0.75\n");

            Assert.Equal(0.75, table.GetDouble("P-SUP-01"));
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUi;
        }
    }

    [Fact]
    public void DLV_014_GetInt_rejects_a_non_integer_value()
    {
        var table = ParameterTable.Parse("P-SUP-01 = 0.75\n");

        Assert.Throws<ParameterFormatException>(() => table.GetInt("P-SUP-01"));
    }

    [Theory]
    [InlineData("1,000")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void DLV_014_GetDouble_rejects_thousands_separators_and_non_finite_values(string value)
    {
        var table = ParameterTable.Parse($"P-SUP-01 = {value}\n");

        Assert.Throws<ParameterFormatException>(() => table.GetDouble("P-SUP-01"));
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
        Assert.True(table.IsPlaceholder("P-RES-01"));
    }
}
