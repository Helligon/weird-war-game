using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Unit stats read from the stats data file (never hard-coded, DLV-014). Every value is a placeholder: the
    /// real stats are open (docs/03-forces/standard-roster.md and hierarchy.md, Parameters). A missing value is an error.
    /// </summary>
    public sealed class UnitStats
    {
        /// <summary>Riflemen walking speed in metres per second (placeholder; open, Q-ROS-01).</summary>
        public double RiflemenSpeed { get; }
        /// <summary>P-HIE-01 for Riflemen: Units in a full-strength Section.</summary>
        public int RiflemenMaxUnits { get; }
        /// <summary>P-HIE-02 for Riflemen: health of one Unit.</summary>
        public int RiflemenHealthPerUnit { get; }
        /// <summary>Riflemen fire range in metres (placeholder; open, Q-ROS-01).</summary>
        public double RiflemenRange { get; }
        /// <summary>Damage dealt by one hit from one Riflemen Unit (placeholder; open, Q-ROS-01).</summary>
        public int RiflemenDamage { get; }
        /// <summary>Chance, 0 to 1, that one Riflemen shot hits while standing still (placeholder; open, Q-ROS-01).</summary>
        public double RiflemenAccuracy { get; }
        /// <summary>Officer health (placeholder; open, Q-ROS-01).</summary>
        public int OfficerHealth { get; }
        /// <summary>Officer walking speed in metres per second (placeholder; open).</summary>
        public double OfficerSpeed { get; }
        /// <summary>Officer hitscan rifle range in metres (placeholder; open).</summary>
        public double OfficerRifleRange { get; }
        /// <summary>Damage of one Officer rifle hit (placeholder; open).</summary>
        public int OfficerRifleDamage { get; }
        /// <summary>How close, in metres, a shot must pass to an enemy Section's centre to hit it (placeholder; open).</summary>
        public double OfficerRifleHitRadius { get; }
        /// <summary>Seconds between one Riflemen volley and the next (stand-in for a rate of fire; DEC-098).</summary>
        public double RiflemenVolleyInterval { get; }
        /// <summary>P-DAI-01 (DAI-001): how far, in metres, a Section looks for "nearby" cover when it has no orders (placeholder 25 m).</summary>
        public double CoverSearchRange { get; }
        /// <summary>CMD-005: how close, in metres, a waypoint must be to a cover object's edge to count as "next to cover" (placeholder, no P- ID; DEC-096).</summary>
        public double CoverAdjacencyDistance { get; }

        public UnitStats(double riflemenSpeed, int riflemenMaxUnits, int riflemenHealthPerUnit, double riflemenRange,
            int riflemenDamage, double riflemenAccuracy, int officerHealth, double riflemenVolleyInterval, double coverSearchRange,
            double officerSpeed, double officerRifleRange, int officerRifleDamage, double officerRifleHitRadius, double coverAdjacencyDistance)
        {
            OfficerSpeed = officerSpeed;
            OfficerRifleRange = officerRifleRange;
            OfficerRifleDamage = officerRifleDamage;
            OfficerRifleHitRadius = officerRifleHitRadius;
            RiflemenSpeed = riflemenSpeed;
            RiflemenMaxUnits = riflemenMaxUnits;
            RiflemenHealthPerUnit = riflemenHealthPerUnit;
            RiflemenRange = riflemenRange;
            RiflemenDamage = riflemenDamage;
            RiflemenAccuracy = riflemenAccuracy;
            OfficerHealth = officerHealth;
            RiflemenVolleyInterval = riflemenVolleyInterval;
            CoverSearchRange = coverSearchRange;
            CoverAdjacencyDistance = coverAdjacencyDistance;
        }

        /// <summary>
        /// Format: { "riflemen": { "speed", "P-HIE-01", "P-HIE-02", "range", "damage", "accuracy", "volleyInterval", "P-DAI-01", "coverAdjacencyDistance" }, "officer": { "health", "speed", "rifleRange", "rifleDamage", "rifleHitRadius" } }.
        /// </summary>
        public static UnitStats Load(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object?>
                ?? throw new DataLoadException("The stats file must be a JSON object.");
            var riflemen = Obj(root, "riflemen");
            var officer = Obj(root, "officer");

            var speed = BattleMapLoader.Number(riflemen, "speed", "riflemen");
            if (speed <= 0) throw new DataLoadException("Stats error: riflemen \"speed\" must be above zero.");
            var maxUnits = Whole(riflemen, "P-HIE-01", "riflemen");
            var healthPerUnit = Whole(riflemen, "P-HIE-02", "riflemen");
            var range = BattleMapLoader.Number(riflemen, "range", "riflemen");
            if (range <= 0) throw new DataLoadException("Stats error: riflemen \"range\" must be above zero.");
            var damage = Whole(riflemen, "damage", "riflemen");
            var accuracy = BattleMapLoader.Number(riflemen, "accuracy", "riflemen");
            if (accuracy < 0 || accuracy > 1) throw new DataLoadException("Stats error: riflemen \"accuracy\" must be from 0 to 1.");
            var interval = BattleMapLoader.Number(riflemen, "volleyInterval", "riflemen");
            if (interval <= 0) throw new DataLoadException("Stats error: riflemen \"volleyInterval\" must be above zero.");
            var coverRange = BattleMapLoader.Number(riflemen, "P-DAI-01", "riflemen");
            if (coverRange <= 0) throw new DataLoadException("Stats error: riflemen \"P-DAI-01\" must be above zero.");
            var adjacency = BattleMapLoader.Number(riflemen, "coverAdjacencyDistance", "riflemen");
            if (adjacency < 0) throw new DataLoadException("Stats error: riflemen \"coverAdjacencyDistance\" must not be negative.");
            var officerHealth = Whole(officer, "health", "officer");
            var officerSpeed = BattleMapLoader.Number(officer, "speed", "officer");
            if (officerSpeed <= 0) throw new DataLoadException("Stats error: officer \"speed\" must be above zero.");
            var rifleRange = BattleMapLoader.Number(officer, "rifleRange", "officer");
            if (rifleRange <= 0) throw new DataLoadException("Stats error: officer \"rifleRange\" must be above zero.");
            var rifleDamage = Whole(officer, "rifleDamage", "officer");
            var hitRadius = BattleMapLoader.Number(officer, "rifleHitRadius", "officer");
            if (hitRadius <= 0) throw new DataLoadException("Stats error: officer \"rifleHitRadius\" must be above zero.");
            return new UnitStats(speed, maxUnits, healthPerUnit, range, damage, accuracy, officerHealth, interval, coverRange,
                officerSpeed, rifleRange, rifleDamage, hitRadius, adjacency);
        }

        private static Dictionary<string, object?> Obj(Dictionary<string, object?> root, string key)
        {
            if (!root.TryGetValue(key, out var v) || !(v is Dictionary<string, object?> o))
                throw new DataLoadException("Stats error: missing \"" + key + "\" object.");
            return o;
        }

        // Keeps units x health far below int.MaxValue.
        private const int MaxValue = 10000;

        private static int Whole(Dictionary<string, object?> o, string key, string what)
        {
            var n = BattleMapLoader.Number(o, key, what);
            if (n < 1 || n != System.Math.Floor(n) || n > MaxValue)
                throw new DataLoadException("Stats error: " + what + " \"" + key + "\" must be a whole number from 1 to " + MaxValue + ".");
            return (int)n;
        }
    }
}
