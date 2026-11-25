using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Tunable values, read from a data file by parameter ID (DLV-014). Format: <c>{"P-CMD-01": 2, "P-CMD-03": 0.5}</c>.
    /// Nothing is built into the code: a missing value is an error.
    /// </summary>
    public sealed class BattleParameters
    {
        /// <summary>P-CMD-01: max waypoints a Section can have queued.</summary>
        public int MaxWaypoints { get; }

        /// <summary>P-CMD-03: share of accuracy lost while firing on the move, from 0 (none lost) to 1 (cannot hit).</summary>
        public double MovingAccuracyPenalty { get; }

        public BattleParameters(int maxWaypoints, double movingAccuracyPenalty)
        {
            MaxWaypoints = maxWaypoints;
            MovingAccuracyPenalty = movingAccuracyPenalty;
        }

        public static BattleParameters Load(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object?>
                ?? throw new CommandFormatException("The parameter file must be a JSON object.");
            if (!root.TryGetValue("P-CMD-01", out var v) || !(v is long n) || n < 1 || n > 1000)
            {
                throw new CommandFormatException("\"P-CMD-01\" must be a whole number from 1 to 1000.");
            }
            double penalty;
            root.TryGetValue("P-CMD-03", out var p);
            if (p is long pl) penalty = pl;
            else if (p is double pd) penalty = pd;
            else throw new CommandFormatException("\"P-CMD-03\" must be a number from 0 to 1.");
            if (penalty < 0 || penalty > 1) throw new CommandFormatException("\"P-CMD-03\" must be a number from 0 to 1.");
            return new BattleParameters((int)n, penalty);
        }
    }
}
