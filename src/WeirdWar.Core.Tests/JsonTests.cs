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
}
