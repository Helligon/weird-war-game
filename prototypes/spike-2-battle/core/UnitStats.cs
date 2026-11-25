using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Unit stats read from the stats data file (never hard-coded). Only the Riflemen speed so far.
    /// The value is a placeholder: the real stat is open (docs/03-forces/standard-roster.md, Parameters).
    /// </summary>
    public sealed class UnitStats
    {
        /// <summary>Riflemen walking speed in metres per second (placeholder; open, see standard-roster Parameters).</summary>
        public double RiflemenSpeed { get; }

        public UnitStats(double riflemenSpeed) { RiflemenSpeed = riflemenSpeed; }

        /// <summary>Format: { "riflemen": { "speed": metres per second } }.</summary>
        public static UnitStats Load(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object?>
                ?? throw new DataLoadException("The stats file must be a JSON object.");
            if (!root.TryGetValue("riflemen", out var r) || !(r is Dictionary<string, object?> riflemen))
                throw new DataLoadException("Stats error: missing \"riflemen\" object.");
            var speed = BattleMapLoader.Number(riflemen, "speed", "riflemen");
            if (speed <= 0) throw new DataLoadException("Stats error: riflemen \"speed\" must be above zero.");
            return new UnitStats(speed);
        }
    }
}
