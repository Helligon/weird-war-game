using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class ExtraSectionTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    private const string MapJson =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,0],\"faction\":\"west\"}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}," +
        "\"west\":{\"brigade\":{\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}}";

    private const string CardsJson =
        "{\"version\":1,\"cards\":{" +
        "\"extraSection\":{\"name\":\"Extra Section\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":1}," +
        "\"alpha\":{\"name\":\"Alpha\",\"pool\":\"Core\",\"cpCost\":1,\"copies\":9}}}";

    private static string Params(int bonus = 1)
        => "P-BRG-01 = 5\nP-BRG-06 = " + bonus + "\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 0\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\n" +
           "P-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1\n" +
           "P-CRD-01 = 5\nP-TRN-01 = 1\nP-CRD-02 = 1";

    private static GameState NewGame(ulong seed = 7, int bonus = 1)
        => new GameState(seed, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params(bonus)), CardTable.Parse(CardsJson));

    private static FactionState Active(GameState g) => g.Factions.Single(f => f.Id == g.ActiveFaction);

    private static HexCoord At(int c, int r) => HexCoord.FromOffset(c, r);

    private static Brigade Add(GameState g, string faction, HexCoord at, int sections)
    {
        var list = Enumerable.Range(0, sections).Select(_ => new Section("infantry", 10)).ToList();
        var b = new Brigade(g.TakeBrigadeId(), faction, 10, at, list);
        g.AddBrigade(b);
        return b;
    }

    /// <summary>The active Faction has the Card in hand and CP, a target Brigade of 5 Sections and an adjacent donor Brigade of 4.</summary>
    private static (GameState g, Brigade target, Brigade donor) Setup(int bonus = 1, int cp = 5)
    {
        var g = NewGame(7, bonus);
        var f = Active(g);
        f._hand.Clear();
        f._hand.AddRange(new[] { "extraSection", "extraSection", "extraSection" });
        f.Cp = cp;
        var target = Add(g, f.Id, At(5, 3), 5);
        var donor = Add(g, f.Id, At(6, 3), 4);
        return (g, target, donor);
    }

    private static CommandResult Play(GameState g, int brigadeId) => g.ApplyCommand(new PlayCardCommand(g.ActiveFaction, "extraSection", brigadeId.ToString()));

    private static CommandResult Transfer(GameState g, Brigade from, Brigade to) => g.ApplyCommand(new TransferSectionCommand(g.ActiveFaction, from.Id, 0, to.Id));

    private static void AssertRejectedUnchanged(GameState g, ICommand c)
    {
        var (hash, logged) = (g.ComputeHash(), g.Log.Count);
        var r = g.ApplyCommand(c);
        Assert.False(r.Accepted);
        Assert.False(string.IsNullOrEmpty(r.Reason));
        Assert.Equal(hash, g.ComputeHash());
        Assert.Equal(logged, g.Log.Count);
    }

    [Fact]
    public void BRG_021_a_brigade_of_5_sections_rejects_a_transfer_into_it()
    {
        var (g, target, donor) = Setup();
        AssertRejectedUnchanged(g, new TransferSectionCommand(g.ActiveFaction, donor.Id, 0, target.Id));
        Assert.Equal(5, target.Sections.Count);
    }

    [Fact]
    public void BRG_022_P_BRG_06_after_Extra_Section_a_transfer_is_accepted_and_the_brigade_has_6_Sections()
    {
        var (g, target, donor) = Setup();
        Assert.True(Play(g, target.Id).Accepted);
        Assert.Equal(1, target.SectionBonus);
        Assert.True(Transfer(g, donor, target).Accepted);
        Assert.Equal(6, target.Sections.Count);
    }

    [Fact]
    public void BRG_021_BRG_022_a_brigade_at_6_Sections_rejects_another_transfer()
    {
        var (g, target, donor) = Setup();
        Assert.True(Play(g, target.Id).Accepted);
        Assert.True(Transfer(g, donor, target).Accepted);
        AssertRejectedUnchanged(g, new TransferSectionCommand(g.ActiveFaction, donor.Id, 0, target.Id));
        Assert.Equal(6, target.Sections.Count);
    }

    [Fact]
    public void P_BRG_06_with_the_parameter_at_2_Extra_Section_makes_the_cap_7()
    {
        var (g, target, donor) = Setup(bonus: 2);
        var third = Add(g, g.ActiveFaction!, At(4, 3), 3);
        Assert.True(Play(g, target.Id).Accepted);
        Assert.Equal(2, target.SectionBonus);
        Assert.True(Transfer(g, donor, target).Accepted);
        Assert.True(Transfer(g, donor, target).Accepted);
        Assert.Equal(7, target.Sections.Count);
        AssertRejectedUnchanged(g, new TransferSectionCommand(g.ActiveFaction, third.Id, 0, target.Id));
    }

    [Fact]
    public void P_BRG_06_a_missing_or_zero_parameter_fails_at_game_start()
    {
        var without = Params().Replace("P-BRG-06 = 1\n", "");
        Assert.ThrowsAny<Exception>(() => new GameState(7, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(without), CardTable.Parse(CardsJson)));
        Assert.ThrowsAny<Exception>(() => NewGame(7, 0));
    }

    [Fact]
    public void BRG_022_Extra_Section_on_Brigade_1_leaves_another_Brigades_cap_unchanged()
    {
        var (g, target, donor) = Setup();
        var other = Add(g, g.ActiveFaction!, At(5, 4), 5);
        Assert.True(Play(g, target.Id).Accepted);
        Assert.Equal(0, other.SectionBonus);
        AssertRejectedUnchanged(g, new TransferSectionCommand(g.ActiveFaction, donor.Id, 0, other.Id));
    }

    [Fact]
    public void BRG_022_playing_it_twice_on_one_Brigade_adds_the_bonus_twice()
    {
        var (g, target, _) = Setup();
        Assert.True(Play(g, target.Id).Accepted);
        Assert.True(Play(g, target.Id).Accepted);
        Assert.Equal(2, target.SectionBonus);
    }

    [Fact]
    public void BRG_022_CRD_001_without_enough_CP_it_is_rejected_and_the_cap_is_unchanged()
    {
        var (g, target, donor) = Setup(cp: 0);
        AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "extraSection", target.Id.ToString()));
        Assert.Equal(0, target.SectionBonus);
        AssertRejectedUnchanged(g, new TransferSectionCommand(g.ActiveFaction, donor.Id, 0, target.Id));
    }

    [Fact]
    public void BRG_022_Extra_Section_aimed_at_an_enemy_Brigade_or_a_bad_target_is_rejected()
    {
        var (g, target, _) = Setup();
        var enemy = g.Brigades.First(b => b.Faction != g.ActiveFaction);
        foreach (var t in new[] { enemy.Id.ToString(), "", "99", "x", "-1", "1.0", " 1" })
            AssertRejectedUnchanged(g, new PlayCardCommand(g.ActiveFaction, "extraSection", t));
        Assert.Equal(0, enemy.SectionBonus);
        Assert.Equal(0, target.SectionBonus);
    }

    [Fact]
    public void BRG_022_TEC_008_the_bonus_is_in_the_state_text_and_hash()
    {
        var (g, target, _) = Setup();
        var before = g.ComputeHash();
        Assert.DoesNotContain("bonus=", g.CanonicalText());
        Assert.True(Play(g, target.Id).Accepted);
        Assert.Contains("brigade=" + target.Id + " faction=" + g.ActiveFaction + " sp=10 sections=infantry:10,infantry:10,infantry:10,infantry:10,infantry:10 at=5,3 bonus=1\n", g.CanonicalText());
        Assert.NotEqual(before, g.ComputeHash());
    }

    [Fact]
    public void BRG_022_TEC_008_replay_from_the_command_log_gives_the_same_hash()
    {
        var registry = CommandRegistry.CreateDefault();
        GameState g = null!;
        ulong seed;
        for (seed = 1; seed < 200; seed++)
        {
            g = NewGame(seed);
            if (Active(g).Hand.Contains("extraSection") && Active(g).Cp >= 1) break;
        }
        var f = Active(g);
        Assert.Contains("extraSection", f.Hand);
        var mine = g.Brigades.Single(b => b.Faction == f.Id);
        Assert.True(Play(g, mine.Id).Accepted);
        var run = registry.ReadLog(registry.WriteLog(seed, g.Log));
        Assert.Contains(run.Log.Entries, e => e.Command is PlayCardCommand p && p.Target == mine.Id.ToString());
        var replayed = GameState.Replay(seed, run.Log, MapImporter.Parse(MapJson), SectionTypeTable.Parse(TypesJson), ParameterTable.Parse(Params()), CardTable.Parse(CardsJson));
        Assert.Equal(g.ComputeHash(), replayed.ComputeHash());
        Assert.Equal(1, replayed.Brigades.Single(b => b.Id == mine.Id).SectionBonus);
    }

    [Fact]
    public void BRG_022_the_shipped_card_data_has_Extra_Section_with_an_effect()
    {
        Assert.True(CardTable.LoadShipped().TryGet("extraSection", out var card));
        Assert.Equal("Extra Section", card.Name);
        Assert.NotNull(card.Effect);
    }
}
