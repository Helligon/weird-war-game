using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class CardTests
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

    private const string BaseParams = "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1\n";

    private static ParameterTable Params(int hand = 5, int extra = 1, int draw = 1) => ParameterTable.Parse(BaseParams + "P-CRD-01 = " + hand + "\nP-TRN-01 = " + extra + "\nP-CRD-02 = " + draw);

    private static CardTable TestCards() => CardTable.Load(Path.Combine(AppContext.BaseDirectory, "data", "test-cards.json"));

    private static GameState NewGame(ulong seed = 7, ParameterTable? p = null, CardTable? cards = null)
        => new GameState(seed, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), p ?? Params(), cards ?? TestCards());

    private static string Second(GameState g) => g.Factions.Single(f => f.Id != g.ActiveFaction).Id;

    private static FactionState Of(GameState g, string id) => g.Factions.Single(f => f.Id == id);

    private static IEnumerable<ulong> Seeds => Enumerable.Range(1, 12).Select(i => (ulong)i);

    [Fact]
    public void CRD_003_each_faction_has_a_deck_a_hand_and_an_empty_discard_pile_in_the_hash_and_text()
    {
        var game = NewGame();
        foreach (var f in game.Factions)
        {
            Assert.NotEmpty(f.Deck);
            Assert.NotEmpty(f.Hand);
            Assert.Empty(f.Discard);
            Assert.Contains("cardstate=" + f.Id + " deck=" + string.Join(",", f.Deck) + " hand=" + string.Join(",", f.Hand) + " discard=\n", game.CanonicalText());
        }
        // The hash follows the cards: other Card data gives another hash.
        var other = NewGame(7, null, CardTable.Parse("{\"version\":1,\"cards\":{\"alpha\":{\"name\":\"Alpha\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":20}}}"));
        Assert.NotEqual(game.ComputeHash(), other.ComputeHash());
    }

    [Fact]
    public void CRD_003_a_game_without_card_data_has_no_cards_and_its_text_has_no_card_lines()
    {
        var game = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), Params());
        Assert.All(game.Factions, f => { Assert.Empty(f.Deck); Assert.Empty(f.Hand); });
        Assert.DoesNotContain("cardstate=", game.CanonicalText());
    }

    [Fact]
    public void CRD_016_deck_and_hand_together_hold_every_listed_card_with_its_copies()
    {
        foreach (var seed in Seeds)
        {
            var game = NewGame(seed);
            foreach (var f in game.Factions)
            {
                var all = f.Deck.Concat(f.Hand).GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
                Assert.Equal(new Dictionary<string, int> { ["alpha"] = 3, ["bravo"] = 2, ["gamma"] = 4 }.OrderBy(k => k.Key), all.OrderBy(k => k.Key));
            }
        }
    }

    [Fact]
    public void CRD_011_every_shipped_card_is_core_and_an_unknown_pool_is_rejected_on_load()
    {
        Assert.All(CardTable.LoadShipped().All, c => Assert.Equal(CardPool.Core, c.Pool));
        Assert.All(TestCards().All, c => Assert.Equal(CardPool.Core, c.Pool));
        var e = Assert.Throws<CardFormatException>(() => CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"pool\":\"Wild\",\"cpCost\":1,\"copies\":1}}}"));
        Assert.Contains("unknown pool \"Wild\"", e.Message);
        Assert.Throws<CardFormatException>(() => CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"cpCost\":1,\"copies\":1}}}"));
    }

    [Fact]
    public void CRD_011_the_shipped_card_data_loads_with_no_cards_and_a_game_on_it_has_empty_hands()
    {
        var cards = CardTable.LoadShipped();
        Assert.Equal(0, cards.Count);
        var game = NewGame(7, null, cards);
        Assert.All(game.Factions, f => { Assert.Empty(f.Deck); Assert.Empty(f.Hand); });
    }

    [Fact]
    public void CRD_011_a_card_entry_carries_name_cp_copies_and_the_single_use_flag()
    {
        Assert.True(TestCards().TryGet("gamma", out var gamma));
        Assert.Equal(("Gamma", 3, 4, true), (gamma.Name, gamma.CpCost, gamma.Copies, gamma.SingleUse));
        Assert.True(TestCards().TryGet("alpha", out var alpha));
        Assert.False(alpha.SingleUse);
    }

    [Fact]
    public void CRD_004_P_CRD_01_the_first_player_starts_with_the_listed_hand_size()
    {
        foreach (var seed in Seeds)
        {
            var game = NewGame(seed);
            Assert.Equal(5 + 1, Of(game, game.ActiveFaction!).Hand.Count); // the first player also draws on turn 1 (CRD-005)
        }
        var three = NewGame(7, Params(hand: 3));
        Assert.Equal(3 + 1, Of(three, three.ActiveFaction!).Hand.Count);
    }

    [Fact]
    public void CRD_018_P_TRN_01_the_second_player_starts_with_extra_cards()
    {
        var game = NewGame();
        Assert.Equal(5 + 1, Of(game, game.ActiveFaction!).Hand.Count); // 5 dealt, 1 drawn on turn 1 (CRD-005)
        Assert.Equal(6, Of(game, Second(game)).Hand.Count); // 5 dealt, 1 extra, not yet drawn
        var two = NewGame(7, Params(extra: 2));
        Assert.Equal(7, Of(two, Second(two)).Hand.Count);
        Assert.Equal(5 + 1, Of(two, two.ActiveFaction!).Hand.Count);
    }

    [Fact]
    public void TRN_033_the_extra_card_goes_to_whoever_goes_second_under_each_seed()
    {
        var firsts = new HashSet<string>();
        foreach (var seed in Seeds)
        {
            var game = NewGame(seed);
            firsts.Add(game.ActiveFaction!);
            Assert.Equal(5 + 1, Of(game, game.ActiveFaction!).Hand.Count); // dealt 5, drew 1
            Assert.Equal(5 + 1, Of(game, Second(game)).Hand.Count); // dealt 5 plus the extra Card
        }
        Assert.Equal(2, firsts.Count);
    }

    [Fact]
    public void TEC_008_same_seed_gives_the_same_decks_hands_and_hash_and_other_seeds_differ()
    {
        var a = NewGame(7);
        var b = NewGame(7);
        Assert.Equal(a.ComputeHash(), b.ComputeHash());
        foreach (var f in a.Factions)
        {
            Assert.Equal(Of(b, f.Id).Deck, f.Deck);
            Assert.Equal(Of(b, f.Id).Hand, f.Hand);
        }
        var orders = Seeds.Select(s => string.Join("|", NewGame(s).Factions.Select(f => string.Join(",", f.Hand.Concat(f.Deck))))).Distinct().Count();
        Assert.True(orders > 1, "different seeds should give at least one different deck order");
    }

    [Fact]
    public void TEC_008_replay_rebuilds_the_same_cards()
    {
        var game = NewGame(7);
        Assert.True(game.ApplyCommand(new EndTurnCommand(game.ActiveFaction!)).Accepted);
        var replayed = GameState.Replay(7, game.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), Params(), TestCards());
        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
    }

    [Fact]
    public void DLV_014_a_missing_hand_size_or_extra_card_parameter_fails_the_load_naming_it()
    {
        var noHand = ParameterTable.Parse(BaseParams + "P-TRN-01 = 1");
        var noExtra = ParameterTable.Parse(BaseParams + "P-CRD-01 = 5");
        Assert.Contains("P-CRD-01", Assert.Throws<MissingParameterException>(() => NewGame(7, noHand)).Message);
        Assert.Contains("P-TRN-01", Assert.Throws<MissingParameterException>(() => NewGame(7, noExtra)).Message);
    }

    [Fact]
    public void DLV_014_a_missing_cards_per_turn_parameter_fails_the_load_naming_it()
    {
        var none = ParameterTable.Parse(BaseParams + "P-CRD-01 = 5\nP-TRN-01 = 1");
        Assert.Contains("P-CRD-02", Assert.Throws<MissingParameterException>(() => NewGame(7, none)).Message);
        var shipped = ParameterTable.LoadShipped();
        Assert.Equal(1, shipped.GetInt("P-CRD-02"));
        Assert.False(shipped.IsPlaceholder("P-CRD-02"));
    }

    [Fact]
    public void DLV_014_a_card_with_no_copies_or_no_cp_cost_is_a_load_error_naming_it()
    {
        var noCopies = Assert.Throws<CardFormatException>(() => CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"pool\":\"Core\",\"cpCost\":1}}}"));
        Assert.Contains("\"copies\"", noCopies.Message);
        var noCost = Assert.Throws<CardFormatException>(() => CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"pool\":\"Core\",\"copies\":1}}}"));
        Assert.Contains("\"cpCost\"", noCost.Message);
        Assert.Throws<CardFormatException>(() => CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":0}}}"));
    }

    [Fact]
    public void CRD_001_a_card_with_a_cp_cost_below_1_is_a_load_error()
    {
        var e = Assert.Throws<CardFormatException>(() => CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"pool\":\"Core\",\"cpCost\":0,\"copies\":1}}}"));
        Assert.Contains("cpCost must be at least 1", e.Message);
        Assert.Equal(1, CardTable.Parse("{\"version\":1,\"cards\":{\"x\":{\"name\":\"X\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":1}}}").Count);
    }

    [Theory]
    [InlineData("A\\nB")]
    [InlineData("A\\rB")]
    [InlineData("A pool=Core")]
    public void TEC_008_a_card_name_with_a_line_break_or_a_pool_marker_is_a_load_error(string escapedName)
    {
        var json = "{\"version\":1,\"cards\":{\"x\":{\"name\":\"" + escapedName + "\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":1}}}";
        Assert.Contains("name must not contain", Assert.Throws<CardFormatException>(() => CardTable.Parse(json)).Message);
    }

    [Fact]
    public void DLV_014_the_shipped_hand_size_and_extra_card_are_decided_values_without_a_placeholder_marker()
    {
        var shipped = ParameterTable.LoadShipped();
        Assert.Equal(5, shipped.GetInt("P-CRD-01"));
        Assert.Equal(1, shipped.GetInt("P-TRN-01"));
        Assert.False(shipped.IsPlaceholder("P-CRD-01"));
        Assert.False(shipped.IsPlaceholder("P-TRN-01"));
    }

    [Fact]
    public void TEC_019_the_card_state_text_has_no_floating_point_value()
    {
        var text = NewGame().CanonicalText();
        var cardLines = text.Split('\n').Where(l => l.StartsWith("cardstate=") || l.StartsWith("cards="));
        Assert.All(cardLines, l => Assert.DoesNotMatch(@"[0-9]\.[0-9]", l));
        foreach (var t in new[] { typeof(FactionState), typeof(CardType) })
            Assert.DoesNotContain(t.GetProperties(), p => p.PropertyType == typeof(double) || p.PropertyType == typeof(float) || p.PropertyType == typeof(decimal));
    }

    [Fact]
    public void TEC_008_a_new_game_with_cards_has_a_pinned_canonical_text_and_hash()
    {
        // Pinned values computed independently in Python (scratchpad pin139.py), not from this code.
        var game = NewGame(7);
        Assert.Equal(PinnedText, game.CanonicalText());
        Assert.Equal(PinnedHash, game.ComputeHash());
    }

    // ---- Draw step (CD1-140)

    private static void End(GameState g) => Assert.True(g.ApplyCommand(new EndTurnCommand(g.ActiveFaction!)).Accepted);

    private static readonly string[] Pile = { "alpha", "bravo", "gamma", "gamma" };

    [Fact]
    public void TRN_005_the_phases_run_start_of_turn_income_draw_actions_with_cards()
    {
        var seen = new List<TurnPhase>();
        var game = new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), Params(), (p, f) => seen.Add(p), TestCards());
        Assert.Equal(new[] { TurnPhase.StartOfTurn, TurnPhase.Income, TurnPhase.Draw, TurnPhase.Actions }, seen);
        seen.Clear();
        End(game);
        Assert.Equal(new[] { TurnPhase.EndOfTurn, TurnPhase.StartOfTurn, TurnPhase.Income, TurnPhase.Draw, TurnPhase.Actions }, seen);
    }

    [Fact]
    public void CRD_005_P_CRD_02_a_faction_draws_one_card_from_the_top_of_its_deck_into_its_hand()
    {
        var game = NewGame(7);
        var next = Of(game, Second(game));
        var top = next.Deck[0];
        var deckBefore = next.Deck.Count;
        var handBefore = next.Hand.ToList();
        End(game);
        Assert.Equal(deckBefore - 1, next.Deck.Count);
        Assert.Equal(handBefore.Append(top), next.Hand);
    }

    [Fact]
    public void CRD_005_P_CRD_02_two_cards_are_drawn_when_the_data_says_2()
    {
        var game = NewGame(7, Params(draw: 2));
        Assert.Equal(5 + 2, Of(game, game.ActiveFaction!).Hand.Count); // turn 1 draws too
        var next = Of(game, Second(game));
        var top = next.Deck.Take(2).ToList();
        var handBefore = next.Hand.ToList();
        End(game);
        Assert.Equal(handBefore.Concat(top), next.Hand);
    }

    [Fact]
    public void CRD_005_the_first_player_draws_on_turn_1_under_each_seed()
    {
        foreach (var seed in Seeds)
        {
            var game = NewGame(seed);
            var first = Of(game, game.ActiveFaction!);
            Assert.Equal(1, game.TurnNumber);
            Assert.Equal(5 + 1, first.Hand.Count);
            Assert.Equal(9 - 5 - 1, first.Deck.Count);
        }
    }

    [Fact]
    public void TRN_005_only_the_active_faction_draws()
    {
        var game = NewGame(7);
        var waiting = Of(game, Second(game));
        var (deck, hand) = (waiting.Deck.ToList(), waiting.Hand.ToList());
        TurnPhases.Draw(game, game.ActiveFaction!);
        Assert.Equal(deck, waiting.Deck);
        Assert.Equal(hand, waiting.Hand);
    }

    [Fact]
    public void CRD_015_an_empty_deck_is_made_from_the_shuffled_discard_pile_and_one_card_is_drawn()
    {
        var game = NewGame(7);
        var east = Of(game, "east"); // east goes second under seed 7
        Assert.Equal("west", game.ActiveFaction);
        east._deck.Clear();
        east._discard.AddRange(Pile);
        var handBefore = east.Hand.Count;
        End(game);
        // The order and the random state are fixed values; pin140.py reproduces the state hash around them but does not compute the reshuffle itself.
        Assert.Equal(new[] { "alpha", "gamma", "gamma" }, east.Deck);
        Assert.Equal(handBefore + 1, east.Hand.Count);
        Assert.Equal("bravo", east.Hand[^1]);
        Assert.Empty(east.Discard);
        Assert.Contains("random=6653367501949350315\n", game.CanonicalText());
    }

    [Fact]
    public void CRD_015_the_discard_pile_is_left_alone_while_the_deck_has_cards()
    {
        var game = NewGame(7);
        var east = Of(game, "east");
        east._discard.Add("alpha");
        var random = game.CanonicalText().Split('\n').Single(l => l.StartsWith("random="));
        End(game);
        Assert.Equal(new[] { "alpha" }, east.Discard);
        Assert.Contains(random + "\n", game.CanonicalText());
    }

    private static string RunReshuffle(ulong seed)
    {
        var g = NewGame(seed);
        var next = Of(g, Second(g));
        next._deck.Clear();
        next._discard.AddRange(Pile);
        End(g);
        return g.ComputeHash() + string.Join(",", next.Deck) + next.Hand[^1];
    }

    [Fact]
    public void CRD_015_TEC_008_the_same_seed_and_commands_give_the_same_reshuffle_and_hash()
    {
        Assert.Equal(RunReshuffle(7), RunReshuffle(7));
        Assert.True(Seeds.Select(RunReshuffle).Distinct().Count() > 1, "different seeds should differ");
    }

    [Fact]
    public void CRD_014_a_hand_of_8_still_draws_and_holds_9()
    {
        var game = NewGame(7);
        var east = Of(game, "east");
        east._deck.AddRange(east._hand);
        east._hand.Clear();
        east._hand.AddRange(east._deck.GetRange(east._deck.Count - 8, 8));
        east._deck.RemoveRange(east._deck.Count - 8, 8);
        Assert.Equal(8, east.Hand.Count);
        End(game);
        Assert.Equal(9, east.Hand.Count);
    }

    [Fact]
    public void CRD_005_with_deck_and_discard_both_empty_nothing_is_drawn_and_the_turn_goes_on()
    {
        // Not specified by any rule; assumed here, Alex to confirm.
        var game = NewGame(7);
        var east = Of(game, "east");
        east._deck.Clear();
        var before = east.Hand.ToList();
        var random = game.CanonicalText().Split('\n').Single(l => l.StartsWith("random="));
        End(game);
        Assert.Equal(before, east.Hand);
        Assert.Equal("east", game.ActiveFaction);
        Assert.Equal(TurnPhase.Actions, game.Phase);
        Assert.Contains(random + "\n", game.CanonicalText());
    }

    [Fact]
    public void DLV_013_a_game_replayed_from_its_seed_and_log_has_the_same_draws_and_hash()
    {
        var game = NewGame(7);
        for (var i = 0; i < 5; i++) End(game);
        var replayed = GameState.Replay(7, game.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), Params(), TestCards());
        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(game.Factions.Select(f => string.Join(",", f.Hand)), replayed.Factions.Select(f => string.Join(",", f.Hand)));
    }

    private const string PinnedText =
        "weirdwar.state.v1\n" +
        "seed=7\n" +
        "random=9344711191398858092\n" +
        "tally=0\n" +
        "map=9e42203dc85df636af50dbd2072184e378bafb5e4b3c8c8c1f089dcb0262202b\n" +
        "parameters=511cae24c375517810b1653316d4ea2579f0f42d6bb8d41dc5bbb3846fe2a6e1\n" +
        "sectiontypes=d62a3d929058987d911b05c0502852239ca13f645d632e940f460f219b77ad63\n" +
        "faction=east reserve=5 cp=0\n" +
        "faction=west reserve=10 cp=2\n" +
        "cards=f124f0993e5806815a9423432390938e4fe4e65d7b7880c4bcefe64c1b6e7327\n" +
        "cardstate=east deck=bravo,alpha,gamma hand=gamma,alpha,gamma,gamma,alpha,bravo discard=\n" +
        "cardstate=west deck=alpha,bravo,gamma hand=gamma,gamma,gamma,alpha,bravo,alpha discard=\n" +
        "location=hq-east owner=east\n" +
        "location=hq-west owner=west\n" +
        "nextbrigade=3\n" +
        "brigade=1 faction=east sp=90 sections=infantry:10,infantry:10,infantry:10 at=5,1\n" +
        "brigade=2 faction=west sp=90 sections=infantry:10,infantry:10,infantry:10 at=0,2\n" +
        "turn=1 active=west phase=Actions\n" +
        "log=0\n" +
        "digest=0000000000000000000000000000000000000000000000000000000000000000\n";
    private const string PinnedHash = "5bd60b1e2f8a486a3704169f9194e6e40286b353d3c825b2cd2c3e92097cdee7";
}
