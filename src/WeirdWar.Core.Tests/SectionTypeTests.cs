using System.Text.Json;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class SectionTypeTests
{
    private static string One(string fields) => "{\"version\":1,\"sectionTypes\":{\"x\":{" + fields + "}}}";

    private const string Good =
        "\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":3,\"maxUnits\":3,\"health\":9,\"sight\":1,\"capacityContribution\":1,\"consumptionPerHex\":1";

    private static string Shipped() => File.ReadAllText(Path.Combine(
        Path.GetDirectoryName(typeof(SectionTypeTable).Assembly.Location)!, "Data", "section-types.json"));

    [Fact]
    public void ROS_001_the_shipped_data_holds_exactly_the_six_decided_sections()
    {
        var table = SectionTypeTable.LoadShipped();

        Assert.Equal(new[] { "heavy-infantry", "medic", "riflemen", "sappers", "shotgunners", "snipers" }, table.All.Select(t => t.Id).OrderBy(i => i, StringComparer.Ordinal));
        foreach (var gone in new[] { "machine-gunners", "flamethrowers", "mortar-team", "field-gun", "light-armoured-car", "infantry", "artillery", "recon" })
            Assert.False(table.TryGet(gone, out _), gone);
        Assert.All(table.All, t =>
        {
            Assert.True(t.SpCost > 0);
            Assert.True(t.CapacityContribution > 0);
            Assert.True(t.ConsumptionPerHex > 0);
            Assert.InRange(t.Sight, 1, 3);
            Assert.Equal(t.Health, t.HealthPerUnit * t.MaxUnits);
        });
    }

    [Fact]
    public void ROS_001_the_shipped_classes_follow_the_roster_table()
    {
        var table = SectionTypeTable.LoadShipped();

        Assert.Equal(SectionClass.Infantry, table.Get("riflemen").Class);
        Assert.Equal(SectionClass.Infantry, table.Get("shotgunners").Class);
        Assert.Equal(SectionClass.Infantry, table.Get("snipers").Class);
        Assert.Equal(SectionClass.Support, table.Get("sappers").Class);
        Assert.Equal(SectionClass.Heavy, table.Get("heavy-infantry").Class);
        Assert.Equal(SectionClass.Support, table.Get("medic").Class);
    }

    [Theory]
    [InlineData("Infantry", SectionClass.Infantry)]
    [InlineData("Heavy", SectionClass.Heavy)]
    [InlineData("Artillery", SectionClass.Artillery)]
    [InlineData("Support", SectionClass.Support)]
    public void TAG_001_the_four_classes_are_accepted(string name, SectionClass expected)
    {
        var table = SectionTypeTable.Parse(One(Good.Replace("Infantry", name)));

        Assert.Equal(expected, table.Get("x").Class);
    }

    [Theory]
    [InlineData("\"class\":\"Cavalry\"")]
    [InlineData("\"class\":\"infantry\"")]
    [InlineData("\"class\":\"3\"")]
    [InlineData("\"class\":\"\"")]
    [InlineData("\"class\":7")]
    public void TAG_001_an_unknown_class_fails_naming_the_type(string bad)
    {
        var json = One(Good.Replace("\"class\":\"Infantry\"", bad));

        var e = Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(json));
        Assert.Contains("\"x\"", e.Message);
        Assert.Contains("class", e.Message);
    }

    [Fact]
    public void TAG_001_a_missing_class_fails_naming_the_type()
    {
        var e = Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(Good.Replace("\"class\":\"Infantry\",", ""))));

        Assert.Contains("\"x\"", e.Message);
        Assert.Contains("class", e.Message);
    }

    [Theory]
    [InlineData(SectionClass.Heavy, true)]
    [InlineData(SectionClass.Artillery, true)]
    [InlineData(SectionClass.Infantry, false)]
    [InlineData(SectionClass.Support, false)]
    public void TAG_012_heavy_weaponry_is_derived_from_the_class(SectionClass cls, bool expected)
    {
        var type = SectionTypeTable.Parse(One(Good.Replace("Infantry", cls.ToString()))).Get("x");

        Assert.Equal(expected, SectionTypeTable.IsHeavyWeaponry(type));
    }

    [Fact]
    public void TAG_012_heavy_weaponry_cannot_be_stored_per_type()
    {
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(Good + ",\"heavyWeaponry\":true")));
    }

    [Theory]
    [InlineData(9, 3)]
    [InlineData(6, 2)]
    public void HIE_002_HIE_004_units_follow_health(int health, int units)
    {
        var table = SectionTypeTable.Parse(One(Good));

        Assert.Equal(units, table.UnitsOf(new Section("x", health)));
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(3, 1)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    public void HIE_010_a_part_unit_still_counts_and_zero_health_is_zero_units(int health, int units)
    {
        var table = SectionTypeTable.Parse(One(Good));

        Assert.Equal(units, table.UnitsOf(new Section("x", health)));
    }

    [Theory]
    [InlineData("spCost")]
    [InlineData("healthPerUnit")]
    [InlineData("maxUnits")]
    public void BRG_034_P_BRG_02_removing_a_value_fails_loading_and_names_it(string field)
    {
        var parts = Good.Split(',').Where(p => !p.StartsWith("\"" + field + "\"")).ToArray();

        var e = Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(string.Join(",", parts))));
        Assert.Contains(field, e.Message);
    }

    [Fact]
    public void BRG_034_the_sp_cost_is_one_whole_number_per_type()
    {
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(Good.Replace("\"spCost\":10", "\"spCost\":2.5"))));
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(Good.Replace("\"spCost\":10", "\"spCost\":-1"))));
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(Good.Replace("\"spCost\":10", "\"spCost\":\"10\""))));
        Assert.Equal(10, SectionTypeTable.Parse(One(Good)).Get("x").SpCost);
    }

    [Theory]
    [InlineData("\"healthPerUnit\":3", "\"healthPerUnit\":0")]
    [InlineData("\"maxUnits\":3", "\"maxUnits\":0")]
    public void HIE_002_a_unit_needs_health_and_a_section_needs_units(string from, string to)
    {
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(Good.Replace(from, to))));
    }

    [Fact]
    public void TAG_004_strange_is_optional_and_false_when_absent()
    {
        Assert.False(SectionTypeTable.Parse(One(Good)).Get("x").Strange);
        Assert.True(SectionTypeTable.Parse(One(Good + ",\"strange\":true")).Get("x").Strange);
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(One(Good + ",\"strange\":1")));
        Assert.All(SectionTypeTable.LoadShipped().All, t => Assert.False(t.Strange)); // no decided Section is Strange yet
    }

    [Fact]
    public void TEC_008_class_cost_units_and_tags_change_the_canonical_text()
    {
        var texts = new[]
        {
            Good,
            Good.Replace("Infantry", "Heavy"),
            Good.Replace("\"spCost\":10", "\"spCost\":11"),
            Good.Replace("\"healthPerUnit\":3", "\"healthPerUnit\":1"),
            Good.Replace("\"maxUnits\":3", "\"maxUnits\":4"),
            Good + ",\"strange\":true",
            Good + ",\"slow\":true",
        }.Select(f => SectionTypeTable.Parse(One(f)).CanonicalText()).ToArray();

        Assert.Equal(texts.Length, texts.Distinct().Count());
    }

    [Fact]
    public void DLV_014_every_open_value_in_the_shipped_file_carries_a_placeholder_marker()
    {
        using var doc = JsonDocument.Parse(Shipped());
        var root = doc.RootElement;
        var markers = root.GetProperty("placeholders");

        Assert.Contains("placeholder (open)", root.GetProperty("status").GetString());
        foreach (var field in new[] { "spCost", "healthPerUnit", "maxUnits", "health", "sight", "capacityContribution", "consumptionPerHex" })
            Assert.Contains("placeholder (open)", markers.GetProperty(field).GetString());
        Assert.Contains("placeholder (proposed)", markers.GetProperty("class.sappers").GetString());
        // Every field used by a type is covered by a marker, so a new value cannot slip in unmarked.
        foreach (var type in root.GetProperty("sectionTypes").EnumerateObject())
            foreach (var field in type.Value.EnumerateObject().Where(p => p.Name != "class"))
                Assert.True(markers.TryGetProperty(field.Name, out _), type.Name + "." + field.Name);
    }

    [Fact]
    public void DLV_014_placeholders_must_be_text_markers()
    {
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse("{\"version\":1,\"placeholders\":{\"a\":1},\"sectionTypes\":{\"x\":{" + Good + "}}}"));
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse("{\"version\":1,\"placeholders\":[],\"sectionTypes\":{\"x\":{" + Good + "}}}"));
    }

    [Fact]
    public void ROS_001_the_sapper_class_is_marked_as_proposed_not_decided()
    {
        using var doc = JsonDocument.Parse(Shipped());

        Assert.Equal("Support", doc.RootElement.GetProperty("sectionTypes").GetProperty("sappers").GetProperty("class").GetString());
        Assert.StartsWith("placeholder (proposed)", doc.RootElement.GetProperty("placeholders").GetProperty("class.sappers").GetString());
    }
}
