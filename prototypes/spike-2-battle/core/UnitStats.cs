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
        /// <summary>Seconds between one Riflemen volley and the next (stand-in for a rate of fire; DEC-098).</summary>
        public double RiflemenVolleyInterval { get; }
        /// <summary>How far, in metres, a Section looks for cover when it has no orders (placeholder; DAI-001 says only "nearby").</summary>
        public double CoverSearchRange { get; }

        public UnitStats(double riflemenSpeed, int riflemenMaxUnits, int riflemenHealthPerUnit, double riflemenRange,
            int riflemenDamage, double riflemenAccuracy, int officerHealth, double riflemenVolleyInterval, double coverSearchRange)
        {
            RiflemenSpeed = riflemenSpeed;
            RiflemenMaxUnits = riflemenMaxUnits;
            RiflemenHealthPerUnit = riflemenHealthPerUnit;
            RiflemenRange = riflemenRange;
            RiflemenDamage = riflemenDamage;
            RiflemenAccuracy = riflemenAccuracy;
            OfficerHealth = officerHealth;
            RiflemenVolleyInterval = riflemenVolleyInterval;
            CoverSearchRange = coverSearchRange;
        }

        /// <summary>
        /// Format: { "riflemen": { "speed", "P-HIE-01", "P-HIE-02", "range", "damage", "accuracy", "volleyInterval", "coverSearchRange" }, "officer": { "health" } }.
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
            var coverRange = BattleMapLoader.Number(riflemen, "coverSearchRange", "riflemen");
            if (coverRange <= 0) throw new DataLoadException("Stats error: riflemen \"coverSearchRange\" must be above zero.");
            var officerHealth = Whole(officer, "health", "officer");
            return new UnitStats(speed, maxUnits, healthPerUnit, range, damage, accuracy, officerHealth, interval, coverRange);
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
