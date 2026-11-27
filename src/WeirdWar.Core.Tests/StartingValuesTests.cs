using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

/// <summary>CC1-94: the starting reserve, CP and Brigade SP come from parameters, not from the map (RES-013, BRG-036).</summary>
public class StartingValuesTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private static string MapJson(string setup = "{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
                                                   "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}")
        => "{\"version\":1,\"width\":6,\"height\":4,\"factions\":[\"east\",\"west\"],\"locations\":[" +
           "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[5,1],\"faction\":\"east\"}," +
           "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}],\"setup\":" + setup + "}";

    private static string Params(string reserve = "5", string cp = "0", string sp = "90")
        => "P-BRG-05 = 3\nP-BRG-09 = " + sp + "\nP-RES-05 = " + reserve + "\nP-RES-06 = " + cp +
           "\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1" +
           "\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2";

    private static GameState NewGame(string parameters, string? map = null)
        => new GameState(7, MapImporter.Parse(map ?? MapJson()), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters));

    /// <summary>The Faction that is not paid yet on turn 1 shows the bare starting values (TRN-001).</summary>
    private static FactionState Waiting(GameState g) => g.Factions.Single(f => f.Id != g.ActiveFaction);

    private static FactionState Active(GameState g) => g.Factions.Single(f => f.Id == g.ActiveFaction);

    [Fact]
    public void RES_013_each_faction_starts_with_the_reserve_in_p_res_05()
    {
        var game = NewGame(Params());

        Assert.Equal(5, Waiting(game).Reserve);
        Assert.Equal(5 + 5, Active(game).Reserve); // plus turn 1 income (P-RES-01)
    }

    [Fact]
    public void RES_013_each_faction_starts_with_the_cp_in_p_res_06()
    {
        var game = NewGame(Params());

        Assert.Equal(0, Waiting(game).Cp);
        Assert.Equal(0 + 2, Active(game).Cp); // plus turn 1 income (P-RES-02)
    }

    [Fact]
    public void BRG_036_each_starting_brigade_has_the_sp_in_p_brg_09()
    {
        var game = NewGame(Params());

        Assert.Equal(2, game.Brigades.Count);
        Assert.All(game.Brigades, b => Assert.Equal(90, b.SP));
    }

    [Fact]
    public void RES_013_BRG_036_the_three_values_follow_the_data_and_are_not_hard_coded()
    {
        var game = NewGame(Params(reserve: "8", cp: "3", sp: "60"));

        Assert.Equal(8, Waiting(game).Reserve);
        Assert.Equal(3, Waiting(game).Cp);
        Assert.Equal(8 + 5, Active(game).Reserve);
        Assert.Equal(3 + 2, Active(game).Cp);
        Assert.All(game.Brigades, b => Assert.Equal(60, b.SP));
    }

    [Theory]
    [InlineData("P-RES-05")]
    [InlineData("P-RES-06")]
    [InlineData("P-BRG-09")]
    public void DLV_014_a_missing_starting_parameter_fails_when_the_game_starts(string id)
    {
        var parameters = string.Join("\n", Params().Split('\n').Where(l => !l.StartsWith(id + " ")));

        var ex = Assert.Throws<MissingParameterException>(() => NewGame(parameters));
        Assert.Equal(id, ex.ParameterId);
    }

    [Theory]
    [InlineData("-1", "0", "90", "P-RES-05")]
    [InlineData("5", "-1", "90", "P-RES-06")]
    [InlineData("5", "0", "-1", "P-BRG-09")]
    public void DLV_014_a_negative_starting_value_fails_when_the_game_starts(string reserve, string cp, string sp, string id)
    {
        var ex = Assert.Throws<ParameterFormatException>(() => NewGame(Params(reserve, cp, sp)));
        Assert.Contains(id, ex.Message);
    }

    [Theory]
    [InlineData("{\"east\":{\"reserve\":5,\"brigade\":{\"sections\":[\"infantry\"]}},\"west\":{\"brigade\":{\"sections\":[\"infantry\"]}}}", "reserve")]
    [InlineData("{\"east\":{\"brigade\":{\"sp\":90,\"sections\":[\"infantry\"]}},\"west\":{\"brigade\":{\"sections\":[\"infantry\"]}}}", "sp")]
    public void MAP_021_a_setup_with_reserve_or_brigade_sp_is_rejected_as_an_unknown_key(string setup, string key)
    {
        var e = Assert.Throws<MapFormatException>(() => MapImporter.Parse(MapJson(setup)));

        Assert.Contains("unknown key \"" + key + "\"", e.Message);
    }

    [Fact]
    public void MAP_021_a_setup_with_only_sections_imports()
    {
        var map = MapImporter.Parse(MapJson());

        Assert.Equal(2, map.Setup.Count);
        Assert.All(map.Setup, s => Assert.Equal(3, s.SectionTypeIds.Count));
    }

    [Fact]
    public void RES_013_BRG_036_the_shipped_data_holds_5_0_and_90_as_decided_values()
    {
        var shipped = ParameterTable.LoadShipped();

        Assert.Equal(5, shipped.GetInt("P-RES-05"));
        Assert.Equal(0, shipped.GetInt("P-RES-06"));
        Assert.Equal(90, shipped.GetInt("P-BRG-09"));
        foreach (var id in new[] { "P-RES-05", "P-RES-06", "P-BRG-09" })
            Assert.False(shipped.IsPlaceholder(id), id);
    }

    [Fact]
    public void TEC_008_the_hash_is_stable_across_replay_and_follows_the_starting_values()
    {
        var game = NewGame(Params());
        Assert.True(game.ApplyCommand(new EndTurnCommand(game.ActiveFaction!)).Accepted);
        var replayed = GameState.Replay(game.Seed, game.Log, MapImporter.Parse(MapJson()), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params()));

        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.NotEqual(NewGame(Params()).ComputeHash(), NewGame(Params(reserve: "8")).ComputeHash());
        Assert.NotEqual(NewGame(Params()).ComputeHash(), NewGame(Params(sp: "60")).ComputeHash());
    }
}
