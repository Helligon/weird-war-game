using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Core.Tests;

// MAP-017: the campaign map uses a hex grid.
// Scheme: flat-top hexes, stored as odd-q offset (column, row), with axial/cube maths.
public class HexCoordTests
{
    [Fact]
    public void MAP_017_neighbours_returns_six_distinct_hexes()
    {
        var hex = new HexCoord(3, -2);

        var neighbours = hex.Neighbours();

        Assert.Equal(6, neighbours.Length);
        Assert.Equal(6, neighbours.Distinct().Count());
        Assert.DoesNotContain(hex, neighbours);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]   // odd column
    [InlineData(2, 7)]   // even column
    [InlineData(29, 49)] // far corner of a 30x50 map
    public void MAP_017_neighbours_returns_six_hexes_at_distance_one(int column, int row)
    {
        var hex = HexCoord.FromOffset(new OffsetCoord(column, row));

        foreach (var neighbour in hex.Neighbours())
        {
            Assert.Equal(1, hex.DistanceTo(neighbour));
        }
    }

    [Fact]
    public void MAP_017_neighbours_of_even_and_odd_columns_match_odd_q_layout()
    {
        // Flat-top, odd-q: odd columns sit half a hex lower than even columns.
        var evenExpected = new[]
        {
            new OffsetCoord(2, 4), new OffsetCoord(2, 6),             // up, down
            new OffsetCoord(1, 4), new OffsetCoord(1, 5),             // left: up-left, down-left
            new OffsetCoord(3, 4), new OffsetCoord(3, 5),             // right: up-right, down-right
        };
        var oddExpected = new[]
        {
            new OffsetCoord(3, 4), new OffsetCoord(3, 6),
            new OffsetCoord(2, 5), new OffsetCoord(2, 6),
            new OffsetCoord(4, 5), new OffsetCoord(4, 6),
        };

        var evenActual = HexCoord.FromOffset(new OffsetCoord(2, 5)).Neighbours().Select(h => h.ToOffset());
        var oddActual = HexCoord.FromOffset(new OffsetCoord(3, 5)).Neighbours().Select(h => h.ToOffset());

        Assert.Equal(evenExpected.OrderBy(o => o.Column).ThenBy(o => o.Row), evenActual.OrderBy(o => o.Column).ThenBy(o => o.Row));
        Assert.Equal(oddExpected.OrderBy(o => o.Column).ThenBy(o => o.Row), oddActual.OrderBy(o => o.Column).ThenBy(o => o.Row));
    }

    [Fact]
    public void MAP_017_distance_to_itself_is_zero()
    {
        var hex = HexCoord.FromOffset(new OffsetCoord(7, 11));

        Assert.Equal(0, hex.DistanceTo(hex));
    }

    [Theory]
    // (fromCol, fromRow, toCol, toRow, expected steps)
    [InlineData(0, 0, 1, 0, 1)]   // even -> odd column, neighbour
    [InlineData(1, 0, 2, 1, 1)]   // odd -> even column, neighbour (down-right)
    [InlineData(1, 3, 2, 2, 2)]   // across parity, not a neighbour
    [InlineData(0, 0, 2, 0, 2)]   // same row, two columns: zig-zag
    [InlineData(0, 0, 3, 0, 3)]
    [InlineData(0, 0, 0, 5, 5)]   // straight down a column
    [InlineData(0, 0, 4, 4, 6)]   // diagonal then straight
    [InlineData(29, 49, 0, 0, 64)] // corner to corner of a 30x50 map
    public void MAP_017_distance_equals_shortest_path_steps(int fromCol, int fromRow, int toCol, int toRow, int expected)
    {
        var from = HexCoord.FromOffset(new OffsetCoord(fromCol, fromRow));
        var to = HexCoord.FromOffset(new OffsetCoord(toCol, toRow));

        Assert.Equal(expected, from.DistanceTo(to));
        Assert.Equal(expected, to.DistanceTo(from));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 10)]
    [InlineData(14, 25)]
    [InlineData(29, 49)]
    public void MAP_017_distance_matches_breadth_first_search_on_30_by_50_grid(int startCol, int startRow)
    {
        // Independent check: BFS over the grid using Neighbours() gives the true step count.
        const int columns = 30, rows = 50;
        var start = HexCoord.FromOffset(new OffsetCoord(startCol, startRow));
        var steps = new Dictionary<HexCoord, int> { [start] = 0 };
        var queue = new Queue<HexCoord>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in current.Neighbours())
            {
                var offset = next.ToOffset();
                if (offset.Column < 0 || offset.Column >= columns || offset.Row < 0 || offset.Row >= rows) continue;
                if (steps.ContainsKey(next)) continue;
                steps[next] = steps[current] + 1;
                queue.Enqueue(next);
            }
        }

        Assert.Equal(columns * rows, steps.Count);
        foreach (var (hex, bfsSteps) in steps)
        {
            Assert.Equal(bfsSteps, start.DistanceTo(hex));
        }
    }

    [Fact]
    public void MAP_017_offset_round_trip_on_30_by_50_grid()
    {
        for (var column = 0; column < 30; column++)
        {
            for (var row = 0; row < 50; row++)
            {
                var original = new OffsetCoord(column, row);

                var roundTripped = HexCoord.FromOffset(original).ToOffset();

                Assert.Equal(original, roundTripped);
            }
        }
    }

    [Fact]
    public void MAP_017_offset_round_trip_handles_negative_coordinates()
    {
        foreach (var original in new[] { new OffsetCoord(-1, -1), new OffsetCoord(-3, 2), new OffsetCoord(-4, -7) })
        {
            Assert.Equal(original, HexCoord.FromOffset(original).ToOffset());
        }
    }
}
