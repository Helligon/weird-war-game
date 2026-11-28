using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class BridgeTests
{
    // 4 columns x 10 rows. West HQ (0,0), east HQ (3,9). The west column crosses rivers at (0,3) (a bridge in the file) and (0,7) (none).
    // Mountain (1,0) and sea (1,1). Seed 7 gives west the first turn.
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private static string MapJson(string? bridges = "[[0,3]]") =>
        "{\"version\":1,\"width\":4,\"height\":10," +
        "\"terrain\":{\"mountain\":[[1,0]],\"sea\":[[1,1]],\"river\":[[0,3],[0,7]]}," +
        (bridges == null ? "" : "\"bridges\":" + bridges + ",") +
        "\"factions\":[\"east\",\"west\"]," +
        "\"locations\":[{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[3,9],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}]," +
        "\"setup\":{" +
        "\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private const string Params = "P-BRG-01 = 5\nP-BRG-06 = 1\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1";

    /// <summary>Test-only command standing in for the Bridgelayer Card (CD1-152): builds a bridge through the internal state method.</summary>
    private sealed class BuildBridgeCommand : ICommand
    {
        private readonly HexCoord _hex;
        public BuildBridgeCommand(HexCoord hex) { _hex = hex; }
        public string Type => "testBridge";
        public string Issuer => "west";
        public TurnPhase? RequiredPhase => TurnPhase.Actions;
        public string? Validate(GameState state) => state.CheckBridge(_hex);
        public void Apply(GameState state) => state.AddBridge(_hex);
        public Dictionary<string, object?> ToJson()
        {
            var (c, r) = _hex.ToOffset();
            return new Dictionary<string, object?> { ["faction"] = "west", ["hex"] = new List<object?> { (long)c, (long)r } };
        }
        public override bool Equals(object? o) => o is BuildBridgeCommand b && b._hex == _hex;
        public override int GetHashCode() => _hex.GetHashCode();
    }

    private static GameState NewGame(string? bridges = "[[0,3]]") =>
        new GameState(7, MapImporter.Parse(MapJson(bridges)), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params));

    private static Brigade West(GameState g) => g.Brigades.First(b => b.Faction == "west");
    private static Brigade East(GameState g) => g.Brigades.First(b => b.Faction == "east");
    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);
    private static CommandResult Move(GameState g, params HexCoord[] path) => g.ApplyCommand(new MoveBrigadeCommand("west", West(g).Id, path));

    // ---- TRN-024

    [Fact]
    public void TRN_024_a_river_hex_without_a_bridge_is_still_rejected()
    {
        var game = NewGame();
        West(game).Position = At(0, 6);
        var hash = game.ComputeHash();

        var result = Move(game, At(0, 7));

        Assert.False(result.Accepted);
        Assert.Contains("river", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
    }

    [Fact]
    public void TRN_024_mountain_and_sea_stay_impassable_and_cannot_carry_a_bridge()
    {
        var game = NewGame();
        West(game).Position = At(0, 1);
        var hash = game.ComputeHash();
        Assert.False(Move(game, At(1, 1)).Accepted);
        Assert.Equal(hash, game.ComputeHash());
        West(game).Position = At(0, 0);
        hash = game.ComputeHash();
        Assert.False(Move(game, At(1, 0)).Accepted);
        Assert.Equal(hash, game.ComputeHash());

        Assert.Throws<MapFormatException>(() => MapImporter.Parse(MapJson("[[1,0]]")));
        Assert.Throws<MapFormatException>(() => MapImporter.Parse(MapJson("[[1,1]]")));
        Assert.NotNull(game.CheckBridge(At(1, 0)));
        Assert.NotNull(game.CheckBridge(At(1, 1)));
    }

    // ---- MAP-054, TRN-024

    [Fact]
    public void MAP_054_a_river_hex_with_a_bridge_can_be_moved_onto_and_through()
    {
        var onto = NewGame();
        Assert.True(Move(onto, At(0, 1), At(0, 2), At(0, 3)).Accepted);
        Assert.Equal(At(0, 3), West(onto).Position);

        var through = NewGame();
        West(through).Position = At(0, 2);
        Assert.True(Move(through, At(0, 3), At(0, 4)).Accepted);
        Assert.Equal(At(0, 4), West(through).Position);
    }

    [Fact]
    public void MAP_054_a_bridge_costs_the_same_as_an_open_hex()
    {
        var game = NewGame();
        West(game).Position = At(0, 2);
        var before = West(game).SP;

        Assert.True(Move(game, At(0, 3)).Accepted);

        Assert.Equal(before - 3, West(game).SP);
    }

    [Fact]
    public void MAP_054_a_bridge_built_in_play_makes_the_river_hex_passable()
    {
        var game = NewGame();
        West(game).Position = At(0, 6);
        var hash = game.ComputeHash();
        Assert.False(Move(game, At(0, 7)).Accepted);
        Assert.Equal(hash, game.ComputeHash());

        Assert.True(game.ApplyCommand(new BuildBridgeCommand(At(0, 7))).Accepted);

        Assert.True(game.HasBridge(At(0, 7)));
        Assert.True(Move(game, At(0, 7)).Accepted);
    }

    // ---- MAP-054: the map file

    [Fact]
    public void MAP_054_a_map_file_that_lists_a_bridge_on_a_river_hex_imports_with_the_bridge()
    {
        var map = MapImporter.Parse(MapJson());

        Assert.Equal(new[] { At(0, 3) }, map.Bridges);
        Assert.Equal(TerrainType.River, map.TerrainAt(At(0, 3)));
        Assert.True(NewGame().HasBridge(At(0, 3)));
        Assert.False(NewGame().HasBridge(At(0, 7)));
    }

    [Fact]
    public void MAP_054_a_map_without_bridges_has_none()
    {
        Assert.Empty(MapImporter.Parse(MapJson(null)).Bridges);
        Assert.Empty(NewGame(null).Bridges);
    }

    [Theory]
    [InlineData("[[2,2]]", "not river")]
    [InlineData("[[0,3],[0,3]]", "more than once")]
    [InlineData("[[9,9]]", "outside")]
    [InlineData("[[0]]", "two whole numbers")]
    [InlineData("[[0.5,3]]", "two whole numbers")]
    [InlineData("{}", "list")]
    public void MAP_054_a_bad_bridge_entry_is_rejected_with_a_message(string bridges, string text)
    {
        var e = Assert.Throws<MapFormatException>(() => MapImporter.Parse(MapJson(bridges)));
        Assert.Contains(text, e.Message);
    }

    [Fact]
    public void MAP_054_bridges_listed_in_any_order_give_the_same_map()
    {
        var a = MapImporter.Parse(MapJson("[[0,3],[0,7]]"));
        var b = MapImporter.Parse(MapJson("[[0,7],[0,3]]"));
        Assert.Equal(a.Digest(), b.Digest());
        Assert.Equal(new[] { At(0, 3), At(0, 7) }, a.Bridges);
    }

    [Fact]
    public void MAP_054_a_bridge_is_not_a_location()
    {
        var game = NewGame();
        var bridge = At(0, 3);

        Assert.Equal(2, game.Map!.Locations.Count);
        Assert.Equal(2, game.Locations.Count);
        Assert.DoesNotContain(game.Locations, l => l.Position == bridge);
        // A location on the bridge hex stays an error: it is a river hex (MED-007).
        var withLocation = MapJson().Replace("\"locations\":[", "\"locations\":[{\"id\":\"st\",\"type\":\"supply\",\"at\":[0,3]},");
        Assert.Throws<MapFormatException>(() => MapImporter.Parse(withLocation));
    }

    // ---- MAP-054: the hash

    [Fact]
    public void MAP_054_two_states_that_differ_only_by_a_bridge_have_different_hashes()
    {
        var without = NewGame("[]");
        var with = NewGame("[[0,3]]");

        Assert.NotEqual(without.ComputeHash(), with.ComputeHash());
        Assert.DoesNotContain("bridge=", without.CanonicalText());
        Assert.Contains("bridge=0,3\n", with.CanonicalText());
    }

    [Fact]
    public void MAP_054_a_bridge_added_to_the_state_changes_the_hash()
    {
        var a = NewGame();
        var b = NewGame();
        b.AddBridge(At(0, 7));

        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
        Assert.Contains("bridge=0,3\nbridge=0,7\n", b.CanonicalText());
    }

    [Fact]
    public void MAP_054_a_bridge_built_by_a_command_replays_to_the_same_hash()
    {
        var map = MapImporter.Parse(MapJson());
        var types = SectionTypeTable.Parse(TypesJson);
        var parameters = ParameterTable.Parse(Params);
        var game = new GameState(7, map, types, parameters);
        Assert.True(game.ApplyCommand(new BuildBridgeCommand(At(0, 7))).Accepted);
        Assert.True(Move(game, At(0, 1), At(0, 2), At(0, 3)).Accepted);

        var replayed = GameState.Replay(7, game.Log, map, types, parameters);

        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.True(replayed.HasBridge(At(0, 7)));
    }

    [Fact]
    public void MAP_054_a_rejected_bridge_command_leaves_the_hash_unchanged()
    {
        var game = NewGame();
        var hash = game.ComputeHash();

        Assert.False(game.ApplyCommand(new BuildBridgeCommand(At(2, 2))).Accepted);   // open hex
        Assert.False(game.ApplyCommand(new BuildBridgeCommand(At(0, 3))).Accepted);   // already bridged
        Assert.False(game.ApplyCommand(new BuildBridgeCommand(At(1, 0))).Accepted);   // mountain
        Assert.False(game.ApplyCommand(new BuildBridgeCommand(At(7, 7))).Accepted);   // off the map

        Assert.Equal(hash, game.ComputeHash());
        Assert.Throws<InvalidOperationException>(() => game.AddBridge(At(2, 2)));
        Assert.Equal(hash, game.ComputeHash());
    }

    [Fact]
    public void MAP_054_the_hash_of_a_state_with_a_file_bridge_and_a_built_bridge_is_pinned()
    {
        // Pinned values computed independently in Python (scratchpad pin144.py), not from this code.
        var fileOnly = NewGame();
        Assert.Equal("9f3f1985f441f96eb953d9345ded51a8e7e1a11b976aa334381fd638012a9344", fileOnly.ComputeHash());

        var built = NewGame();
        Assert.True(built.ApplyCommand(new BuildBridgeCommand(At(0, 7))).Accepted);
        Assert.Equal("070366eb43d5c8f64707ea6631bfaed1836f733419d9d55ac84c1360962793c8", built.ComputeHash());
    }

    // ---- MAP-054: contact and battle

    [Fact]
    public void MAP_054_entering_a_bridge_next_to_an_enemy_starts_a_battle_as_on_any_hex()
    {
        var game = NewGame();
        East(game).Position = At(0, 4);

        Assert.True(Move(game, At(0, 1), At(0, 2), At(0, 3)).Accepted);

        Assert.Equal(At(0, 3), West(game).Position);
        var battle = Assert.Single(game.Battles);
        Assert.Equal(West(game).Id, battle.AttackerId);
        Assert.Equal(East(game).Id, battle.DefenderId);
    }

    [Fact]
    public void MAP_054_a_brigade_on_a_bridge_can_attack_an_adjacent_enemy()
    {
        var game = NewGame();
        West(game).Position = At(0, 3);
        East(game).Position = At(0, 4);

        Assert.True(Move(game, At(0, 4)).Accepted);

        Assert.Equal(At(0, 3), West(game).Position);
        Assert.Single(game.Battles);
    }

    [Fact]
    public void MAP_054_an_enemy_brigade_on_a_bridge_blocks_it_and_can_be_attacked()
    {
        var game = NewGame();
        West(game).Position = At(0, 2);
        East(game).Position = At(0, 3);
        var hash = game.ComputeHash();

        Assert.False(Move(game, At(0, 3), At(0, 4)).Accepted); // blocked on the way through (TRN-023)
        Assert.Equal(hash, game.ComputeHash());

        Assert.True(Move(game, At(0, 3)).Accepted);            // a one-hex order is an attack (TRN-036)
        Assert.Single(game.Battles);
    }
}
