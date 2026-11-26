using System.Reflection;
using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class SetupTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":1}," +
        "\"artillery\":{\"health\":6,\"sight\":1,\"capacityContribution\":35,\"consumptionPerHex\":2}," +
        "\"recon\":{\"health\":4,\"sight\":3,\"capacityContribution\":25,\"consumptionPerHex\":1}}}";

    private const string Hqs =
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[5,1],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}";

    private const string StsBlock =
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[2,0]}," +
        "{\"id\":\"command-1\",\"type\":\"command\",\"at\":[3,3]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[3,1]}";

    private const string SetupBlock =
        "{\"east\":{\"reserve\":5,\"brigade\":{\"sp\":90,\"sections\":[\"infantry\",\"infantry\",\"artillery\"]}}," +
        "\"west\":{\"reserve\":6,\"brigade\":{\"sp\":85,\"sections\":[\"infantry\",\"infantry\",\"recon\"]}}}";

    private static string MapJson(string locations = Hqs + "," + StsBlock, string factions = "[\"east\",\"west\"]", string setup = SetupBlock)
        => "{\"version\":1,\"width\":6,\"height\":4,\"terrain\":{\"mountain\":[[1,1]]},\"factions\":" + factions +
           ",\"locations\":[" + locations + "],\"setup\":" + setup + "}";

    private static ParameterTable Params() => ParameterTable.Parse("P-BRG-05 = 3\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-10 = 2");

    private static SectionTypeTable Types() => SectionTypeTable.Parse(TypesJson);

    private static GameState NewGame(ulong seed = 7, string? json = null)
        => new GameState(seed, MapImporter.Parse(json ?? MapJson()), Types(), Params());

    private static string ImportError(string json) => Assert.Throws<MapFormatException>(() => MapImporter.Parse(json)).Message;

    // ---- MAP-003: exactly one HQ per Faction

    [Fact]
    public void MAP_003_each_faction_has_exactly_one_hq()
    {
        var map = MapImporter.Parse(MapJson());

        Assert.Equal(new[] { "east", "west" }, map.Factions);
        foreach (var f in map.Factions)
            Assert.Single(map.Locations, l => l.Type == LocationType.Hq && l.Faction == f);
    }

    [Fact]
    public void MAP_003_a_faction_with_no_hq_is_rejected()
    {
        var msg = ImportError(MapJson("{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[5,1],\"faction\":\"east\"}," + StsBlock));

        Assert.Contains("\"west\"", msg);
        Assert.Contains("exactly one HQ", msg);
    }

    [Fact]
    public void MAP_003_a_faction_with_two_hqs_is_rejected()
    {
        var msg = ImportError(MapJson(Hqs + ",{\"id\":\"hq-east-2\",\"type\":\"hq\",\"at\":[4,1],\"faction\":\"east\"}"));

        Assert.Contains("\"east\"", msg);
        Assert.Contains("found 2", msg);
    }

    [Fact]
    public void MAP_003_an_hq_naming_an_unlisted_faction_is_rejected()
    {
        ImportError(MapJson(Hqs + ",{\"id\":\"hq-x\",\"type\":\"hq\",\"at\":[4,1],\"faction\":\"north\"}"));
    }

    // The two-Faction limit is a CC1 constraint (MUL-002, two-player lite), not a MAP-003 rule.
    [Theory]
    [InlineData("[\"east\"]")]
    [InlineData("[\"east\",\"west\",\"north\"]")]
    public void MUL_002_CC1_a_map_must_list_exactly_two_factions(string factions)
    {
        // Locations and setup are made to match the list, so the count is the only problem.
        var hqs = factions.Contains("north") ? Hqs + ",{\"id\":\"hq-north\",\"type\":\"hq\",\"at\":[4,3],\"faction\":\"north\"}" : "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[5,1],\"faction\":\"east\"}";
        var setup = factions.Contains("north")
            ? SetupBlock.Substring(0, SetupBlock.Length - 1) + ",\"north\":{\"reserve\":1,\"brigade\":{\"sp\":1,\"sections\":[\"infantry\",\"infantry\",\"infantry\"]}}}"
            : "{\"east\":{\"reserve\":5,\"brigade\":{\"sp\":90,\"sections\":[\"infantry\",\"infantry\",\"artillery\"]}}}";

        var msg = ImportError(MapJson(hqs, factions, setup));

        Assert.Contains("exactly two Factions", msg);
    }

    [Fact]
    public void MAP_003_a_duplicate_faction_id_is_rejected()
    {
        Assert.Contains("more than once", ImportError(MapJson(factions: "[\"east\",\"east\"]")));
    }

    // ---- MAP-022: three ST types, unknown type rejected

    [Fact]
    public void MAP_022_each_st_has_exactly_one_of_supply_command_major()
    {
        var map = MapImporter.Parse(MapJson());

        Assert.Equal(LocationType.Supply, map.Locations.Single(l => l.Id == "supply-1").Type);
        Assert.Equal(LocationType.Command, map.Locations.Single(l => l.Id == "command-1").Type);
        Assert.Equal(LocationType.Major, map.Locations.Single(l => l.Id == "major-1").Type);
        Assert.Equal(5, map.Locations.Count);
    }

    [Theory]
    [InlineData("depot")]
    [InlineData("fob")]
    [InlineData("Supply")]
    public void MAP_022_MAP_015_an_unknown_st_type_is_rejected(string type)
    {
        var msg = ImportError(MapJson(Hqs + ",{\"id\":\"x\",\"type\":\"" + type + "\",\"at\":[2,2]}"));

        Assert.Contains("unknown location type", msg);
    }

    [Fact]
    public void MAP_022_a_location_without_a_type_is_rejected()
    {
        ImportError(MapJson(Hqs + ",{\"id\":\"x\",\"at\":[2,2]}"));
    }

    [Fact]
    public void MAP_022_bad_locations_are_rejected()
    {
        ImportError(MapJson(Hqs + ",{\"id\":\"hq-east\",\"type\":\"supply\",\"at\":[2,2]}"));        // duplicate id
        ImportError(MapJson(Hqs + ",{\"id\":\"x\",\"type\":\"supply\",\"at\":[6,0]}"));              // outside grid
        ImportError(MapJson(Hqs + ",{\"id\":\"x\",\"type\":\"supply\",\"at\":[1.5,0]}"));            // not whole
        ImportError(MapJson(Hqs + ",{\"id\":\"x y\",\"type\":\"supply\",\"at\":[1,0]}"));            // bad id
        ImportError(MapJson(Hqs + ",{\"id\":\"x\",\"type\":\"supply\",\"at\":[1,0],\"owner\":\"e\"}")); // unknown key
    }

    // ---- MAP-021 / MAP-015: starting position

    [Fact]
    public void MAP_021_each_side_starts_with_only_its_hq_and_its_starting_brigade()
    {
        var game = NewGame();

        foreach (var f in game.Factions)
        {
            var owned = game.Locations.Where(l => l.Owner == f.Id).ToList();
            Assert.Single(owned);
            Assert.Equal(LocationType.Hq, owned[0].Type);
            Assert.Equal(f.Id, owned[0].Location.Faction);
            var brigade = Assert.Single(game.Brigades, b => b.Faction == f.Id);
            Assert.Equal(owned[0].Position, brigade.Position);
        }
        Assert.Equal(2, game.Brigades.Count);
    }

    [Fact]
    public void MAP_021_every_st_starts_neutral()
    {
        var game = NewGame();

        var sts = game.Locations.Where(l => l.Type != LocationType.Hq).ToList();
        Assert.Equal(3, sts.Count);
        Assert.All(sts, l => Assert.Null(l.Owner));
    }

    [Fact]
    public void MAP_021_an_st_with_a_faction_in_the_file_is_rejected()
    {
        ImportError(MapJson(Hqs + ",{\"id\":\"x\",\"type\":\"supply\",\"at\":[2,2],\"faction\":\"east\"}"));
    }

    [Fact]
    public void MAP_021_a_starting_brigade_has_the_named_sections_at_full_health()
    {
        var game = NewGame();
        var east = game.Brigades.Single(b => b.Faction == "east");
        var west = game.Brigades.Single(b => b.Faction == "west");

        Assert.Equal(new[] { "infantry", "infantry", "artillery" }, east.Sections.Select(s => s.TypeId));
        Assert.Equal(new[] { 10, 10, 6 }, east.Sections.Select(s => s.Health));
        Assert.Equal(new[] { "infantry", "infantry", "recon" }, west.Sections.Select(s => s.TypeId));
        Assert.Equal(90, east.SP);
        Assert.Equal(85, west.SP);
        Assert.Equal(5, game.Factions.Single(f => f.Id == "east").Reserve);
        Assert.Equal(11, game.Factions.Single(f => f.Id == "west").Reserve); // 6 from the map setup plus turn 1 income (P-RES-01 = 5)
    }

    [Fact]
    public void MAP_015_supply_sts_start_neutral_and_there_is_no_depot_location_type()
    {
        var game = NewGame();

        Assert.Null(game.Locations.Single(l => l.Type == LocationType.Supply).Owner);
        Assert.Equal(new[] { "Hq", "Supply", "Command", "Major" }, Enum.GetNames(typeof(LocationType)));
    }

    [Fact]
    public void MAP_021_setup_must_cover_each_faction_and_use_known_section_types()
    {
        ImportError(MapJson(setup: "{\"east\":{\"reserve\":5,\"brigade\":{\"sp\":10,\"sections\":[\"infantry\"]}}}")); // west missing
        ImportError(MapJson(setup: SetupBlock.Replace("\"reserve\":5", "\"reserve\":-5")));
        var unlisted = ImportError(MapJson(setup: SetupBlock.Substring(0, SetupBlock.Length - 1) + ",\"north\":{\"reserve\":1,\"brigade\":{\"sp\":1,\"sections\":[\"infantry\"]}}}"));
        Assert.Contains("\"north\"", unlisted);
        Assert.Contains("not a listed Faction", unlisted);
        ImportError(MapJson(setup: SetupBlock.Replace("[\"infantry\",\"infantry\",\"artillery\"]", "[]")));
        var e = Assert.Throws<MapFormatException>(() => NewGame(json: MapJson(setup: SetupBlock.Replace("recon", "tank"))));
        Assert.Contains("tank", e.Message);
    }

    [Fact]
    public void MAP_021_a_map_without_factions_imports_but_cannot_start_a_game()
    {
        var map = MapImporter.Parse("{\"version\":1,\"width\":2,\"height\":2}");

        Assert.Empty(map.Factions);
        Assert.Throws<MapFormatException>(() => new GameState(1, map, Types(), Params()));
    }

    [Fact]
    public void BRG_021_a_starting_brigade_of_more_than_five_sections_is_rejected()
    {
        var six = SetupBlock.Replace("[\"infantry\",\"infantry\",\"artillery\"]", "[\"infantry\",\"infantry\",\"infantry\",\"infantry\",\"infantry\",\"infantry\"]");

        Assert.Contains("BRG-021", ImportError(MapJson(setup: six)));
    }

    [Theory]
    [InlineData("[\"infantry\",\"infantry\"]")]
    [InlineData("[\"infantry\",\"infantry\",\"infantry\",\"infantry\"]")]
    public void P_BRG_05_the_starting_brigade_has_the_parameter_number_of_sections(string sections)
    {
        var json = MapJson(setup: SetupBlock.Replace("[\"infantry\",\"infantry\",\"artillery\"]", sections));

        var e = Assert.Throws<MapFormatException>(() => NewGame(json: json));

        Assert.Contains("P-BRG-05", e.Message);
    }

    [Fact]
    public void P_BRG_05_the_size_is_read_from_the_parameter_table_and_is_decided()
    {
        var shipped = ParameterTable.LoadShipped();

        Assert.Equal(3, shipped.GetInt("P-BRG-05"));
        Assert.False(shipped.IsPlaceholder("P-BRG-05"));
        // A different value in the table changes what the game accepts.
        var two = ParameterTable.Parse("P-BRG-05 = 2\nP-RES-01 = 5\nP-RES-02 = 2\nP-RES-04 = 1\nP-FOW-03 = hq=3 fob=2 major=2 command=1 supply=1\nP-MAP-02 = minus=1 min=1\nP-SUP-01 = 0.75\nP-SUP-02 = 0.25\nP-SUP-10 = 2");
        var json = MapJson(setup: SetupBlock.Replace("[\"infantry\",\"infantry\",\"artillery\"]", "[\"infantry\",\"infantry\"]").Replace("[\"infantry\",\"infantry\",\"recon\"]", "[\"infantry\",\"recon\"]"));
        Assert.Equal(2, new GameState(1, MapImporter.Parse(json), Types(), two).Brigades[0].Sections.Count);
    }

    [Fact]
    public void BRG_026_the_starting_brigade_stands_on_its_own_hq_hex_and_is_in_the_hash()
    {
        var game = NewGame();

        Assert.Equal(HexCoord.FromOffset(5, 1), game.Brigades.Single(b => b.Faction == "east").Position);
        Assert.Equal(HexCoord.FromOffset(0, 2), game.Brigades.Single(b => b.Faction == "west").Position);
        var moved = NewGame();
        moved.Brigades[0].Position = HexCoord.FromOffset(4, 1);
        Assert.NotEqual(game.ComputeHash(), moved.ComputeHash());
    }

    [Fact]
    public void BRG_019_SUP_021_starting_brigades_in_the_samples_do_not_start_over_capacity()
    {
        var types = SectionTypeTable.LoadShipped();
        foreach (var name in new[] { "sample-20x12.json", "sample-50x30.json" })
        {
            var game = new GameState(1, MapImporter.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "maps", name))), types, ParameterTable.LoadShipped());
            foreach (var b in game.Brigades)
                Assert.True(b.SP <= b.Sections.Sum(s => types.Get(s.TypeId).CapacityContribution), name + " Brigade " + b.Id);
        }
    }

    [Fact]
    public void SUP_016_placeholder_profiles_let_a_five_section_brigade_move_10_hexes_and_fight_one_battle()
    {
        // SUP-021: capacity and cost per hex are sums. P-SUP-03 (~30%) and P-SUP-04 (~20%) are proposed placeholders.
        var inf = SectionTypeTable.LoadShipped().Get("infantry");
        var capacity = 5 * inf.CapacityContribution;
        var march10 = 10 * 5 * inf.ConsumptionPerHex;
        var battle = capacity * 20 / 100;

        Assert.InRange(march10 * 100.0 / capacity, 25, 35);
        Assert.True(march10 + battle <= capacity);
        Assert.True(march10 + battle + march10 <= capacity); // and back again (SUP-009), still not empty
    }

    [Fact]
    public void BRG_018_a_brigade_cannot_be_given_sections_from_outside()
    {
        var game = NewGame();

        Assert.False(game.Brigades[0].Sections is List<Section>);
    }

    [Fact]
    public void MAP_021_setup_null_is_an_error_even_without_factions()
    {
        ImportError("{\"version\":1,\"width\":2,\"height\":2,\"setup\":null}");
    }

    // ---- BRG-018: SP is held by the Brigade only

    [Fact]
    public void BRG_018_sp_is_read_and_changed_on_the_brigade()
    {
        var game = NewGame();
        var brigade = game.Brigades[0];

        Assert.Equal(90, brigade.SP);
        brigade.SP = 4; // internal: only commands may do this (DLV-013)
        Assert.Equal(4, brigade.SP);
    }

    [Fact]
    public void BRG_018_a_section_has_no_sp_value()
    {
        var members = typeof(Section).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Select(m => m.Name).ToList();

        Assert.DoesNotContain(members, n => n.Contains("SP", StringComparison.OrdinalIgnoreCase) || n.Contains("Supply", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(typeof(Brigade).GetProperty("SP"));
        Assert.Null(typeof(SectionType).GetProperty("SP"));
    }

    // ---- P-SUP-09: Section types are data

    [Fact]
    public void P_SUP_09_section_types_load_with_health_sight_and_supply_profile()
    {
        var t = Types().Get("recon");

        Assert.Equal((4, 3, 25, 1), (t.Health, t.Sight, t.CapacityContribution, t.ConsumptionPerHex));
        Assert.Equal(new[] { "artillery", "infantry", "recon" }, Types().All.Select(x => x.Id));
    }

    [Fact]
    public void P_SUP_09_shipped_section_types_load_and_are_marked_placeholder()
    {
        var table = SectionTypeTable.LoadShipped();

        Assert.NotEmpty(table.All);
        Assert.All(table.All, t => Assert.InRange(t.Sight, 1, 3));
        var path = Path.Combine(Path.GetDirectoryName(typeof(SectionTypeTable).Assembly.Location)!, "Data", "section-types.json");
        Assert.Contains("placeholder", File.ReadAllText(path));
    }

    [Theory]
    [InlineData("{\"version\":1,\"sectionTypes\":{\"a\":{\"health\":1,\"sight\":4,\"capacityContribution\":1,\"consumptionPerHex\":1}}}")]
    [InlineData("{\"version\":1,\"sectionTypes\":{\"a\":{\"health\":0,\"sight\":1,\"capacityContribution\":1,\"consumptionPerHex\":1}}}")]
    [InlineData("{\"version\":1,\"sectionTypes\":{\"a\":{\"health\":1,\"sight\":1,\"capacityContribution\":1}}}")]
    [InlineData("{\"version\":1,\"sectionTypes\":{\"a\":{\"health\":1,\"sight\":1,\"capacityContribution\":1,\"consumptionPerHex\":1,\"sp\":1}}}")]
    [InlineData("{\"version\":1,\"sectionTypes\":{}}")]
    [InlineData("{\"version\":2,\"sectionTypes\":{}}")]
    [InlineData("{\"sectionTypes\":{}}")]
    public void P_SUP_09_bad_section_type_data_is_rejected(string json)
    {
        Assert.Throws<SectionTypeFormatException>(() => SectionTypeTable.Parse(json));
    }

    // ---- TEC-008: hash includes the map and the new state

    private const string PinnedMapDigest = "d78a38d94569c43e26b19b6139528ed732a262960b562a5059bf5c49d48fb05b";

    [Fact]
    public void TEC_008_the_map_digest_is_pinned_and_independent_of_file_order()
    {
        var map = MapImporter.Parse(MapJson());
        var shuffled = MapImporter.Parse(MapJson(locations: StsBlock + "," + Hqs, factions: "[\"west\",\"east\"]"));

        Assert.Equal(PinnedMapDigest, map.Digest());
        Assert.Equal(map.Digest(), shuffled.Digest());
    }

    [Fact]
    public void TEC_008_a_new_game_canonical_text_and_hash_are_pinned()
    {
        // Pinned values were computed independently in Python from the format, not from this code.
        var game = NewGame();

        Assert.Equal(
            "weirdwar.state.v1\nseed=7\nrandom=11400714819323198492\ntally=0\n" +
            "map=" + PinnedMapDigest + "\n" +
            "parameters=9c9ca9cd914554490a098b1d33b1a779ac96c652bf8b428c8471baf720ed9153\n" +
            "sectiontypes=3682338fa7adc7ba4e6dad930b4292cd2e583523d2e7c16d432aae993ebbcd99\n" +
            "faction=east reserve=5 cp=0\nfaction=west reserve=11 cp=2\n" +
            "location=command-1 owner=-\nlocation=hq-east owner=east\nlocation=hq-west owner=west\n" +
            "location=major-1 owner=-\nlocation=supply-1 owner=-\n" +
            "brigade=1 faction=east sp=90 sections=infantry:10,infantry:10,artillery:6 at=5,1\n" +
            "brigade=2 faction=west sp=85 sections=infantry:10,infantry:10,recon:4 at=0,2\n" +
            "turn=1 active=west phase=Actions\n" +
            "log=0\ndigest=" + new string('0', 64) + "\n",
            game.CanonicalText());
        Assert.Equal("c13dae696ced55df5dde95078e621e9de8a93242e5ece824e6f900999f1cec3b", game.ComputeHash());
    }

    [Fact]
    public void TEC_008_a_new_game_hash_after_a_command_is_pinned()
    {
        var game = NewGame();
        Assert.True(game.ApplyCommand(new AddToTallyCommand(5, "west")).Accepted);

        Assert.Equal("178ac179cc294f340d0514e2cbf3ead065188b1ea022dce6879b13ec7136fa04", game.ComputeHash());
    }

    [Fact]
    public void TRN_007_the_hash_after_the_first_faction_ends_its_turn_is_pinned()
    {
        // Pinned value computed independently in Python: seed 7 gives west first; west ends its turn, east is active on turn 2.
        var game = NewGame();
        Assert.True(game.ApplyCommand(new EndTurnCommand("west")).Accepted);

        Assert.Contains("turn=2 active=east phase=Actions\n", game.CanonicalText());
        Assert.Equal("e8412a6b74368c7c5441245a63ff02caa382720fff466d4359ce4667db578001", game.ComputeHash());
    }

    [Fact]
    public void TEC_008_the_hash_changes_with_ownership_sp_section_health_and_reserve()
    {
        var baseline = NewGame().ComputeHash();

        var owner = NewGame(); owner.Locations.Single(l => l.Id == "supply-1").Owner = "east";
        var sp = NewGame(); sp.Brigades[0].SP = 9;
        var hp = NewGame(); hp.Brigades[0].Sections[0].Health = 9;
        var reserve = NewGame(); reserve.Factions[0].Reserve = 4;

        var all = new[] { baseline, owner.ComputeHash(), sp.ComputeHash(), hp.ComputeHash(), reserve.ComputeHash() };
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Fact]
    public void TEC_008_the_hash_changes_with_the_map_and_the_section_types()
    {
        var baseline = NewGame().ComputeHash();
        var otherMap = NewGame(json: MapJson().Replace("[[1,1]]", "[[2,2]]")).ComputeHash();
        var otherTypes = new GameState(7, MapImporter.Parse(MapJson()), SectionTypeTable.Parse(TypesJson.Replace("\"health\":10", "\"health\":11")), Params()).ComputeHash();

        Assert.NotEqual(baseline, otherMap);
        Assert.NotEqual(baseline, otherTypes);
    }

    [Fact]
    public void TEC_008_the_canonical_text_does_not_grow_with_the_log()
    {
        var game = NewGame();
        var before = game.CanonicalText().Length;
        for (var i = 0; i < 50; i++) Assert.True(game.ApplyCommand(new AddToTallyCommand(1, game.ActiveFaction)).Accepted);

        Assert.True(game.CanonicalText().Length <= before + 8, "text must not grow with the log");
    }

    [Fact]
    public void TEC_008_replay_on_a_map_reproduces_the_hash()
    {
        var game = NewGame();
        Assert.True(game.ApplyCommand(new AddToTallyCommand(2, game.ActiveFaction)).Accepted);
        Assert.True(game.ApplyCommand(new RollIntoTallyCommand(6, game.ActiveFaction)).Accepted);
        var map = MapImporter.Parse(MapJson());

        var replayed = GameState.Replay(7, game.Log, map, Types(), Params());

        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
    }

    // ---- MED-006 / MAP-003: the shipped samples and the documented example include the new keys

    [Theory]
    [InlineData("sample-20x12.json")]
    [InlineData("sample-50x30.json")]
    public void MAP_021_sample_maps_start_a_game_with_the_shipped_section_types(string name)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "maps", name));
        var game = new GameState(1, MapImporter.Parse(text), SectionTypeTable.LoadShipped(), ParameterTable.LoadShipped());

        Assert.Equal(2, game.Brigades.Count);
        Assert.Equal(2, game.Locations.Count(l => l.Owner != null));
        Assert.Equal(4, game.Locations.Count(l => l.Owner == null));
    }

    [Fact]
    public void MED_006_the_format_doc_example_includes_locations_and_starts_a_game()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WeirdWar.Core", "Maps", "MapFormat.md")))
            dir = dir.Parent;
        var doc = File.ReadAllText(Path.Combine(dir!.FullName, "WeirdWar.Core", "Maps", "MapFormat.md"));
        var start = doc.IndexOf("```json\n", StringComparison.Ordinal) + "```json\n".Length;
        var example = doc.Substring(start, doc.IndexOf("```", start, StringComparison.Ordinal) - start);

        var game = new GameState(1, MapImporter.Parse(example), SectionTypeTable.LoadShipped(), ParameterTable.LoadShipped());

        Assert.Equal(5, game.Locations.Count);
        Assert.Equal(2, game.Brigades.Count);
        foreach (var word in new[] { "factions", "locations", "setup", "supply", "command", "major", "hq" })
            Assert.Contains(word, doc);
    }
}
