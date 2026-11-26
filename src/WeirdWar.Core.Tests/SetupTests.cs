using System.Reflection;
using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class SetupTests
{
    private const string TypesJson =
        "{\"version\":1,\"sectionTypes\":{" +
        "\"infantry\":{\"health\":10,\"sight\":1,\"capacityContribution\":2,\"consumptionPerHex\":1}," +
        "\"artillery\":{\"health\":6,\"sight\":1,\"capacityContribution\":1,\"consumptionPerHex\":2}," +
        "\"recon\":{\"health\":4,\"sight\":3,\"capacityContribution\":1,\"consumptionPerHex\":1}}}";

    private const string Hqs =
        "{\"id\":\"hq-east\",\"type\":\"hq\",\"at\":[5,1],\"faction\":\"east\"}," +
        "{\"id\":\"hq-west\",\"type\":\"hq\",\"at\":[0,2],\"faction\":\"west\"}";

    private const string StsBlock =
        "{\"id\":\"supply-1\",\"type\":\"supply\",\"at\":[2,0]}," +
        "{\"id\":\"command-1\",\"type\":\"command\",\"at\":[3,3]}," +
        "{\"id\":\"major-1\",\"type\":\"major\",\"at\":[3,1]}";

    private const string SetupBlock =
        "{\"east\":{\"reserve\":5,\"brigade\":{\"sp\":10,\"sections\":[\"infantry\",\"artillery\"]}}," +
        "\"west\":{\"reserve\":6,\"brigade\":{\"sp\":12,\"sections\":[\"infantry\",\"infantry\",\"recon\"]}}}";

    private static string MapJson(string locations = Hqs + "," + StsBlock, string factions = "[\"east\",\"west\"]", string setup = SetupBlock)
        => "{\"version\":1,\"width\":6,\"height\":4,\"terrain\":{\"mountain\":[[1,1]]},\"factions\":" + factions +
           ",\"locations\":[" + locations + "],\"setup\":" + setup + "}";

    private static SectionTypeTable Types() => SectionTypeTable.Parse(TypesJson);

    private static GameState NewGame(ulong seed = 7, string? json = null)
        => new GameState(seed, MapImporter.Parse(json ?? MapJson()), Types());

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

    [Theory]
    [InlineData("[\"east\"]")]
    [InlineData("[\"east\",\"west\",\"north\"]")]
    [InlineData("[\"east\",\"east\"]")]
    public void MAP_003_the_map_must_have_two_factions(string factions)
    {
        ImportError(MapJson(factions: factions));
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
            Assert.Single(game.Brigades, b => b.Faction == f.Id);
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

        Assert.Equal(new[] { "infantry", "artillery" }, east.Sections.Select(s => s.TypeId));
        Assert.Equal(new[] { 10, 6 }, east.Sections.Select(s => s.Health));
        Assert.Equal(new[] { "infantry", "infantry", "recon" }, west.Sections.Select(s => s.TypeId));
        Assert.Equal(10, east.SP);
        Assert.Equal(12, west.SP);
        Assert.Equal(5, game.Factions.Single(f => f.Id == "east").Reserve);
        Assert.Equal(6, game.Factions.Single(f => f.Id == "west").Reserve);
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
        ImportError(MapJson(setup: SetupBlock.Replace("[\"infantry\",\"artillery\"]", "[]")));
        var e = Assert.Throws<MapFormatException>(() => NewGame(json: MapJson(setup: SetupBlock.Replace("recon", "tank"))));
        Assert.Contains("tank", e.Message);
    }

    [Fact]
    public void MAP_021_a_map_without_factions_imports_but_cannot_start_a_game()
    {
        var map = MapImporter.Parse("{\"version\":1,\"width\":2,\"height\":2}");

        Assert.Empty(map.Factions);
        Assert.Throws<MapFormatException>(() => new GameState(1, map, Types()));
    }

    // ---- BRG-018: SP is held by the Brigade only

    [Fact]
    public void BRG_018_sp_is_read_and_changed_on_the_brigade()
    {
        var game = NewGame();
        var brigade = game.Brigades[0];

        Assert.Equal(10, brigade.SP);
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

        Assert.Equal((4, 3, 1, 1), (t.Health, t.Sight, t.CapacityContribution, t.ConsumptionPerHex));
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
        Assert.Throws<MapFormatException>(() => SectionTypeTable.Parse(json));
    }

    // ---- TEC-008: hash includes the map and the new state

    private const string PinnedMapDigest = "8d2764e6933ee5ee114ee695d68229b928837fd1b19cae8a2aa6e3b306c893fd";

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
            "weirdwar.state.v1\nseed=7\nrandom=7\ntally=0\n" +
            "map=" + PinnedMapDigest + "\n" +
            "sectiontypes=925db20cf015b7328ccf0bcd3e74f26791fa5e167a374311a80fcf7b652496dc\n" +
            "faction=east reserve=5\nfaction=west reserve=6\n" +
            "location=command-1 owner=-\nlocation=hq-east owner=east\nlocation=hq-west owner=west\n" +
            "location=major-1 owner=-\nlocation=supply-1 owner=-\n" +
            "brigade=1 faction=east sp=10 sections=infantry:10,artillery:6\n" +
            "brigade=2 faction=west sp=12 sections=infantry:10,infantry:10,recon:4\n" +
            "log=0\ndigest=" + new string('0', 64) + "\n",
            game.CanonicalText());
        Assert.Equal("8fcb5868670ff09471c78370cc8474096b4b94981c93aa9996919e1d18d02c9c", game.ComputeHash());
    }

    [Fact]
    public void TEC_008_a_new_game_hash_after_a_command_is_pinned()
    {
        var game = NewGame();
        game.ApplyCommand(new AddToTallyCommand(5));

        Assert.Equal("cac732fd97dff7635616cad60a11238aabf35b9525f4e3c922af14635e96072d", game.ComputeHash());
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
        var otherTypes = new GameState(7, MapImporter.Parse(MapJson()), SectionTypeTable.Parse(TypesJson.Replace("\"health\":10", "\"health\":11"))).ComputeHash();

        Assert.NotEqual(baseline, otherMap);
        Assert.NotEqual(baseline, otherTypes);
    }

    [Fact]
    public void TEC_008_the_canonical_text_does_not_grow_with_the_log()
    {
        var game = NewGame();
        var before = game.CanonicalText().Length;
        for (var i = 0; i < 50; i++) game.ApplyCommand(new AddToTallyCommand(1));

        Assert.True(game.CanonicalText().Length <= before + 8, "text must not grow with the log");
    }

    [Fact]
    public void TEC_008_replay_on_a_map_reproduces_the_hash()
    {
        var game = NewGame();
        game.ApplyCommand(new AddToTallyCommand(2));
        game.ApplyCommand(new RollIntoTallyCommand(6));
        var map = MapImporter.Parse(MapJson());

        var replayed = GameState.Replay(7, game.Log, map, Types());

        Assert.Equal(game.ComputeHash(), replayed.ComputeHash());
    }

    // ---- MED-006 / MAP-003: the shipped samples and the documented example include the new keys

    [Theory]
    [InlineData("sample-20x12.json")]
    [InlineData("sample-50x30.json")]
    public void MAP_021_sample_maps_start_a_game_with_the_shipped_section_types(string name)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "maps", name));
        var game = new GameState(1, MapImporter.Parse(text), SectionTypeTable.LoadShipped());

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

        var game = new GameState(1, MapImporter.Parse(example), SectionTypeTable.LoadShipped());

        Assert.Equal(5, game.Locations.Count);
        Assert.Equal(2, game.Brigades.Count);
        foreach (var word in new[] { "factions", "locations", "setup", "supply", "command", "major", "hq" })
            Assert.Contains(word, doc);
    }
}
