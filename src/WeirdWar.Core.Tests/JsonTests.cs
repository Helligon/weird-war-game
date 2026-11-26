using WeirdWar.Core.Serialization;

namespace WeirdWar.Core.Tests;

public class JsonTests
{
    [Fact]
    public void TEC_008_json_round_trips_all_value_kinds()
    {
        var original = new Dictionary<string, object?>
        {
            ["n"] = null, ["t"] = true, ["f"] = false, ["i"] = -42L, ["big"] = long.MaxValue,
            ["d"] = 1.5, ["whole"] = 2.0, ["s"] = "quote\" slash\\ nl\n tab\t ctl\u0001 unié",
            ["list"] = new List<object?> { 1L, "a", null, new List<object?>() },
            ["obj"] = new Dictionary<string, object?> { ["k"] = "v" },
        };

        var back = (Dictionary<string, object?>)Json.Parse(Json.Write(original))!;

        Assert.Equal(Json.Write(original), Json.Write(back));
        Assert.IsType<long>(back["i"]);
        Assert.IsType<double>(back["whole"]);
        Assert.Equal(long.MaxValue, back["big"]);
        Assert.Equal("quote\" slash\\ nl\n tab\t ctl\u0001 unié", back["s"]);
    }

    [Fact]
    public void TEC_008_json_writer_is_canonical_sorted_keys_no_whitespace()
    {
        var a = new Dictionary<string, object?> { ["b"] = 1L, ["a"] = 2L };
        var b = new Dictionary<string, object?> { ["a"] = 2L, ["b"] = 1L };

        Assert.Equal("{\"a\":2,\"b\":1}", Json.Write(a));
        Assert.Equal(Json.Write(a), Json.Write(b));
    }

    [Fact]
    public void TEC_008_json_reads_whitespace_and_escapes()
    {
        var v = (Dictionary<string, object?>)Json.Parse(" { \"a\" : [ 1 , 2.5e1 , \"\\u0041\\/\" ] } ")!;

        var list = (List<object?>)v["a"]!;
        Assert.Equal(1L, list[0]);
        Assert.Equal(25.0, list[1]);
        Assert.Equal("A/", list[2]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("{\"a\":1,}")]
    [InlineData("[1,]")]
    [InlineData("{\"a\":1,\"a\":2}")]
    [InlineData("01")]
    [InlineData("1.")]
    [InlineData("\"unterminated")]
    [InlineData("\"bad\\q\"")]
    [InlineData("tru")]
    [InlineData("1 2")]
    public void TEC_008_json_rejects_malformed_text(string text)
    {
        Assert.Throws<JsonException>(() => Json.Parse(text));
    }

    [Fact]
    public void TEC_008_json_writer_rejects_unsupported_values()
    {
        Assert.Throws<JsonException>(() => Json.Write(double.NaN));
        Assert.Throws<JsonException>(() => Json.Write(new object()));
    }

    [Fact]
    public void TEC_008_json_float_text_is_pinned_and_culture_independent()
    {
        var odd = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        odd.NumberFormat.NegativeSign = "\u2212";
        odd.NumberFormat.NumberDecimalSeparator = ",";
        odd.NumberFormat.NumberGroupSeparator = " ";
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = odd;

            Assert.Equal("0.30000000000000004", Json.Write(0.1 + 0.2));
            Assert.Equal("-0.0", Json.Write(-0.0));
            Assert.Equal("1E+21", Json.Write(1e21));
            Assert.Equal("5E-324", Json.Write(double.Epsilon));
            Assert.Equal("1.7976931348623157E+308", Json.Write(double.MaxValue));
            Assert.Equal("-1.5", Json.Write(-1.5));
            Assert.Equal("2.0", Json.Write(2.0));
            Assert.Equal("-1234567", Json.Write(-1234567L));
            Assert.Equal("-12", Json.Write(-12));
            Assert.Equal("9223372036854775807", Json.Write((ulong)long.MaxValue));
            Assert.Equal(-1234567L, Json.Parse("-1234567"));
            Assert.Equal(-1.5, Json.Parse("-1.5"));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void TEC_008_json_floats_round_trip_exactly()
    {
        foreach (var d in new[] { 0.1 + 0.2, 1e21, double.Epsilon, double.MaxValue, -1.5, 1.0 / 3.0 })
            Assert.Equal(d, Json.Parse(Json.Write(d)));
    }

    [Theory]
    [InlineData("9223372036854775808")]
    [InlineData("-9223372036854775809")]
    [InlineData("123456789012345678901234567890")]
    public void TEC_008_json_integers_beyond_long_are_an_error(string text)
    {
        var ex = Assert.Throws<JsonException>(() => Json.Parse(text));
        Assert.Contains("Integer out of range", ex.Message);
    }

    [Fact]
    public void TEC_008_json_long_extremes_read_exactly()
    {
        Assert.Equal(long.MinValue, Json.Parse("-9223372036854775808"));
        Assert.Equal(long.MaxValue, Json.Parse("9223372036854775807"));
    }

    [Theory]
    [InlineData("1e400")]
    [InlineData("-1e400")]
    public void TEC_008_json_rejects_non_finite_numbers_on_read(string text)
    {
        Assert.Throws<JsonException>(() => Json.Parse(text));
    }

    [Fact]
    public void TEC_008_json_nesting_limit_is_a_clear_error_not_a_crash()
    {
        string Nest(int n) => new string('[', n) + new string(']', n);

        Assert.NotNull(Json.Parse(Nest(Json.MaxDepth)));
        var ex = Assert.Throws<JsonException>(() => Json.Parse(Nest(Json.MaxDepth + 1)));
        Assert.Contains("Nesting deeper than", ex.Message);
        Assert.Throws<JsonException>(() => Json.Parse(new string('[', 100000)));
    }

    [Fact]
    public void TEC_008_json_accepts_a_utf8_bom()
    {
        Assert.Equal(1L, Json.Parse("\uFEFF1"));
        Assert.Equal(1L, ((List<object?>)Json.Parse("\uFEFF [1]")!)[0]);
    }

    [Fact]
    public void TEC_008_json_errors_report_line_and_column()
    {
        var ex = Assert.Throws<JsonException>(() => Json.Parse("{\n  \"a\": 1,\n  \"b\": ?\n}"));

        Assert.Contains("line 3, column 8", ex.Message);
        Assert.Contains("line 1, column 1", Assert.Throws<JsonException>(() => Json.Parse("x")).Message);
    }

    [Fact]
    public void TEC_008_json_writes_ulong_and_typed_lists()
    {
        Assert.Equal("[1,2,3]", Json.Write(new List<long> { 1, 2, 3 }));
        Assert.Equal("[1,2]", Json.Write(new[] { 1, 2 }));
        Assert.Equal("[\"a\",null]", Json.Write(new List<string?> { "a", null }));
        Assert.Equal("{\"x\":[true]}", Json.Write(new Dictionary<string, object?> { ["x"] = new List<bool> { true } }));
        Assert.Equal("9223372036854775807", Json.Write((ulong)long.MaxValue));
    }

    [Fact]
    public void TEC_008_json_writer_never_writes_a_ulong_the_reader_would_reject()
    {
        Assert.Throws<JsonException>(() => Json.Write((ulong)long.MaxValue + 1));
        Assert.Throws<JsonException>(() => Json.Write(ulong.MaxValue));
        Assert.Throws<JsonException>(() => Json.Write(new List<ulong> { ulong.MaxValue }));
        Assert.Equal(long.MaxValue, Json.Parse(Json.Write((ulong)long.MaxValue)));
    }

    [Fact]
    public void TEC_008_json_writer_refuses_unordered_collections()
    {
        var hashSet = new HashSet<long> { 1, 2 };
        var sorted = new SortedSet<long> { 1, 2 };

        var ex = Assert.Throws<JsonException>(() => Json.Write(hashSet));
        Assert.Contains("Unordered collection", ex.Message);
        Assert.Throws<JsonException>(() => Json.Write(sorted)); // any ISet is refused, even a sorted one
        Assert.Throws<JsonException>(() => Json.Write(new Dictionary<string, object?> { ["s"] = hashSet }));
        Assert.Equal("[1,2]", Json.Write(hashSet.OrderBy(x => x).ToList()));
    }

    [Fact]
    public void TEC_008_json_depth_is_released_after_each_container_so_wide_input_is_accepted()
    {
        var wide = "[" + string.Join(",", Enumerable.Repeat("{\"a\":[1]}", 100)) + "]";
        var wideArrays = "[" + string.Join(",", Enumerable.Repeat("[[1]]", 100)) + "]";

        Assert.Equal(100, ((List<object?>)Json.Parse(wide)!).Count);
        Assert.Equal(100, ((List<object?>)Json.Parse(wideArrays)!).Count);
    }

    [Fact]
    public void TEC_008_json_writer_rejects_lists_holding_unsupported_values()
    {
        Assert.Throws<JsonException>(() => Json.Write(new List<object> { new object() }));
    }
}
