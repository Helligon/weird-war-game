using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class CaptureTerritoryTests
{
    // 12 columns x 8 rows. West HQ (0,0) in the corner, east HQ (11,7). supply-1 (0,2), command-1 (7,4), major-1 (9,1).
    // Seed 7 gives west the first turn (see TurnTests). Brigade 1 is east, brigade 2 is west.
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}," +
        "\"thrall\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1,\"slow\":true}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[0,2]}," +
        "{\"id\":\"command-1\",\"type\":\"command\",\"at\":[7,4]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[9,1]}]," +
        "\"setup\":{\"east\":{\"reserve\":50,\"brigade\":{\"sp\":90,\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"reserve\":50,\"brigade\":{\"sp\":90,\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private const string Sight = "P-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1";
    private const string Radius = "P-MAP-02 = minus=1 min=1";
    private static string Params(string sight = Sight, string radius = Radius)
        => "P-BRG-05 = 3\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\n" + sight + "\n" + radius + "\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2";

    private static GameState NewGame(string? parameters = null)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(parameters ?? Params()));

    private static LocationState Location(GameState g, string id) => g.Locations.Single(l => l.Id == id);
    private static FactionState Faction(GameState g, string id) => g.Factions.Single(f => f.Id == id);
    private static Brigade West(GameState g) => g.Brigades.Single(b => b.Faction == "west");
    private static Brigade East(GameState g) => g.Brigades.Single(b => b.Faction == "east");
    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);
    private static void EndTurn(GameState g) => Assert.True(g.ApplyCommand(new EndTurnCommand(g.ActiveFaction)).Accepted);

    private static void WestMovesTo(GameState g, params (int c, int r)[] path)
        => Assert.True(g.ApplyCommand(new MoveBrigadeCommand("west", West(g).Id, path.Select(p => At(p.c, p.r)))).Accepted);

    /// <summary>West ends its turn on supply-1; returns the game in east's turn (west's capture is pending).</summary>
    private static GameState WestEndsTurnOnSupply()
    {
        var game = NewGame();
        Assert.Equal("west", game.ActiveFaction);
        WestMovesTo(game, (0, 1), (0, 2));
        EndTurn(game);
        return game;
    }

    // ---- TRN-009

    [Fact]
    public void TRN_009_a_brigade_that_ends_its_turn_on_an_st_and_is_still_there_captures_it_at_its_next_turn_start()
    {
        var game = WestEndsTurnOnSupply();
        Assert.Null(Location(game, "supply-1").Owner);

        EndTurn(game); // east ends; west's turn starts

        Assert.Equal("west", game.ActiveFaction);
        Assert.Equal("west", Location(game, "supply-1").Owner);
        Assert.Null(Location(game, "supply-1").Capturing);
    }

    [Fact]
    public void TRN_009_a_brigade_that_moves_onto_an_st_does_not_capture_it_the_same_turn()
    {
        var game = NewGame();
        WestMovesTo(game, (0, 1), (0, 2));

        Assert.Null(Location(game, "supply-1").Owner);
        EndTurn(game); // the end of west's turn: still not captured
        Assert.Null(Location(game, "supply-1").Owner);
        Assert.Equal("east", game.ActiveFaction);
    }

    [Fact]
    public void TRN_009_a_brigade_that_has_left_the_st_by_its_next_turn_start_does_not_capture_it()
    {
        var game = WestEndsTurnOnSupply();
        // Something moved it away during the enemy's turn (a retreat, later). Placed directly: no command moves an enemy Brigade.
        West(game).Position = At(0, 3);

        EndTurn(game);

        Assert.Null(Location(game, "supply-1").Owner);
        Assert.Null(Location(game, "supply-1").Capturing); // the pending capture is dropped
    }

    [Fact]
    public void TRN_009_a_brigade_that_returns_to_the_st_starts_a_new_stay_that_must_last_a_whole_turn()
    {
        var game = WestEndsTurnOnSupply();
        West(game).Position = At(0, 3); // gone before its turn starts, so the first stay does not count
        EndTurn(game); // east ends; west's turn starts with nothing to capture
        WestMovesTo(game, (0, 2)); // back on the ST: arriving captures nothing
        Assert.Null(Location(game, "supply-1").Owner);
        Assert.Null(Location(game, "supply-1").Capturing);
        EndTurn(game); // ending the turn on it starts the new capture
        Assert.Equal("west", Location(game, "supply-1").Capturing);
    }

    [Fact]
    public void TRN_009_neutral_and_enemy_owned_sts_both_become_owned_by_the_capturing_faction()
    {
        var game = NewGame();
        Location(game, "command-1").Owner = "east"; // enemy owned
        West(game).Position = At(7, 4);              // on the enemy ST
        EndTurn(game);
        East(game).Position = At(9, 1);              // on the neutral major ST
        EndTurn(game); // west's turn: command-1 captured, east's pending
        Assert.Equal("west", Location(game, "command-1").Owner);
        EndTurn(game); // east's turn: major-1 captured
        Assert.Equal("east", Location(game, "major-1").Owner);

        var neutral = NewGame();
        West(neutral).Position = At(0, 2);
        EndTurn(neutral);
        EndTurn(neutral);
        Assert.Equal("west", Location(neutral, "supply-1").Owner);
    }

    [Fact]
    public void TRN_009_an_hq_is_not_captured_by_standing_on_it()
    {
        var game = NewGame();
        West(game).Position = At(11, 7); // on east's HQ
        EndTurn(game);
        EndTurn(game);

        Assert.Equal("east", Location(game, "hq-east").Owner);
        Assert.Null(Location(game, "hq-east").Capturing);
    }

    [Fact]
    public void TRN_009_a_brigade_ending_its_turn_on_an_st_its_faction_already_owns_starts_no_capture()
    {
        var game = NewGame();
        Location(game, "supply-1").Owner = "west";
        West(game).Position = At(0, 2);
        EndTurn(game);

        Assert.Null(Location(game, "supply-1").Capturing);
    }

    [Fact]
    public void TRN_009_a_pending_capture_belongs_to_the_faction_that_ended_its_turn_on_the_st()
    {
        // East's Brigade is on the ST after west's; only east's start resolves east's capture.
        var game = WestEndsTurnOnSupply();
        West(game).Position = At(1, 3);
        East(game).Position = At(0, 2);
        EndTurn(game); // east ends on the ST: its mark replaces west's, and west's start finds nothing to capture
        Assert.Equal("east", Location(game, "supply-1").Capturing);
        Assert.Null(Location(game, "supply-1").Owner);
        EndTurn(game);
        EndTurn(game);
        Assert.Equal("east", Location(game, "supply-1").Owner);
    }

    // ---- TRN-005

    [Fact]
    public void TRN_005_captures_resolve_before_income_so_a_new_st_pays_the_same_turn()
    {
        var game = WestEndsTurnOnSupply();
        Assert.Equal(55, Faction(game, "west").Reserve); // setup 50 + base 5 on turn 1
        EndTurn(game);

        // West's turn 3 has started: base SP 5 plus the Supply ST bonus 3 (P-RES-04), because the capture came first.
        Assert.Equal("west", Location(game, "supply-1").Owner);
        Assert.Equal(55 + 5 + 3, Faction(game, "west").Reserve);
    }

    [Fact]
    public void TRN_005_the_capture_step_runs_in_the_start_of_turn_phase_before_income()
    {
        var seen = new List<(TurnPhase Phase, string Owner)>();
        GameState? game = null;
        game = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params()),
            (phase, faction) => { if (game != null && faction == "west") seen.Add((phase, game.Locations.Single(l => l.Id == "supply-1").Owner ?? "-")); });
        WestMovesTo(game, (0, 1), (0, 2));
        EndTurn(game);
        seen.Clear();
        EndTurn(game);

        // The owner is still "-" when StartOfTurn is entered, and "west" by the time Income is entered.
        Assert.Equal(new[] { (TurnPhase.StartOfTurn, "-"), (TurnPhase.Income, "west"), (TurnPhase.Actions, "west") }, seen);
    }

    // ---- MAP-001

    [Fact]
    public void MAP_001_a_captured_st_can_be_captured_back_in_the_same_way()
    {
        var game = WestEndsTurnOnSupply();
        EndTurn(game);
        Assert.Equal("west", Location(game, "supply-1").Owner);
        West(game).Position = At(0, 3);
        EndTurn(game);
        East(game).Position = At(0, 2); // east ends its turn on west's ST
        EndTurn(game);
        Assert.Equal("west", Location(game, "supply-1").Owner); // not yet
        EndTurn(game);

        Assert.Equal("east", Location(game, "supply-1").Owner);
    }

    // ---- MAP-016, MAP-027

    /// <summary>Independent check: every on-map hex within <paramref name="radius"/> of the centre, found by brute force over the map.</summary>
    private static IEnumerable<HexCoord> Around(GameState g, HexCoord centre, int radius)
    {
        for (var c = 0; c < g.Map!.Width; c++)
            for (var r = 0; r < g.Map.Height; r++)
                if (centre.DistanceTo(At(c, r)) <= radius) yield return At(c, r);
    }

    private static List<HexCoord> Sorted(IEnumerable<HexCoord> hexes) => hexes.Distinct().OrderBy(h => h.Q).ThenBy(h => h.R).ToList();

    [Fact]
    public void MAP_016_territory_is_exactly_the_hexes_around_the_locations_the_faction_owns()
    {
        var game = NewGame();
        Location(game, "command-1").Owner = "west";
        Location(game, "major-1").Owner = "east";

        var expected = Sorted(Around(game, At(0, 0), 2).Concat(Around(game, At(7, 4), 1)));

        Assert.Equal(expected, game.Territory!.HexesOf(game, "west"));
        Assert.Equal(Sorted(Around(game, At(11, 7), 2).Concat(Around(game, At(9, 1), 1))), game.Territory.HexesOf(game, "east"));
    }

    [Fact]
    public void MAP_016_overlapping_claims_list_each_hex_once()
    {
        var game = NewGame();
        Location(game, "supply-1").Owner = "west"; // (0,2) lies inside the HQ's radius 2 around (0,0)

        var territory = game.Territory!.HexesOf(game, "west");

        Assert.Equal(territory.Count, territory.Distinct().Count());
        Assert.Equal(Sorted(Around(game, At(0, 0), 2).Concat(Around(game, At(0, 2), 1))), territory);
    }

    [Fact]
    public void MAP_027_radius_is_sight_minus_one_with_a_minimum_of_one()
    {
        var game = NewGame();

        Assert.Equal(2, game.Territory!.RadiusOf(LocationType.Hq));      // sight 3
        Assert.Equal(1, game.Territory.RadiusOf(LocationType.Major));    // sight 2
        Assert.Equal(1, game.Territory.RadiusOf(LocationType.Command));  // sight 1 - 1 = 0, minimum 1
        Assert.Equal(1, game.Territory.RadiusOf(LocationType.Supply));   // sight 1 - 1 = 0, minimum 1
    }

    [Fact]
    public void MAP_027_an_owned_st_in_the_open_claims_itself_and_its_six_neighbours()
    {
        var game = NewGame();
        Location(game, "command-1").Owner = "west"; // (7,4) is well inside the map
        Location(game, "hq-west").Owner = null;

        var territory = game.Territory!.HexesOf(game, "west");

        Assert.Equal(7, territory.Count);
        Assert.Contains(At(7, 4), territory);
    }

    [Fact]
    public void MAP_027_radius_follows_the_data_file_not_the_code()
    {
        var game = NewGame(Params("P-FOW-03 = hq=5 fob=2 major=4 command=3 supply=2", "P-MAP-02 = minus=2 min=2"));

        Assert.Equal(3, game.Territory!.RadiusOf(LocationType.Hq));
        Assert.Equal(2, game.Territory.RadiusOf(LocationType.Major));
        Assert.Equal(2, game.Territory.RadiusOf(LocationType.Command)); // 3 - 2 = 1, minimum 2
        Assert.Equal(2, game.Territory.RadiusOf(LocationType.Supply));  // 2 - 2 = 0, minimum 2
        Assert.Equal(Sorted(Around(game, At(0, 0), 3)), game.Territory.HexesOf(game, "west"));
    }

    [Fact]
    public void MAP_027_the_shipped_data_gives_the_documented_radii()
    {
        var game = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.LoadShipped());

        Assert.Equal(new[] { 2, 1, 1, 1 }, new[] { LocationType.Hq, LocationType.Major, LocationType.Command, LocationType.Supply }.Select(game.Territory!.RadiusOf));
    }

    [Fact]
    public void MAP_016_a_neutral_location_claims_no_territory_and_a_brigade_claims_none()
    {
        var game = NewGame();
        East(game).Position = At(5, 4); // a Brigade in the open, away from any location
        West(game).Position = At(7, 4); // on a neutral ST

        Assert.Equal(Sorted(Around(game, At(11, 7), 2)), game.Territory!.HexesOf(game, "east"));
        Assert.Equal(Sorted(Around(game, At(0, 0), 2)), game.Territory.HexesOf(game, "west"));
        Assert.Empty(game.Territory.HexesOf(game, "nobody"));
    }

    [Fact]
    public void MAP_016_territory_is_clipped_to_the_hexes_on_the_map()
    {
        var game = NewGame();

        var territory = game.Territory!.HexesOf(game, "west"); // HQ in the corner (0,0)

        Assert.All(territory, h => Assert.True(game.Map!.Contains(h)));
        Assert.True(territory.Count < 19);
        Assert.Equal(Sorted(Around(game, At(0, 0), 2)), territory);
    }

    [Fact]
    public void MAP_016_a_newly_captured_st_adds_its_territory()
    {
        var game = WestEndsTurnOnSupply();
        var before = game.Territory!.HexesOf(game, "west").Count;
        EndTurn(game);

        Assert.True(game.Territory.HexesOf(game, "west").Count > before);
        Assert.Equal(Sorted(Around(game, At(0, 0), 2).Concat(Around(game, At(0, 2), 1))), game.Territory.HexesOf(game, "west"));
    }

    // ---- DLV-014: parameters are read once, at game start

    [Theory]
    [InlineData("P-FOW-03")]
    [InlineData("P-MAP-02")]
    public void DLV_014_a_missing_territory_parameter_fails_when_the_game_starts(string id)
    {
        var lines = Params().Split('\n').Where(l => !l.StartsWith(id + " ")).ToArray();

        Assert.Throws<MissingParameterException>(() => NewGame(string.Join("\n", lines)));
    }

    [Theory]
    [InlineData("hq=3 fob=2 major=2 command=1")]                  // supply missing
    [InlineData("hq=3 fob=2 major=2 command=1 supply=1 extra=1")] // unknown key
    [InlineData("hq=3 hq=3 fob=2 major=2 command=1 supply=1")]    // repeated key
    [InlineData("hq=three fob=2 major=2 command=1 supply=1")]     // not a number
    [InlineData("hq=-3 fob=2 major=2 command=1 supply=1")]        // negative
    public void DLV_014_a_malformed_sight_parameter_fails_when_the_game_starts(string value)
    {
        Assert.Throws<ParameterFormatException>(() => NewGame(Params("P-FOW-03 = " + value)));
    }

    [Theory]
    [InlineData("minus=1")]
    [InlineData("minus=1 min=x")]
    [InlineData("minus=-1 min=1")]
    public void DLV_014_a_malformed_radius_parameter_fails_when_the_game_starts(string value)
    {
        Assert.Throws<ParameterFormatException>(() => NewGame(Params(radius: "P-MAP-02 = " + value)));
    }

    // ---- TEC-008: the pending capture is part of the state and the hash

    [Fact]
    public void TEC_008_the_hash_after_a_capture_is_started_is_pinned()
    {
        // Pinned value computed independently in Python from the canonical text format, not from this code.
        var game = WestEndsTurnOnSupply();

        Assert.Contains("location=supply-1 owner=- capturing=west\n", game.CanonicalText());
        Assert.Equal("204cad2a30c90fd8ca6352735681258443b3057806db9f2cefa4281c383bda01", game.ComputeHash());
    }

    [Fact]
    public void TEC_008_a_pending_capture_changes_the_hash_and_replay_reproduces_it()
    {
        var game = WestEndsTurnOnSupply();
        var plain = NewGame();
        WestMovesTo(plain, (0, 1), (0, 2));

        Assert.NotEqual(plain.ComputeHash(), game.ComputeHash());
        var replayed = GameState.Replay(7, game.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params()));
        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.Equal("west", Location(replayed, "supply-1").Capturing);
    }

    [Fact]
    public void TEC_008_a_game_with_no_pending_capture_hashes_without_a_capturing_field()
    {
        Assert.DoesNotContain("capturing", NewGame().CanonicalText());
    }
}
