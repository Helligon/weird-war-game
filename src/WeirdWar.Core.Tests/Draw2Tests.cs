using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class Draw2Tests
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

    private const string ParamText = "P-BRG-01 = 5\nP-BRG-06 = 1\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 5\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1\nP-CRD-01 = 5\nP-TRN-01 = 1\nP-CRD-02 = 1";

    private const string CardsJson =
        "{\"version\":1,\"cards\":{" +
        "\"draw2\":{\"name\":\"Draw 2\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":2}," +
        "\"alpha\":{\"name\":\"Alpha\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":8}}}";

    private static GameState NewGame() => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(ParamText), CardTable.Parse(CardsJson));

    private static FactionState Active(GameState g) => g.Factions.Single(f => f.Id == g.ActiveFaction);

    private static GameState Setup(string[] deck, string[] discard, int cp = 3, string[]? hand = null)
    {
        var g = NewGame();
        var f = Active(g);
        f._hand.Clear();
        f._hand.AddRange(hand ?? new[] { "draw2" });
        f._deck.Clear();
        f._deck.AddRange(deck);
        f._discard.Clear();
        f._discard.AddRange(discard);
        f.Cp = cp;
        return g;
    }

    private static CommandResult Play(GameState g) => g.ApplyCommand(new PlayCardCommand(g.ActiveFaction, "draw2"));

    [Fact]
    public void CRD_007_the_shipped_card_data_has_a_draw_2_card_with_open_placeholder_cost_and_copies()
    {
        var cards = CardTable.LoadShipped();
        Assert.True(cards.TryGet("draw2", out var draw2));
        Assert.Equal("Draw 2", draw2.Name);
        Assert.NotNull(draw2.Effect);
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "cards.json"));
        Assert.Contains("\"cpCost\": \"placeholder (open): P-CRD-08 CP cost per Card\"", text);
        Assert.Contains("\"copies\": \"placeholder (open): P-CRD-08 copies per Card in the deck\"", text);
    }

    [Fact]
    public void CRD_007_draw_2_pays_its_CP_and_the_hand_gains_the_top_2_cards_of_a_deck_of_5_in_order()
    {
        var g = Setup(new[] { "a1", "a2", "a3", "a4", "a5" }, new string[0], cp: 3, hand: new[] { "draw2", "alpha" });
        var f = Active(g);
        Assert.True(Play(g).Accepted);
        Assert.Equal(2, f.Cp);
        Assert.Equal(new[] { "alpha", "a1", "a2" }, f.Hand);
        Assert.Equal(new[] { "a3", "a4", "a5" }, f.Deck);
        Assert.Equal(new[] { "draw2" }, f.Discard);
    }

    [Fact]
    public void CRD_007_a_hand_of_1_Draw_2_gains_one_more_than_before_the_play()
    {
        var g = Setup(new[] { "a1", "a2", "a3", "a4", "a5" }, new string[0]);
        Assert.True(Play(g).Accepted);
        Assert.Equal(2, Active(g).Hand.Count);
    }

    [Fact]
    public void CRD_007_CRD_015_with_an_empty_deck_and_a_discard_pile_of_4_the_pile_is_reshuffled_and_2_cards_are_drawn()
    {
        var g = Setup(new string[0], new[] { "d1", "d2", "d3", "d4" });
        var f = Active(g);
        Assert.True(Play(g).Accepted);
        Assert.Equal(2, f.Hand.Count);
        // The played Card is already in the discard pile, so it is shuffled in with the 4 and the 5 Cards make the new deck.
        Assert.Equal(3, f.Deck.Count);
        Assert.Empty(f.Discard);
        Assert.Equal(new[] { "d1", "d2", "d3", "d4", "draw2" }, f.Hand.Concat(f.Deck).OrderBy(c => c, StringComparer.Ordinal));
        // Fixed values from an independent Python model of the seeded source and the shuffle (not from the Draw step's code).
        Assert.Equal(new[] { "draw2", "d2" }, f.Hand);
        Assert.Equal(new[] { "d3", "d4", "d1" }, f.Deck);
        Assert.Contains("random=3962023812499842538\n", g.CanonicalText());
    }

    [Fact]
    public void CRD_007_CRD_015_with_a_deck_of_1_and_a_discard_pile_of_3_the_deck_card_comes_first_then_the_reshuffle()
    {
        var g = Setup(new[] { "top" }, new[] { "d1", "d2", "d3" });
        var f = Active(g);
        Assert.True(Play(g).Accepted);
        Assert.Equal(2, f.Hand.Count);
        Assert.Equal("top", f.Hand[0]);
        Assert.Contains(f.Hand[1], new[] { "d1", "d2", "d3", "draw2" });
        Assert.Equal(3, f.Deck.Count);
        Assert.Empty(f.Discard);
        Assert.Equal(new[] { "d1", "d2", "d3", "draw2" }, f.Hand.Skip(1).Concat(f.Deck).OrderBy(c => c, StringComparer.Ordinal));
    }

    [Fact]
    public void CRD_007_with_fewer_than_2_cards_in_deck_and_discard_together_draw_2_draws_what_there_is()
    {
        // Not specified by any rule; assumed here (same as the Draw step), Alex to confirm. The played Card itself joins the discard pile.
        var g = Setup(new string[0], new string[0]);
        var f = Active(g);
        Assert.True(Play(g).Accepted);
        Assert.Equal(new[] { "draw2" }, f.Hand);
        Assert.Empty(f.Deck);
        Assert.Empty(f.Discard);
    }

    [Fact]
    public void CRD_007_draw_2_can_take_the_hand_above_8()
    {
        var hand = new[] { "draw2", "alpha", "alpha", "alpha", "alpha", "alpha", "alpha", "alpha" };
        var g = Setup(new[] { "a1", "a2", "a3" }, new string[0], hand: hand);
        Assert.True(Play(g).Accepted);
        Assert.Equal(9, Active(g).Hand.Count);
    }

    [Fact]
    public void CRD_007_a_faction_without_enough_CP_is_rejected_and_nothing_is_drawn()
    {
        var g = Setup(new[] { "a1", "a2", "a3" }, new string[0], cp: 0);
        var (hash, logged) = (g.ComputeHash(), g.Log.Count);
        var r = Play(g);
        Assert.False(r.Accepted);
        Assert.Equal(hash, g.ComputeHash());
        Assert.Equal(logged, g.Log.Count);
        Assert.Equal(new[] { "draw2" }, Active(g).Hand);
        Assert.Equal(new[] { "a1", "a2", "a3" }, Active(g).Deck);
    }

    private static string RunDraw2(ulong seed)
    {
        var g = new GameState(seed, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(ParamText), CardTable.Parse(CardsJson));
        g.ApplyCommand(new EndTurnCommand(g.ActiveFaction));
        g.ApplyCommand(new EndTurnCommand(g.ActiveFaction));
        var f = Active(g);
        f._deck.Clear();
        f._discard.AddRange(new[] { "d1", "d2", "d3" });
        f._hand.Add("draw2");
        f.Cp = 3;
        Assert.True(g.ApplyCommand(new PlayCardCommand(f.Id, "draw2")).Accepted);
        return g.ComputeHash() + string.Join(",", f.Hand) + "|" + string.Join(",", f.Deck);
    }

    [Fact]
    public void CRD_007_TEC_008_the_same_seed_gives_the_same_draw_2_results_and_hash()
    {
        Assert.Equal(RunDraw2(7), RunDraw2(7));
    }

    [Fact]
    public void CRD_007_TEC_008_replay_from_the_command_log_of_a_game_with_a_draw_2_play_gives_the_same_hash()
    {
        var registry = CommandRegistry.CreateDefault();
        // Find a seed where the deal and draws put Draw 2 in the first Faction's hand by its second turn, so the play needs no state changes outside commands.
        GameState g = null!;
        ulong seed;
        for (seed = 1; seed < 200; seed++)
        {
            g = new GameState(seed, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(ParamText), CardTable.Parse(CardsJson));
            g.ApplyCommand(new EndTurnCommand(g.ActiveFaction));
            g.ApplyCommand(new EndTurnCommand(g.ActiveFaction));
            if (Active(g).Hand.Contains("draw2") && Active(g).Cp >= 1) break;
        }
        var f = Active(g);
        Assert.Contains("draw2", f.Hand);
        Assert.True(g.ApplyCommand(new PlayCardCommand(f.Id, "draw2")).Accepted);
        Assert.Contains(g.Log.Entries, e => e.Command is PlayCardCommand);
        var run = registry.ReadLog(registry.WriteLog(seed, g.Log));
        var replayed = GameState.Replay(seed, run.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(ParamText), CardTable.Parse(CardsJson));
        Assert.Equal(g.ComputeHash(), replayed.ComputeHash());
    }
}
