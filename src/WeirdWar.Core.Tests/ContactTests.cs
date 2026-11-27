using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class ContactTests
{
    // Same 4 x 10 map as MovementTests. West HQ at (0,0), east HQ at (3,9). Seed 7 gives west the first turn.
    // Every Section costs 1 SP per hex, so a Brigade costs 3 per hex.
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}," +
        "\"thrall\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1,\"slow\":true}}}";

    private static string MapJson(string westSections) =>
        "{\"version\":1,\"width\":4,\"height\":10," +
        "\"terrain\":{\"mountain\":[[1,0]],\"sea\":[[0,6]],\"river\":[[2,2]]}," +
        "\"factions\":[\"east\",\"west\"]," +
        "\"locations\":[{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[3,9],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}]," +
        "\"setup\":{" +
        "\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[" + westSections + "]}}}}";

    private const string Params = "P-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1";

    private static ParameterTable Table(string parameters = Params) => ParameterTable.Parse(parameters);

    private static GameState NewGame(string west = "infantry,infantry,infantry", string parameters = Params)
    {
        var quoted = string.Join(",", west.Split(',').Select(t => "\"" + t + "\""));
        var game = new GameState(7, MapImporter.Parse(MapJson(quoted)), SectionTypeTable.Parse(TypesJson), Table(parameters));
        Assert.Equal("west", game.ActiveFaction);
        return game;
    }

    private static Brigade West(GameState game) => game.Brigades.First(b => b.Faction == "west");
    private static Brigade East(GameState game) => game.Brigades.First(b => b.Faction == "east");
    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);
    private static HexCoord[] Down(int n, int fromRow = 0) => Enumerable.Range(fromRow + 1, n).Select(r => At(0, r)).ToArray();

    private static CommandResult Move(GameState game, params HexCoord[] path) => game.ApplyCommand(new MoveBrigadeCommand("west", West(game).Id, path));
    private static CommandResult MoveDown(GameState game, int n, int fromRow = 0) => Move(game, Down(n, fromRow));

    /// <summary>East stands at (0,row) in front of west's start. Set directly: the state has no command that places a Brigade.</summary>
    private static GameState EastAt(int row, string west = "infantry,infantry,infantry", string parameters = Params)
    {
        var game = NewGame(west, parameters);
        East(game).Position = At(0, row);
        return game;
    }

    private static void AddFriend(GameState game, int row)
        => ((List<Brigade>)game.Brigades).Add(new Brigade(3, "west", 40, At(0, row), new List<Section> { new Section("infantry", 10) }));

    // ---- TRN-003, TRN-008: contact stops the move

    [Fact]
    public void TRN_003_entering_a_hex_next_to_an_enemy_brigade_stops_the_brigade_and_starts_a_battle_naming_both()
    {
        var game = EastAt(3);

        Assert.True(MoveDown(game, 2).Accepted);

        Assert.Equal(At(0, 2), West(game).Position);
        var battle = Assert.Single(game.Battles);
        Assert.Equal(West(game).Id, battle.AttackerId);
        Assert.Equal(East(game).Id, battle.DefenderId);
    }

    [Fact]
    public void TRN_008_the_rest_of_the_path_is_not_walked_and_sp_is_paid_only_for_the_hexes_walked()
    {
        var game = EastAt(3);

        // A valid hex after the contact hex: it is not walked.
        var beyond = At(0, 2).Neighbours().First(n => n != At(0, 1) && n != At(0, 3) && n != At(1, 0)
            && n.ToOffset().Column >= 0 && n.ToOffset().Column < 4 && n.ToOffset().Row >= 0 && n.ToOffset().Row < 10);
        Assert.True(Move(game, At(0, 1), At(0, 2), beyond).Accepted);

        Assert.Equal(At(0, 2), West(game).Position);
        Assert.Equal(90 - 2 * 3, West(game).SP);
        Assert.Equal(2, West(game).HexesMovedIn(game.TurnNumber));
    }

    [Fact]
    public void TRN_023_a_bad_hex_after_the_contact_hex_still_rejects_the_whole_order()
    {
        var game = EastAt(3);
        var hash = game.ComputeHash();

        // Off the map, through the enemy hex, and onto the enemy hex: the whole ordered path is checked, then cut at contact.
        Assert.False(Move(game, At(0, 1), At(0, 2), new HexCoord(5, 5)).Accepted);
        Assert.False(Move(game, At(0, 1), At(0, 2), At(0, 3), At(0, 4)).Accepted);
        var onto = Move(game, At(0, 1), At(0, 2), At(0, 3));
        Assert.False(onto.Accepted);
        Assert.Contains("TRN-023", onto.Reason);
        Assert.Equal(hash, game.ComputeHash());
        Assert.Empty(game.Battles);
    }

    [Fact]
    public void TRN_003_a_move_that_never_enters_a_hex_next_to_an_enemy_completes_and_starts_no_battle()
    {
        var game = EastAt(8);

        Assert.True(MoveDown(game, 4).Accepted);

        Assert.Equal(At(0, 4), West(game).Position);
        Assert.Empty(game.Battles);
        Assert.DoesNotContain("battle=", game.CanonicalText());
    }

    [Fact]
    public void TRN_008_the_battle_starts_with_both_brigades_on_their_own_hexes()
    {
        var game = EastAt(3);

        MoveDown(game, 2);

        Assert.Equal(At(0, 3), East(game).Position); // the defender has not moved
        Assert.Equal(At(0, 2), West(game).Position); // the attacker is on the contact hex
        Assert.Equal(1, game.Battles.Count);
    }

    [Fact]
    public void TRN_003_the_battle_carries_no_outcome_only_the_two_brigades()
    {
        var names = typeof(PendingBattle).GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();

        Assert.Equal(new[] { "AttackerId", "DefenderId" }, names);
    }

    [Fact]
    public void SUP_025_a_slow_brigade_is_limited_by_the_whole_ordered_path_even_when_contact_would_stop_it()
    {
        var game = EastAt(2, "thrall,thrall,thrall"); // contact at (0,1) after one hex
        var hash = game.ComputeHash();

        Assert.True(MoveDown(game, 1).Accepted); // one hex: within the cap of 2, contact stops it there
        Assert.Single(game.Battles);

        // A valid 3-hex path, contact on hex 2, the third hex is not the enemy's: over the cap of 2 (answer 2), whatever contact does.
        var other = EastAt(3, "thrall,thrall,thrall");
        var otherHash = other.ComputeHash();
        var beyond = At(0, 2).Neighbours().First(n => n != At(0, 1) && n != At(0, 3) && n != At(1, 0)
            && n.ToOffset().Column >= 0 && n.ToOffset().Column < 4 && n.ToOffset().Row >= 0 && n.ToOffset().Row < 10);
        var result = Move(other, At(0, 1), At(0, 2), beyond);
        Assert.False(result.Accepted);
        Assert.Contains("P-SUP-10", result.Reason);
        Assert.Empty(other.Battles);
        Assert.Equal(otherHash, other.ComputeHash());
        Assert.NotEqual(hash, otherHash);
    }

    [Fact]
    public void TRN_036_an_attack_counts_as_zero_hexes_so_a_slow_brigade_at_its_cap_can_still_attack()
    {
        var game = EastAt(1, "thrall,thrall,thrall");
        West(game).MovedTurn = game.TurnNumber;
        West(game).MovedHexes = 2; // the Slow cap (P-SUP-10 = 2) is used up

        Assert.True(Move(game, At(0, 1)).Accepted);
        Assert.Single(game.Battles);
        Assert.Equal(2, West(game).HexesMovedIn(game.TurnNumber));
    }

    // ---- TRN-008, TRN-020: a friendly-held contact hex

    [Fact]
    public void TRN_008_a_move_that_contact_would_stop_on_a_friendly_held_hex_is_rejected_and_no_battle_starts()
    {
        var game = EastAt(3);
        AddFriend(game, 2); // (0,2) is next to the enemy at (0,3)
        var hash = game.ComputeHash();

        var result = MoveDown(game, 2);

        Assert.False(result.Accepted);
        Assert.Contains("TRN-020", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
        Assert.Empty(game.Battles);
    }

    [Fact]
    public void TRN_020_a_path_through_a_friendly_hex_that_is_not_next_to_an_enemy_is_accepted()
    {
        var game = EastAt(8);
        AddFriend(game, 2);

        Assert.True(MoveDown(game, 4).Accepted);

        Assert.Equal(At(0, 4), West(game).Position);
        Assert.Empty(game.Battles);
    }

    // ---- SUP-006, TRN-008: SP and contact

    [Fact]
    public void SUP_006_a_brigade_whose_sp_stops_it_before_the_contact_hex_stops_there_and_no_battle_starts()
    {
        var game = EastAt(4); // contact would be at (0,3)
        West(game).SP = 7;    // two hexes

        Assert.True(MoveDown(game, 3).Accepted);

        Assert.Equal(At(0, 2), West(game).Position);
        Assert.Equal(1, West(game).SP);
        Assert.Empty(game.Battles);
    }

    [Fact]
    public void SUP_006_a_brigade_whose_last_payable_hex_is_next_to_an_enemy_starts_a_battle_there()
    {
        var game = EastAt(3); // contact at (0,2)
        West(game).SP = 6;    // exactly two hexes

        Assert.True(MoveDown(game, 2).Accepted);

        Assert.Equal(At(0, 2), West(game).Position);
        Assert.Equal(0, West(game).SP);
        Assert.Single(game.Battles);
    }

    // ---- TRN-036, P-TRN-02: attack from an adjacent hex

    [Fact]
    public void TRN_036_a_move_order_at_an_adjacent_enemy_hex_starts_a_battle_at_once_and_costs_p_trn_02()
    {
        var game = EastAt(1);

        Assert.True(Move(game, At(0, 1)).Accepted);

        Assert.Equal(At(0, 0), West(game).Position);   // stays where it is
        Assert.Equal(At(0, 1), East(game).Position);
        Assert.Equal(89, West(game).SP);               // P-TRN-02 = 1
        Assert.Equal(0, West(game).HexesMovedIn(game.TurnNumber)); // 0 hexes walked
        var battle = Assert.Single(game.Battles);
        Assert.Equal((West(game).Id, East(game).Id), (battle.AttackerId, battle.DefenderId));
    }

    [Fact]
    public void TRN_036_a_brigade_with_less_sp_than_p_trn_02_cannot_attack_and_nothing_changes()
    {
        var game = EastAt(1, parameters: Params.Replace("P-TRN-02 = 1", "P-TRN-02 = 3"));
        West(game).SP = 2;
        var hash = game.ComputeHash();

        var result = Move(game, At(0, 1));

        Assert.False(result.Accepted);
        Assert.Contains("P-TRN-02", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
        Assert.Empty(game.Battles);
    }

    [Fact]
    public void TRN_036_an_order_at_an_enemy_hex_that_is_not_adjacent_is_rejected_as_before()
    {
        var game = EastAt(3);
        West(game).Position = At(0, 1); // two hexes away from the enemy
        var hash = game.ComputeHash();

        var result = Move(game, At(0, 2), At(0, 3)); // ends on the enemy hex, but not from next to it

        Assert.False(result.Accepted);
        Assert.Contains("TRN-023", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
        Assert.Empty(game.Battles);
    }

    [Fact]
    public void TRN_023_a_longer_path_through_an_adjacent_enemy_hex_is_still_rejected()
    {
        var game = EastAt(1);
        var hash = game.ComputeHash();

        var result = MoveDown(game, 3);

        Assert.False(result.Accepted);
        Assert.Contains("enemy", result.Reason);
        Assert.Equal(hash, game.ComputeHash());
    }

    [Fact]
    public void DLV_014_p_trn_02_changed_in_the_data_changes_the_cost_of_an_attack()
    {
        var game = EastAt(1, parameters: Params.Replace("P-TRN-02 = 1", "P-TRN-02 = 3"));

        Assert.True(Move(game, At(0, 1)).Accepted);

        Assert.Equal(87, West(game).SP);
    }

    [Fact]
    public void DLV_014_a_missing_p_trn_02_fails_when_the_game_starts()
    {
        Assert.Throws<MissingParameterException>(() => NewGame(parameters: Params.Replace("\nP-TRN-02 = 1", "")));
    }

    [Fact]
    public void TRN_036_a_negative_p_trn_02_fails_when_the_game_starts()
    {
        Assert.Throws<ParameterFormatException>(() => NewGame(parameters: Params.Replace("P-TRN-02 = 1", "P-TRN-02 = -1")));
    }

    [Fact]
    public void DLV_014_the_shipped_data_has_p_trn_02_as_a_proposed_placeholder()
    {
        var shipped = ParameterTable.LoadShipped();

        Assert.True(shipped.IsPlaceholder("P-TRN-02"));
        Assert.Equal(1, shipped.GetInt("P-TRN-02"));
    }

    // ---- TRN-015: nothing moves the attacker afterwards (CC1 assumption: a Brigade in a battle neither moves nor attacks again)

    [Fact]
    public void TRN_015_the_attacker_stays_where_contact_stopped_it_and_cannot_move_or_attack_again()
    {
        var game = EastAt(3);
        MoveDown(game, 2);
        var hash = game.ComputeHash();

        Assert.False(Move(game, At(0, 1)).Accepted);   // back
        Assert.False(Move(game, At(0, 3)).Accepted);   // attack again
        Assert.Equal(At(0, 2), West(game).Position);
        Assert.Equal(hash, game.ComputeHash());
    }

    [Fact]
    public void TRN_015_the_attacker_of_an_adjacent_attack_stays_on_the_hex_it_stood_on()
    {
        var game = EastAt(1);
        Move(game, At(0, 1));

        Assert.False(Move(game, At(1, 0)).Accepted); // the battle is pending
        Assert.Equal(At(0, 0), West(game).Position);
    }

    [Fact]
    public void TRN_015_the_defender_in_a_pending_battle_does_not_move_either()
    {
        var game = EastAt(3);
        MoveDown(game, 2);
        Assert.True(game.ApplyCommand(new EndTurnCommand("west")).Accepted);
        var hash = game.ComputeHash();

        Assert.False(game.ApplyCommand(new MoveBrigadeCommand("east", East(game).Id, new[] { At(0, 4) })).Accepted);
        Assert.Equal(hash, game.ComputeHash());
    }

    // ---- DLV-013, TEC-008: replay and the hash

    [Fact]
    public void TEC_008_replaying_the_same_commands_emits_the_same_battle_and_the_same_hash()
    {
        // West walks down its column on turn 1; east then walks toward it, greedy by distance, and meets it on turn 2.
        var impassable = new[] { At(1, 0), At(0, 6), At(2, 2) };
        GameState Run()
        {
            var game = NewGame();
            Assert.True(MoveDown(game, 5).Accepted);
            Assert.True(game.ApplyCommand(new EndTurnCommand("west")).Accepted);
            var target = West(game).Position;
            var at = East(game).Position;
            var path = new List<HexCoord>();
            while (at.DistanceTo(target) > 1)
            {
                at = at.Neighbours().Where(n => n.ToOffset().Column >= 0 && n.ToOffset().Column < 4 && n.ToOffset().Row >= 0 && n.ToOffset().Row < 10 && !impassable.Contains(n))
                    .OrderBy(n => n.DistanceTo(target)).First();
                path.Add(at);
            }
            Assert.True(game.ApplyCommand(new MoveBrigadeCommand("east", East(game).Id, path)).Accepted);
            return game;
        }

        var first = Run();
        var replayed = GameState.Replay(first.Seed, first.Log, MapImporter.Parse(MapJson("\"infantry\",\"infantry\",\"infantry\"")), SectionTypeTable.Parse(TypesJson), Table());

        var battle = Assert.Single(first.Battles);
        Assert.Equal((East(first).Id, West(first).Id), (battle.AttackerId, battle.DefenderId));
        Assert.Equal(first.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(first.ComputeHash(), Run().ComputeHash());
        Assert.Equal((battle.AttackerId, battle.DefenderId), (replayed.Battles[0].AttackerId, replayed.Battles[0].DefenderId));
    }

    [Fact]
    public void TEC_008_the_hash_after_a_contact_is_pinned()
    {
        // Pinned value computed independently in Python from the canonical text format, not from this code.
        var game = EastAt(3);
        Assert.True(MoveDown(game, 2).Accepted);

        Assert.Contains("brigade=2 faction=west sp=84 sections=infantry:10,infantry:10,infantry:10 at=0,2 moved=1:2\nbattle=2,1\nturn=1", game.CanonicalText());
        Assert.Equal("c9a5010688dae7e994fbaf76a6827edc0f39f61b483a44c858046945b7932ad2", game.ComputeHash());
    }
}
