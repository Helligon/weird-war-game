using WeirdWar.Core.Maps;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Tests;

public class SupplyTests
{
    private static readonly SectionTypeTable Types = SectionTypeTable.LoadShipped();
    private static readonly ParameterTable Params = ParameterTable.LoadShipped();
    private static readonly Supply Supply = new Supply(Types, Params);

    private const string OneTypeJson =
        "{\"version\":1,\"sectionTypes\":{\"x\":{\"health\":1,\"sight\":1,\"capacityContribution\":100,\"consumptionPerHex\":1}}}";

    private static Brigade Make(int sp, params string[] sections)
        => new Brigade(1, "east", sp, HexCoord.FromOffset(0, 0), sections.Select(t => new Section(t, 1)).ToList());

    private static Brigade Infantry(int n, int sp = 0) => Make(sp, Enumerable.Repeat("infantry", n).ToArray());

    [Fact]
    public void SUP_001_a_brigade_has_a_capacity()
    {
        Assert.True(Supply.CapacityOf(Infantry(3)) > 0);
    }

    [Fact]
    public void SUP_021_capacity_and_cost_are_sums_of_the_section_profiles()
    {
        var types = SectionTypeTable.Parse("{\"version\":1,\"sectionTypes\":{" +
            "\"a\":{\"health\":1,\"sight\":1,\"capacityContribution\":40,\"consumptionPerHex\":3}," +
            "\"b\":{\"health\":1,\"sight\":2,\"capacityContribution\":7,\"consumptionPerHex\":5}}}");
        var supply = new Supply(types, Params);

        var brigade = Make(0, "a", "a", "b");

        Assert.Equal(87, supply.CapacityOf(brigade));
        Assert.Equal(11, supply.CostPerHexOf(brigade));
    }

    [Fact]
    public void SUP_021_shipped_placeholder_profiles_sum()
    {
        var brigade = Make(0, "infantry", "infantry", "artillery");

        Assert.Equal(105, Supply.CapacityOf(brigade));
        Assert.Equal(4, Supply.CostPerHexOf(brigade));
    }

    [Fact]
    public void SUP_019_more_sections_give_higher_capacity_and_higher_cost_per_hex()
    {
        var small = Infantry(3);
        var large = Infantry(5);

        Assert.True(Supply.CapacityOf(large) > Supply.CapacityOf(small));
        Assert.True(Supply.CostPerHexOf(large) > Supply.CostPerHexOf(small));
    }

    [Fact]
    public void SUP_002_state_is_measured_against_each_brigades_own_capacity()
    {
        // 70 SP is 100% of 2 infantry (capacity 70) but 40% of 5 infantry (capacity 175).
        Assert.Equal(SupplyState.WellSupplied, Supply.StateOf(Infantry(2, 70)));
        Assert.Equal(SupplyState.Supplied, Supply.StateOf(Infantry(5, 70)));
    }

    [Theory]
    [InlineData(76, SupplyState.WellSupplied)]
    [InlineData(100, SupplyState.WellSupplied)]
    [InlineData(75, SupplyState.Supplied)]   // exactly on P-SUP-01: not above
    [InlineData(50, SupplyState.Supplied)]
    [InlineData(25, SupplyState.Supplied)]   // exactly on P-SUP-02: not below
    [InlineData(24, SupplyState.Low)]
    [InlineData(1, SupplyState.Low)]
    [InlineData(0, SupplyState.Low)]         // zero is Low, not Overextended
    [InlineData(-1, SupplyState.Overextended)]
    [InlineData(-50, SupplyState.Overextended)]
    public void SUP_003_supply_state_follows_the_thresholds(int sp, SupplyState expected)
    {
        // Capacity 100, so SP equals the percentage of capacity.
        var supply = new Supply(SectionTypeTable.Parse(OneTypeJson), Params);

        Assert.Equal(expected, supply.StateOf(Make(sp, "x")));
    }

    [Fact]
    public void SUP_003_thresholds_are_read_from_P_SUP_01_and_P_SUP_02()
    {
        var supply = new Supply(SectionTypeTable.Parse(OneTypeJson), ParameterTable.Parse("P-SUP-01 = 0.5\nP-SUP-02 = 0.1\n"));

        Assert.Equal(SupplyState.WellSupplied, supply.StateOf(Make(51, "x")));
        Assert.Equal(SupplyState.Supplied, supply.StateOf(Make(50, "x")));
        Assert.Equal(SupplyState.Supplied, supply.StateOf(Make(10, "x")));
        Assert.Equal(SupplyState.Low, supply.StateOf(Make(9, "x")));
    }

    [Fact]
    public void SUP_003_inconsistent_thresholds_are_rejected()
    {
        Assert.Throws<ParameterFormatException>(() => new Supply(Types, ParameterTable.Parse("P-SUP-01 = 0.2\nP-SUP-02 = 0.5\n")));
    }

    [Fact]
    public void SUP_003_the_shipped_thresholds_are_the_decided_values_and_the_proposed_ones_are_placeholders()
    {
        Assert.Equal(0.75, Params.GetDouble("P-SUP-01"));
        Assert.Equal(0.25, Params.GetDouble("P-SUP-02"));
        Assert.False(Params.IsPlaceholder("P-SUP-01"));
        Assert.False(Params.IsPlaceholder("P-SUP-02"));
        Assert.True(Params.IsPlaceholder("P-SUP-03"));
        Assert.True(Params.IsPlaceholder("P-SUP-04"));
    }

    [Fact]
    public void SUP_004_the_core_reports_every_brigades_supply_state()
    {
        var map = MapImporter.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "maps", "sample-20x12.json")));
        var game = new GameState(1, map, Types, Params);
        var before = game.ComputeHash();

        var states = Supply.StatesOf(game);

        Assert.Equal(game.Brigades.Select(b => b.Id), states.Select(s => s.Key));
        Assert.Equal(game.Brigades.Select(b => Supply.StateOf(b)), states.Select(s => s.Value));
        Assert.Equal(before, game.ComputeHash()); // derived on demand: the hash is unchanged
    }

    [Fact]
    public void SUP_016_a_full_five_section_brigade_can_move_10_hexes_and_fight_a_battle()
    {
        var brigade = Infantry(5);
        var capacity = Supply.CapacityOf(brigade);
        var march = 10 * Supply.CostPerHexOf(brigade);
        var battle = Params.GetDouble("P-SUP-04") * capacity; // proposed placeholder, data only

        Assert.True(march + battle <= capacity, "10 hexes " + march + " + battle " + battle + " exceeds capacity " + capacity);
    }

    [Fact]
    public void SUP_008_march_battle_and_march_back_leave_sp_and_do_not_overextend()
    {
        var capacity = Supply.CapacityOf(Infantry(5));
        var march = Params.GetDouble("P-SUP-03") * capacity;   // proposed placeholders, data only
        var battle = Params.GetDouble("P-SUP-04") * capacity;

        var left = capacity - march - battle - march;

        Assert.True(left > 0, "SP left " + left);
        // SUP-009 predicts about 20% left, which is Low and not Overextended.
        Assert.Equal(SupplyState.Low, Supply.StateOf(Infantry(5, (int)Math.Floor(left))));
    }

    [Fact]
    public void SUP_003_a_non_dyadic_threshold_is_exact_on_its_boundary()
    {
        // 0.70 * 90 is 62.99999999999999 in double; exactly 70% of 90 is 63, which is not above the threshold.
        var types = SectionTypeTable.Parse("{\"version\":1,\"sectionTypes\":{\"x\":{\"health\":1,\"sight\":1,\"capacityContribution\":90,\"consumptionPerHex\":1}}}");
        var supply = new Supply(types, ParameterTable.Parse("P-SUP-01 = 0.70\nP-SUP-02 = 0.25\n"));

        Assert.Equal(SupplyState.Supplied, supply.StateOf(Make(63, "x")));
        Assert.Equal(SupplyState.WellSupplied, supply.StateOf(Make(64, "x")));
    }

    [Fact]
    public void SUP_004_states_of_a_game_need_a_supply_built_from_the_games_own_section_types()
    {
        var map = MapImporter.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "maps", "sample-20x12.json")));
        var game = new GameState(1, map, Types, Params);
        var other = new Supply(SectionTypeTable.LoadShipped(), Params);

        Assert.Throws<ArgumentException>(() => other.StatesOf(game));
        Assert.Equal(game.Brigades.Count, Supply.StatesOf(game).Count);
    }
}
