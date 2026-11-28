using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

/// <summary>FW1-136: the whole-Feature guarantees for fog of war. Each earlier Story tests its own part.</summary>
public class FogOfWarClosingTests
{
    private static string Types(int sight = 1) =>
        "{\"version\":1,\"sectionTypes\":{\"a\":{\"class\":\"Infantry\",\"spCost\":10,\"healthPerUnit\":1,\"maxUnits\":10,\"health\":10,\"sight\":" + sight + ",\"capacityContribution\":35,\"consumptionPerHex\":1}}}";

    // West HQ (5,4), east HQ (11,7), a Supply ST at (3,1) far from both HQs, and a neutral major at (8,1).
    private const string Map =
        "{\"version\":1,\"width\":12,\"height\":8,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[11,7],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[5,4],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[3,1]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[8,1]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}},\"west\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}}}}";

    private const string Big =
        "{\"version\":1,\"width\":30,\"height\":20,\"terrain\":{},\"factions\":[\"east\",\"west\"],\"locations\":[" +
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[29,19],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[15,10],\"faction\":\"west\"}," +
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[5,5]}]," +
        "\"setup\":{\"east\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}},\"west\":{\"brigade\":{\"sections\":[\"a\",\"a\",\"a\"]}}}}";

    private static string Params(string sight = "hq=3 fob=2 major=2 command=1 supply=1") =>
        "P-BRG-01 = 5\nP-BRG-05 = 3\nP-BRG-09 = 90\nP-RES-05 = 50\nP-RES-06 = 20\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 3\nP-FOW-03 = " + sight + "\n" +
        "P-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-08 = 1\nP-SUP-10 = 2\nP-TRN-02 = 1\nP-MAP-03 = sp=5 cp=1 cpStep=1\nP-MAP-14 = 1";

    private static GameState NewGame(int sight = 1, string sightParam = "hq=3 fob=2 major=2 command=1 supply=1", string map = Map)
        => new GameState(7, MapImporter.Parse(map), SectionTypeTable.Parse(Types(sight)), ParameterTable.Parse(Params(sightParam)));
    private static HexCoord Hex(int c, int r) => HexCoord.FromOffset(c, r);
    private static Brigade Of(GameState g, string f) => g.Brigades.First(b => b.Faction == f);
    private static LocationState Loc(GameState g, string id) => g.Locations.Single(l => l.Id == id);
    private static FactionView View(GameState g, string f) => g.FogOfWar!.ViewFor(g, f);
    private static void Do(GameState g, ICommand c) { var r = g.ApplyCommand(c); Assert.True(r.Accepted, r.Reason); }
    private static void EndTurn(GameState g) => Do(g, new EndTurnCommand(g.ActiveFaction!));

    private static Brigade AddScout(GameState g, string faction, HexCoord at)
    {
        var b = new Brigade(g.TakeBrigadeId(), faction, 30, at, new List<Section> { new Section("a", 10), new Section("a", 10), new Section("a", 10) });
        g.AddBrigade(b);
        return b;
    }

    private static void MoveTo(GameState g, Brigade b, HexCoord target)
    {
        var path = new List<HexCoord>();
        var at = b.Position;
        while (at != target)
        {
            at = at.Neighbours().OrderBy(n => n.DistanceTo(target)).First();
            path.Add(at);
        }
        Do(g, new MoveBrigadeCommand(b.Faction, b.Id, path));
    }

    /// <summary>Every public value in a view, one line per leaf, walking the whole object (lists in order).</summary>
    private static List<string> Walk(object? o, string path = "view")
    {
        var lines = new List<string>();
        if (o == null) { lines.Add(path + "=null"); return lines; }
        var t = o.GetType();
        if (o is string || t.IsPrimitive || t.IsEnum || o is HexCoord) { lines.Add(path + "=" + Convert.ToString(o, CultureInfo.InvariantCulture)); return lines; }
        if (o is System.Collections.IEnumerable list)
        {
            var i = 0;
            foreach (var item in list) lines.AddRange(Walk(item, path + "[" + i++ + "]"));
            if (i == 0) lines.Add(path + "=empty");
            return lines;
        }
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name, StringComparer.Ordinal))
            lines.AddRange(Walk(p.GetValue(o), path + "." + p.Name));
        return lines;
    }

    private static string Dump(FactionView v) => string.Join("\n", Walk(v));
    private static List<string> Known(GameState g, string faction = "") =>
        g.CanonicalText().Split('\n').Where(l => l.StartsWith("known=" + faction)).ToList();

    // Seed 7 gives west the first turn. West owns supply-1 and upgrades it while east, who saw it earlier, has no sight of it.
    private static GameState UpgradedOutOfSight(out Brigade eastScout)
    {
        var g = NewGame();
        Loc(g, "supply-1").Owner = "west";
        eastScout = AddScout(g, "east", Hex(4, 1));
        EndTurn(g);                                  // west ends; east's scout next to supply-1 records it
        MoveTo(g, eastScout, Hex(7, 1));             // east's scout leaves
        EndTurn(g);
        Do(g, new UpgradeFobCommand("west", "supply-1"));
        return g;
    }

    [Fact]
    public void FOW_010_FOW_009_an_upgrade_made_out_of_sight_still_shows_the_remembered_type()
    {
        var g = UpgradedOutOfSight(out var scout);
        var remembered = View(g, "east").Locations.Single(l => l.Id == "supply-1");
        Assert.Equal(LocationType.Fob, Loc(g, "supply-1").Type);                 // the truth
        Assert.False(remembered.InSightNow);
        Assert.Equal(LocationType.Supply, remembered.Type);                      // not the FOB
        Assert.Equal("west", remembered.Owner);
        Assert.Equal(LocationType.Fob, View(g, "west").Locations.Single(l => l.Id == "supply-1").Type);

        // Once east sees it again it learns the FOB.
        EndTurn(g);
        MoveTo(g, scout, Hex(4, 1));
        var seen = View(g, "east").Locations.Single(l => l.Id == "supply-1");
        Assert.True(seen.InSightNow);
        Assert.Equal(LocationType.Fob, seen.Type);
    }

    [Fact]
    public void FOW_010_an_enemy_fob_seen_earlier_and_now_out_of_sight_shows_its_last_known_owner_and_type()
    {
        var g = UpgradedOutOfSight(out var scout);
        EndTurn(g);
        MoveTo(g, scout, Hex(4, 1));                 // east sees the enemy FOB
        Assert.Equal(LocationType.Fob, View(g, "east").Locations.Single(l => l.Id == "supply-1").Type);
        MoveTo(g, scout, Hex(7, 1));                 // and walks off again
        Loc(g, "supply-1").Owner = null;             // the truth changes where east cannot see
        var v = View(g, "east").Locations.Single(l => l.Id == "supply-1");
        Assert.False(v.InSightNow);
        Assert.Equal("west", v.Owner);
        Assert.Equal(LocationType.Fob, v.Type);
        Assert.Equal(Hex(3, 1), v.Position);
    }

    [Fact]
    public void FOW_010_an_enemy_hq_seen_earlier_and_now_out_of_sight_shows_its_last_known_owner()
    {
        var g = NewGame();
        var scout = AddScout(g, "west", Hex(11, 7).Neighbours().OrderBy(n => n.DistanceTo(Hex(7, 6))).First());
        EndTurn(g);                                  // the accepted command records what west's scout sees
        EndTurn(g);
        Assert.Equal("east", View(g, "west").Locations.Single(l => l.Id == "hq-east").Owner);
        MoveTo(g, scout, Hex(7, 6));
        Loc(g, "hq-east").Owner = null;              // the truth flips out of sight
        var v = View(g, "west").Locations.Single(l => l.Id == "hq-east");
        Assert.False(v.InSightNow);
        Assert.Equal("east", v.Owner);
        Assert.Equal(LocationType.Hq, v.Type);
    }

    private static GameState Scripted(out string savedLog)
    {
        var g = UpgradedOutOfSight(out var scout);
        EndTurn(g);
        MoveTo(g, scout, Hex(4, 1));
        EndTurn(g);
        MoveTo(g, Of(g, "west"), Hex(7, 1));
        savedLog = CommandRegistry.CreateDefault().WriteLog(7, g.Log);
        return g;
    }

    private static void AssertSame(GameState a, GameState b)
    {
        Assert.Equal(a.ComputeHash(), b.ComputeHash());
        Assert.Equal(a.CanonicalText(), b.CanonicalText());
        Assert.Equal(Known(a), Known(b));
        foreach (var f in new[] { "east", "west" }) Assert.Equal(Dump(View(a, f)), Dump(View(b, f)));
    }

    [Fact]
    public void TEC_008_the_same_start_and_commands_run_twice_and_from_a_saved_log_give_the_same_record_views_and_hash()
    {
        var a = Scripted(out var logA);
        var b = Scripted(out var logB);
        Assert.Equal(logA, logB);
        Assert.NotEmpty(Known(a, "east"));
        Assert.NotEmpty(Known(a, "west"));
        AssertSame(a, b);

        // The scout is part of the start, so place it before the first logged command.
        var fromLog = NewGame();
        Loc(fromLog, "supply-1").Owner = "west";
        AddScout(fromLog, "east", Hex(4, 1));
        foreach (var e in CommandRegistry.CreateDefault().ReadLog(logA).Log.Entries) Do(fromLog, e.Command);
        AssertSame(a, fromLog);
    }

    [Fact]
    public void TEC_008_a_game_with_no_hand_placed_pieces_replays_from_its_saved_log_to_the_same_record_views_and_hash()
    {
        var g = NewGame();
        MoveTo(g, Of(g, "west"), Hex(7, 1));
        EndTurn(g);
        MoveTo(g, Of(g, "east"), Hex(8, 3));
        EndTurn(g);
        var run = CommandRegistry.CreateDefault().ReadLog(CommandRegistry.CreateDefault().WriteLog(7, g.Log));
        var replayed = GameState.Replay(run.Seed, run.Log, MapImporter.Parse(Map), SectionTypeTable.Parse(Types()), ParameterTable.Parse(Params()));
        Assert.Contains(Known(g, "west"), l => l.StartsWith("known=west major-1"));
        AssertSame(g, replayed);
    }

    private static GameState WithEastAt(HexCoord eastAt)
    {
        var g = NewGame();
        EndTurn(g);                                  // west ends
        MoveTo(g, Of(g, "east"), eastAt);
        return g;
    }

    [Fact]
    public void TEC_008_MUL_014_a_hidden_enemy_brigade_moved_elsewhere_leaves_the_view_equal_but_changes_the_hash()
    {
        var a = WithEastAt(Hex(10, 6));
        var b = WithEastAt(Hex(9, 6));
        Assert.DoesNotContain(Hex(10, 6), a.FogOfWar!.VisibleHexes(a, "west"));
        Assert.DoesNotContain(Hex(9, 6), b.FogOfWar!.VisibleHexes(b, "west"));

        Assert.Equal(Dump(View(a, "west")), Dump(View(b, "west")));
        Assert.Equal(Known(a, "west"), Known(b, "west"));
        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());                       // the state still holds the truth
    }

    [Fact]
    public void MUL_014_the_view_holds_no_id_hex_owner_or_count_the_faction_cannot_see()
    {
        var g = NewGame();
        g.Factions.Single(f => f.Id == "east").Reserve = 4242;
        g.Factions.Single(f => f.Id == "east").Cp = 3131;
        var hidden = Of(g, "east");
        hidden.SP = 4;
        var west = Of(g, "west");
        var self = g.Factions.Single(f => f.Id == "west");

        var lines = Walk(View(g, "west"));

        var expected = new List<string>
        {
            "view.Cp=" + self.Cp,
            "view.EnemyBrigades=empty",
            "view.Faction=west",
            "view.Locations[0].Id=hq-west", "view.Locations[0].InSightNow=True", "view.Locations[0].Owner=west",
            "view.Locations[0].Position=" + Hex(5, 4), "view.Locations[0].Type=Hq",
            "view.OwnBrigades[0].Id=" + west.Id, "view.OwnBrigades[0].Position=" + west.Position, "view.OwnBrigades[0].SP=" + west.SP,
            "view.Reserve=" + self.Reserve,
        };
        // What is left is terrain (one entry per hex) and the three own Sections, and nothing else.
        var rest = lines.Where(l => !expected.Contains(l) && !l.StartsWith("view.Terrain[") && !l.StartsWith("view.OwnBrigades[0].Sections[")).ToList();
        Assert.Empty(rest);
        foreach (var e in expected) Assert.Contains(e, lines);
        Assert.Equal(12 * 8, View(g, "west").Terrain.Count);
        Assert.Equal(3, View(g, "west").OwnBrigades.Single().Sections.Count);
        Assert.DoesNotContain(lines, l => l.EndsWith("=4242") || l.EndsWith("=3131") || l.EndsWith("=4") || l.Contains("hq-east") || l.Contains("east"));
        Assert.NotEqual(hidden.Position, west.Position);
    }

    [Fact]
    public void MUL_014_the_view_types_expose_no_property_beyond_the_ones_the_rules_allow()
    {
        var allowed = new Dictionary<Type, string[]>
        {
            [typeof(FactionView)] = new[] { "Cp", "EnemyBrigades", "Faction", "Locations", "OwnBrigades", "Reserve", "Terrain" },
            [typeof(ViewHex)] = new[] { "Position", "Terrain" },
            [typeof(ViewLocation)] = new[] { "Id", "InSightNow", "Owner", "Position", "Type" },
            [typeof(ViewOwnBrigade)] = new[] { "Id", "Position", "SP", "Sections" },
            [typeof(ViewSection)] = new[] { "Health", "TypeId" },
            [typeof(ViewEnemyBrigade)] = new[] { "Faction", "Position" },
        };
        foreach (var kv in allowed)
            Assert.Equal(kv.Value.OrderBy(n => n, StringComparer.Ordinal), kv.Key.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void TEC_019_the_canonical_state_text_holds_no_floating_point_number_including_the_sight_record()
    {
        var g = UpgradedOutOfSight(out _);
        var text = g.CanonicalText();
        Assert.Contains("known=east supply-1", text);
        Assert.Contains("known=west hq-west", text);
        // Digest lines are hex; every other line must hold no decimal point, exponent, NaN or Infinity.
        var plain = text.Split('\n').Where(l => !l.StartsWith("digest=") && !l.StartsWith("map=") && !l.StartsWith("parameters=") && !l.StartsWith("sectiontypes="));
        Assert.DoesNotMatch(new Regex(@"\d\.\d|\d[eE][+-]?\d|NaN|Infinity"), string.Join("\n", plain));
        Assert.DoesNotContain(".", string.Join("\n", Known(g)));
    }

    [Fact]
    public void DLV_013_making_any_factions_view_creates_no_command_and_leaves_the_log_and_state_alone()
    {
        var g = UpgradedOutOfSight(out _);
        var entries = g.Log.Entries.ToList();
        var text = g.CanonicalText();
        var hash = g.ComputeHash();
        foreach (var f in new[] { "east", "west" })
        {
            View(g, f);
            g.FogOfWar!.VisibleHexes(g, f);
        }
        Assert.True(entries.SequenceEqual(g.Log.Entries));
        Assert.Equal(entries.Count, g.Log.Count);
        Assert.Equal(text, g.CanonicalText());
        Assert.Equal(hash, g.ComputeHash());
    }

    [Fact]
    public void DLV_014_FOW_006_a_changed_section_sight_and_hq_radius_change_the_visible_hexes()
    {
        // The Brigade stands on the HQ (15,10), far from every edge, so the larger of the two radii decides.
        foreach (var (sight, hq) in new[] { (1, 1), (2, 1), (3, 1), (1, 3), (1, 5), (2, 4) })
        {
            var g = NewGame(sight, "hq=" + hq + " fob=2 major=2 command=1 supply=1", Big);
            var seen = g.FogOfWar!.VisibleHexes(g, "west");
            var radius = Math.Max(sight, hq);
            Assert.Equal(3 * radius * (radius + 1) + 1, seen.Count);
            Assert.All(seen, h => Assert.True(h.DistanceTo(Of(g, "west").Position) <= radius));
        }
    }

    [Fact]
    public void DLV_014_FOW_007_a_changed_supply_radius_changes_the_visible_hexes_of_an_owned_supply_st()
    {
        foreach (var supply in new[] { 1, 2, 3 })
        {
            var g = NewGame(1, "hq=1 fob=2 major=2 command=1 supply=" + supply, Big);
            var st = Hex(5, 5);
            var near = g.FogOfWar!.VisibleHexes(g, "west").Count(h => h.DistanceTo(st) <= 4);
            Assert.Equal(0, near);                                        // neutral: no sight
            Loc(g, "supply-1").Owner = "west";
            var around = g.FogOfWar!.VisibleHexes(g, "west").Where(h => h.DistanceTo(st) <= 4).ToList();
            Assert.Equal(3 * supply * (supply + 1) + 1, around.Count);
            Assert.Equal(supply, around.Max(h => h.DistanceTo(st)));
        }
    }
}
