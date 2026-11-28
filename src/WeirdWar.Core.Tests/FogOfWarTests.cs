using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class FogOfWarTests
{
    // Seed 7 gives west the first turn. Each Faction starts with three "a" Sections.
    private const string MapJson =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}},\"west\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}}}}";

    private const string Params =
        "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\n" +
        "P-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1";

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
        var types = SectionTypeTable.Parse(Data(1, 3, 2));
        var fog = new FogOfWar(types);
        var game = new GameState(7, MapImporter.Parse(MapJson), types, ParameterTable.Parse(Params));
        var source = new Brigade(game.TakeBrigadeId(), "west", 40, HexCoord.FromOffset(3, 3), new[] { "a", "b", "c" }.Select(t => new Section(t, 9)).ToList());
        var target = new Brigade(game.TakeBrigadeId(), "west", 40, HexCoord.FromOffset(4, 3), new[] { "a" }.Select(t => new Section(t, 9)).ToList());
        game.AddBrigade(source);
        game.AddBrigade(target);
        Assert.Equal(3, fog.BrigadeSight(source));

        Assert.True(game.ApplyCommand(new TransferSectionCommand("west", source.Id, 1, target.Id)).Accepted);

        Assert.Equal(2, fog.BrigadeSight(source));
    }

    [Fact]
    public void FOW_006_a_single_section_brigade_has_that_sections_sight()
    {
        var fog = new FogOfWar(SectionTypeTable.Parse(Data(1, 3, 2)));

        Assert.Equal(2, fog.BrigadeSight(Of("c")));
    }
}
