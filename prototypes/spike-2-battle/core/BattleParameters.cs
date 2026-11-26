using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Tunable values, read from a data file by parameter ID (DLV-014). Format: <c>{"P-CMD-01": 2, "P-CMD-03": 0.5, "P-DAI-01": 25, "P-CMD-04": 3, "P-DAI-02": 6, "P-DAI-03": 2, "P-DAI-04": 0.5}</c>.
    /// Nothing is built into the code: a missing value is an error.
    /// </summary>
    public sealed class BattleParameters
    {
        /// <summary>P-CMD-01: max waypoints a Section can have queued.</summary>
        public int MaxWaypoints { get; }

        /// <summary>P-CMD-03: share of accuracy lost while firing on the move, from 0 (none lost) to 1 (cannot hit).</summary>
        public double MovingAccuracyPenalty { get; }

        /// <summary>P-DAI-01 (DAI-001): how far, in metres, "nearby" cover is for a Section with no orders (placeholder 25 m).</summary>
        public double CoverSearchRange { get; }

        /// <summary>P-CMD-04 (CMD-005): how close, in metres, a waypoint must be to a cover object's edge to count as "next to cover" (placeholder 3 m).</summary>
        public double CoverAdjacencyDistance { get; }

        /// <summary>P-DAI-02 (CMD-010): how far in front of its Section, in metres, an uncontrolled Officer stands (placeholder 6 m).</summary>
        public double OfficerProtectDistance { get; }

        /// <summary>P-DAI-03 (DAI-005): seconds between an uncontrolled Officer's rifle shots (placeholder 2 s).</summary>
        public double OfficerFireInterval { get; }

        /// <summary>P-DAI-04 (DEC-102): chance, 0 to 1, that an uncontrolled Officer's shot hits (placeholder 0.5).</summary>
        public double AiOfficerHitChance { get; }

        public BattleParameters(int maxWaypoints, double movingAccuracyPenalty, double coverSearchRange, double coverAdjacencyDistance,
            double officerProtectDistance, double officerFireInterval, double aiOfficerHitChance)
        {
            OfficerProtectDistance = officerProtectDistance;
            OfficerFireInterval = officerFireInterval;
            AiOfficerHitChance = aiOfficerHitChance;
            CoverAdjacencyDistance = coverAdjacencyDistance;
            CoverSearchRange = coverSearchRange;
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
            if (!root.TryGetValue("P-DAI-01", out var c) || !(c is long || c is double) || System.Convert.ToDouble(c) <= 0)
            {
                throw new CommandFormatException("\"P-DAI-01\" must be a number above 0.");
            }
            if (!root.TryGetValue("P-CMD-04", out var a) || !(a is long || a is double) || System.Convert.ToDouble(a) < 0)
            {
                throw new CommandFormatException("\"P-CMD-04\" must be a number of 0 or more.");
            }
            var protect = Number(root, "P-DAI-02", "a number of 0 or more");
            if (protect < 0) throw new CommandFormatException("\"P-DAI-02\" must be a number of 0 or more.");
            var interval = Number(root, "P-DAI-03", "a number above 0");
            if (interval <= 0) throw new CommandFormatException("\"P-DAI-03\" must be a number above 0.");
            var aiHit = Number(root, "P-DAI-04", "a number from 0 to 1");
            if (aiHit < 0 || aiHit > 1) throw new CommandFormatException("\"P-DAI-04\" must be a number from 0 to 1.");
            return new BattleParameters((int)n, penalty, System.Convert.ToDouble(c), System.Convert.ToDouble(a), protect, interval, aiHit);
        }

        private static double Number(Dictionary<string, object?> root, string key, string what)
        {
            if (root.TryGetValue(key, out var v) && v is long l) return l;
            if (v is double d) return d;
            throw new CommandFormatException("\"" + key + "\" must be " + what + ".");
        }
    }
}
