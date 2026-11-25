using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Tunable values, read from a data file by parameter ID (DLV-014). Format: <c>{"P-CMD-01": 2}</c>.
    /// Nothing is built into the code: a missing value is an error.
    /// </summary>
    public sealed class BattleParameters
    {
        /// <summary>P-CMD-01: max waypoints a Section can have queued.</summary>
        public int MaxWaypoints { get; }

        public BattleParameters(int maxWaypoints) { MaxWaypoints = maxWaypoints; }

        public static BattleParameters Load(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object?>
                ?? throw new CommandFormatException("The parameter file must be a JSON object.");
            if (!root.TryGetValue("P-CMD-01", out var v) || !(v is long n) || n < 1 || n > 1000)
            {
                throw new CommandFormatException("\"P-CMD-01\" must be a whole number from 1 to 1000.");
            }
            return new BattleParameters((int)n);
        }
    }
}
