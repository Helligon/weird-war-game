using System.Collections.Generic;

namespace WeirdWar.Spike1.Core
{
    /// <summary>Reads a scenario JSON file into a <see cref="GameConfig"/>. Format: maps/README.md.</summary>
    public static class GameConfigLoader
    {
        public static GameConfig Load(string json)
        {
            object? parsed;
            try
            {
                parsed = MiniJson.Parse(json);
            }
            catch (MapLoadException e)
            {
                throw new SetupException("Scenario error: " + e.Message);
            }
            var root = parsed as Dictionary<string, object?>
                ?? throw new SetupException("The scenario file must be a JSON object.");

            // Defaults to Player 1 when missing (stand-in for TRN-033, DEC-087).
            long f = 1;
            if (root.TryGetValue("firstPlayer", out var fp))
            {
                if (!(fp is long given) || (given != 1 && given != 2))
                {
                    throw new SetupException("\"firstPlayer\" must be 1 or 2.");
                }
                f = given;
            }
            if (!root.TryGetValue("startingBudget", out var sb) || !(sb is long b) || b < 1 || b > 10000)
            {
                throw new SetupException("\"startingBudget\" must be a whole number from 1 to 10000.");
            }
            // P-SUP-09 is open: the per-hex cost is data, defaulting to 1.
            long cost = 1;
            if (root.TryGetValue("movementCostPerHex", out var mc))
            {
                if (!(mc is long c) || c < 1 || c > 10000)
                {
                    throw new SetupException("\"movementCostPerHex\" must be a whole number from 1 to 10000.");
                }
                cost = c;
            }
            var tokens = root.TryGetValue("tokens", out var t) ? t as Dictionary<string, object?> : null;
            if (tokens == null)
            {
                throw new SetupException("\"tokens\" must be an object with keys \"1\" and \"2\".");
            }
            return new GameConfig((PlayerId)(int)f, (int)b, ReadStart(tokens, "1"), ReadStart(tokens, "2"), (int)cost);
        }

        private static OffsetCoord ReadStart(Dictionary<string, object?> tokens, string key)
        {
            if (tokens.TryGetValue(key, out var v) && v is List<object?> pair && pair.Count == 2
                && pair[0] is long c && pair[1] is long r && c >= int.MinValue && c <= int.MaxValue && r >= int.MinValue && r <= int.MaxValue)
            {
                return new OffsetCoord((int)c, (int)r);
            }
            throw new SetupException("Token \"" + key + "\" must be a [column, row] pair of whole numbers.");
        }
    }
}
