using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class PlayCardTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":6,\"height\":4,\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[5,1],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private const string ParamText = "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1\nP-CRD-01 = 5\nP-TRN-01 = 1\nP-CRD-02 = 1";

    // alpha costs 1 CP, bravo 2, gamma 3 and is single-use (test-cards.json). Bravo gets the effect given.
    private static CardTable Cards(ICardEffect? bravo = null)
    {
        var cards = CardTable.Load(Path.Combine(AppContext.BaseDirectory, "data", "test-cards.json"));
        if (bravo != null) cards.SetEffect("bravo", bravo);
        return cards;
    }

    private static GameState NewGame(CardTable? cards = null)
        => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(ParamText), cards ?? Cards());

    private static FactionState Active(GameState g) => g.Factions.Single(f => f.Id == g.ActiveFaction);
    private static FactionState Other(GameState g) => g.Factions.Single(f => f.Id != g.ActiveFaction);

    private static GameState Setup(string[] hand, int cp, CardTable? cards = null)
    {
        var g = NewGame(cards);
        var f = Active(g);
        f._hand.Clear();
        f._hand.AddRange(hand);
        f.Cp = cp;
        return g;
    }

    private static CommandResult Play(GameState g, string card, string target = "") => g.ApplyCommand(new PlayCardCommand(g.ActiveFaction, card, target));

    private static void AssertRejectedUnchanged(GameState g, ICommand c)
    {
        var (hash, logged) = (g.ComputeHash(), g.Log.Count);
        var r = g.ApplyCommand(c);
        Assert.False(r.Accepted);
        Assert.False(string.IsNullOrEmpty(r.Reason));
        Assert.Equal(hash, g.ComputeHash());
        Assert.Equal(logged, g.Log.Count);
    }

    private sealed class Condition : ICardEffect
    {
        public bool Holds;
        public string? SeenTarget;
        public int Applied;
        public string? CanPlay(GameState state, FactionState player, string target) => Holds ? null : "The condition does not hold";
        public void Apply(GameState state, FactionState player, string target) { Applied++; SeenTarget = target; }
    }

    private sealed class Spy : ICardEffect
    {
        public int? CpSeen;
        public bool? InHandSeen;
        public bool? InDiscardSeen;
        public string? CanPlay(GameState state, FactionState player, string target) => null;
        public void Apply(GameState state, FactionState player, string target)
        {
            CpSeen = player.Cp;
            InHandSeen = player.Hand.Contains("bravo");
            InDiscardSeen = player.Discard.Contains("bravo");
        }
    }

    [Fact]
    public void CRD_010_RES_006_CP_is_spent_and_the_card_has_left_the_hand_before_the_effect_runs()
    {
        var spy = new Spy();
        var g = Setup(new[] { "bravo" }, 3, Cards(spy));
        Assert.True(Play(g, "bravo").Accepted);
        Assert.Equal(1, spy.CpSeen);
        Assert.False(spy.InHandSeen);
        Assert.True(spy.InDiscardSeen);
    }

    [Fact]
    public void TRN_002_the_active_faction_plays_a_card_from_its_hand_in_the_actions_phase()
    {
        var g = Setup(new[] { "alpha" }, 3);
        Assert.Equal(TurnPhase.Actions, g.Phase);
        Assert.True(Play(g, "alpha").Accepted);
    }

    [Fact]
    public void TRN_002_another_faction_is_rejected_with_the_state_unchanged()
    {
        var g = Setup(new[] { "alpha" }, 3);
        var other = Other(g);
        other._hand.Add("alpha");
        other.Cp = 3;
        AssertRejectedUnchanged(g, new PlayCardCommand(other.Id, "alpha"));
    }

    [Fact]
    public void CRD_008_TRN_002_a_card_played_in_any_phase_but_actions_is_rejected_with_the_state_unchanged()
    {
        foreach (var phase in Enum.GetValues<TurnPhase>().Where(p => p != TurnPhase.Actions))
        {
            var g = Setup(new[] { "alpha" }, 3);
            g.Phase = phase;
            AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "alpha"));
        }
    }

    [Fact]
    public void CRD_001_RES_006_a_card_costing_2_with_3_CP_leaves_1_CP()
    {
        var g = Setup(new[] { "bravo" }, 3);
        Assert.True(Play(g, "bravo").Accepted);
        Assert.Equal(1, Active(g).Cp);
    }

    [Fact]
    public void CRD_001_RES_006_a_card_costing_2_with_1_CP_is_rejected_stays_in_hand_and_CP_is_unchanged()
    {
        var g = Setup(new[] { "bravo" }, 1);
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "bravo"));
        Assert.Equal(new[] { "bravo" }, Active(g).Hand);
        Assert.Equal(1, Active(g).Cp);
    }

    [Fact]
    public void CRD_001_a_card_not_in_the_issuers_hand_is_rejected()
    {
        var g = Setup(new[] { "alpha" }, 9);
        var deckCard = Active(g).Deck.First(c => c != "alpha");
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "bravo"));   // in no hand
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, deckCard));  // in the deck, not the hand
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "nonesuch"));
        Other(g)._hand.Add("bravo");                                                 // in the other Faction's hand
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "bravo"));
    }

    [Fact]
    public void CRD_001_an_accepted_card_that_is_not_single_use_leaves_the_hand_and_goes_to_the_discard_pile()
    {
        var g = Setup(new[] { "alpha", "bravo" }, 3);
        Assert.True(Play(g, "alpha").Accepted);
        Assert.Equal(new[] { "bravo" }, Active(g).Hand);
        Assert.Equal(new[] { "alpha" }, Active(g).Discard);
    }

    [Fact]
    public void CRD_009_all_3_cards_in_hand_and_a_second_copy_can_be_played_in_one_turn()
    {
        var g = Setup(new[] { "alpha", "alpha", "bravo" }, 10);
        Assert.True(Play(g, "alpha").Accepted);
        Assert.True(Play(g, "alpha").Accepted);   // a second copy of a Card already played this turn
        Assert.True(Play(g, "bravo").Accepted);
        Assert.Empty(Active(g).Hand);
        Assert.Equal(new[] { "alpha", "alpha", "bravo" }, Active(g).Discard);
        Assert.Equal(10 - 1 - 1 - 2, Active(g).Cp);
    }

    [Fact]
    public void CRD_010_a_card_whose_play_condition_fails_is_rejected_spends_no_CP_and_stays_in_hand()
    {
        var effect = new Condition { Holds = false };
        var g = Setup(new[] { "bravo" }, 5, Cards(effect));
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "bravo"));
        Assert.Equal(5, Active(g).Cp);
        Assert.Equal(new[] { "bravo" }, Active(g).Hand);
        Assert.Equal(0, effect.Applied);
    }

    [Fact]
    public void CRD_010_a_card_whose_play_condition_holds_is_accepted_and_its_effect_gets_the_target()
    {
        var effect = new Condition { Holds = true };
        var g = Setup(new[] { "bravo" }, 5, Cards(effect));
        Assert.True(Play(g, "bravo", "hq-east").Accepted);
        Assert.Equal(1, effect.Applied);
        Assert.Equal("hq-east", effect.SeenTarget);
        Assert.Equal(3, Active(g).Cp);
    }

    [Fact]
    public void SUP_014_MAP_011_a_single_use_card_is_in_no_pile_and_never_comes_back_after_a_reshuffle()
    {
        var g = Setup(new[] { "gamma", "alpha" }, 9);
        var f = Active(g);
        f._deck.RemoveAll(c => c == "gamma");         // this hand holds the only gamma
        Assert.True(Play(g, "gamma").Accepted);
        Assert.True(Play(g, "alpha").Accepted);
        Assert.DoesNotContain("gamma", f.Hand);
        Assert.DoesNotContain("gamma", f.Deck);
        Assert.DoesNotContain("gamma", f.Discard);
        Assert.Equal(new[] { "alpha" }, f.Discard);
        // Use the deck up, then let the Draw step reshuffle the discard pile (CRD-015) on this Faction's next turn.
        f._deck.Clear();
        End(g);
        End(g);
        Assert.Empty(f.Discard);                      // the pile went back into the deck
        Assert.Equal(new[] { "alpha" }, f.Hand);      // and its one Card was drawn
        Assert.DoesNotContain("gamma", f.Deck.Concat(f.Hand).Concat(f.Discard));
    }

    [Fact]
    public void SUP_014_MAP_011_a_card_not_flagged_single_use_comes_back_after_the_reshuffle()
    {
        var g = Setup(new[] { "alpha" }, 9);
        var f = Active(g);
        Assert.True(Play(g, "alpha").Accepted);
        f._deck.Clear();
        End(g);
        End(g);                                       // the Draw step reshuffles the discard pile and draws its one Card
        Assert.Empty(f.Discard);
        Assert.Equal(new[] { "alpha" }, f.Hand);
    }

    [Fact]
    public void DLV_013_an_accepted_play_is_logged_both_round_trip_through_json_and_replay_gives_the_same_hash()
    {
        var registry = CommandRegistry.CreateDefault();
        var g = Setup(new[] { "alpha", "bravo" }, 2);
        var rejected = new PlayCardCommand(g.ActiveFaction, "bravo", "hq-east");
        var accepted = new PlayCardCommand(g.ActiveFaction, "alpha");
        Assert.False(g.ApplyCommand(new PlayCardCommand(g.ActiveFaction, "gamma")).Accepted);
        Assert.True(g.ApplyCommand(accepted).Accepted);
        Assert.Equal(accepted, g.Log.Entries.Single().Command);   // the core logs accepted commands only (see ApplyCommand)
        Assert.False(g.ApplyCommand(rejected).Accepted);          // 1 CP left for a 2 CP Card
        Assert.Single(g.Log.Entries);

        Assert.Equal(accepted, registry.ReadCommand(registry.WriteCommand(accepted)));
        Assert.Equal(rejected, registry.ReadCommand(registry.WriteCommand(rejected)));
        Assert.Contains("\"target\":\"hq-east\"", registry.WriteCommand(rejected));
        Assert.DoesNotContain("target", registry.WriteCommand(accepted));
    }

    [Fact]
    public void DLV_013_TEC_008_a_log_with_plays_replays_from_the_seed_to_the_same_hash()
    {
        // CP is 0 at the start of turn 1, so the first play is on the second turn, after income.
        var registry = CommandRegistry.CreateDefault();
        var g = NewGame();
        End(g);
        End(g);
        var f = Active(g);
        Assert.True(f.Cp >= 1);
        var cheap = f.Hand.First(c => c == "alpha");
        Assert.True(g.ApplyCommand(new PlayCardCommand(f.Id, cheap)).Accepted);
        End(g);

        var run = registry.ReadLog(registry.WriteLog(7, g.Log));
        var replayed = GameState.Replay(run.Seed, run.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(ParamText), Cards());
        Assert.Equal(g.ComputeHash(), replayed.ComputeHash());
        Assert.Contains(g.Log.Entries, e => e.Command is PlayCardCommand);
    }

    [Fact]
    public void DLV_013_a_play_card_command_with_a_bad_field_is_a_read_error()
    {
        var registry = CommandRegistry.CreateDefault();
        Assert.Throws<WeirdWar.Core.Serialization.JsonException>(() => registry.ReadCommand("{\"type\":\"playCard\"}"));
        Assert.Throws<WeirdWar.Core.Serialization.JsonException>(() => registry.ReadCommand("{\"type\":\"playCard\",\"card\":3}"));
        Assert.Throws<WeirdWar.Core.Serialization.JsonException>(() => registry.ReadCommand("{\"type\":\"playCard\",\"card\":\"alpha\",\"target\":1}"));
    }

    [Fact]
    public void TRN_002_a_game_without_cards_rejects_a_play()
    {
        var g = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(ParamText));
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "alpha"));
    }

    [Fact]
    public void DLV_014_a_card_with_no_CP_value_fails_to_load()
    {
        Assert.Throws<CardFormatException>(() => CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"pool\":\"Core\",\"copies\":1}}}"));
    }

    private static void End(GameState g) => Assert.True(g.ApplyCommand(new EndTurnCommand(g.ActiveFaction!)).Accepted);
}
