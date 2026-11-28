using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class FogOfWarTests
{
    private static string Data(int a, int b, int c) =>
        "{\"version\":1,\"sectionTypes\":{" +
        T("a", a) + "," + T("b", b) + "," + T("c", c) + "}}";

    private static string T(string id, int sight) =>
        "\"" + id + "\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":3,\"maxUnits\":3,\"health\":9,\"sight\":" + sight + ",\"capacityContribution\":1,\"consumptionPerHex\":1}";

    private static Brigade Of(params string[] types) =>
        new Brigade(1, "west", 40, default(HexCoord), types.Select(t => new Section(t, 9)).ToList());

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void FOW_004_a_sight_outside_1_to_3_fails_loading(int sight)
    {
        var e = Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(Data(sight, 1, 1)));
        Assert.Contains("sight", e.Message);
    }

    [Fact]
    public void FOW_004_P_FOW_01_sight_follows_the_data_file()
    {
        var one = new FogOfWar(SectionTypeTable.Parse(Data(1, 1, 1)));
        var two = new FogOfWar(SectionTypeTable.Parse(Data(2, 1, 1)));

        Assert.Equal(1, one.BrigadeSight(Of("a")));
        Assert.Equal(2, two.BrigadeSight(Of("a")));
    }

    [Fact]
    public void FOW_006_brigade_sight_is_the_best_of_its_sections()
    {
        var fog = new FogOfWar(SectionTypeTable.Parse(Data(1, 3, 1)));

        Assert.Equal(3, fog.BrigadeSight(Of("a", "b", "c")));
    }

    [Fact]
    public void FOW_006_removing_the_best_section_leaves_the_best_of_the_rest()
    {
        var fog = new FogOfWar(SectionTypeTable.Parse(Data(1, 3, 2)));
        var brigade = Of("a", "b", "c");
        Assert.Equal(3, fog.BrigadeSight(brigade));

        brigade.RemoveSection(1);

        Assert.Equal(2, fog.BrigadeSight(brigade));
    }

    [Fact]
    public void FOW_006_a_single_section_brigade_has_that_sections_sight()
    {
        var fog = new FogOfWar(SectionTypeTable.Parse(Data(1, 3, 2)));

        Assert.Equal(2, fog.BrigadeSight(Of("c")));
    }
}
