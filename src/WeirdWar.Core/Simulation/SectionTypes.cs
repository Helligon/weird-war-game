using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;

namespace WeirdWar.Core.Simulation
{
    /// <summary>Thrown when the Section type data is malformed or invalid.</summary>
    public sealed class SectionTypeFormatException : Exception
    {
        public SectionTypeFormatException(string message) : base(message) { }
    }

    /// <summary>
    /// Data for one Section type: health, sight (FOW-004) and the supply profile (P-SUP-09). Only the data shape
    /// is defined here; the capacity maths comes later. Values are placeholders.
    /// </summary>
    public sealed class SectionType
    {
        internal SectionType(string id, int health, int sight, int capacityContribution, int consumptionPerHex)
        {
            Id = id;
            Health = health;
            Sight = sight;
            CapacityContribution = capacityContribution;
            ConsumptionPerHex = consumptionPerHex;
        }

        public string Id { get; }
        public int Health { get; }
        public int Sight { get; }
        public int CapacityContribution { get; }
        public int ConsumptionPerHex { get; }
    }

    /// <summary>
    /// The Section types, read from Data/section-types.json through the strict JSON reader. Per-type parameters
    /// are an open question, so this is a small data file and not one parameter ID per value.
    /// </summary>
    public sealed class SectionTypeTable
    {
        private readonly SortedDictionary<string, SectionType> _types;

        private SectionTypeTable(SortedDictionary<string, SectionType> types) { _types = types; }

        public IEnumerable<SectionType> All => _types.Values;

        public bool TryGet(string id, out SectionType type) => _types.TryGetValue(id, out type!);

        public SectionType Get(string id)
        {
            if (!_types.TryGetValue(id, out var t)) throw new KeyNotFoundException("Unknown Section type \"" + id + "\"");
            return t;
        }

        public static SectionTypeTable Parse(string json)
        {
            object? root;
            try { root = Json.Parse(json); }
            catch (JsonException e) { throw new SectionTypeFormatException("Section types are not valid JSON: " + e.Message); }
            var obj = root as Dictionary<string, object?> ?? throw new SectionTypeFormatException("Section types must be a JSON object");
            foreach (var key in obj.Keys)
                if (key != "version" && key != "status" && key != "sectionTypes")
                    throw new SectionTypeFormatException("Unknown key \"" + key + "\" in section types (expected version, status, sectionTypes)");
            if (!(obj.TryGetValue("version", out var v) && v is long ver && ver == 1))
                throw new SectionTypeFormatException("Section types must have \"version\": 1");
            var list = obj.TryGetValue("sectionTypes", out var s) ? s as Dictionary<string, object?> : null;
            if (list == null || list.Count == 0) throw new SectionTypeFormatException("\"sectionTypes\" must be an object with at least one type");

            var types = new SortedDictionary<string, SectionType>(StringComparer.Ordinal);
            foreach (var kv in list)
            {
                if (!MapIds.IsValid(kv.Key)) throw new SectionTypeFormatException("Section type id \"" + kv.Key + "\" may only use letters, digits, - and _");
                var o = kv.Value as Dictionary<string, object?> ?? throw new SectionTypeFormatException("Section type \"" + kv.Key + "\" must be an object");
                foreach (var key in o.Keys)
                    if (key != "health" && key != "sight" && key != "capacityContribution" && key != "consumptionPerHex")
                        throw new SectionTypeFormatException("Unknown key \"" + key + "\" in section type \"" + kv.Key + "\"");
                var sight = Num(o, kv.Key, "sight");
                if (sight < 1 || sight > 3) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": sight must be 1 to 3 (FOW-004, P-FOW-01)");
                var health = Num(o, kv.Key, "health");
                if (health < 1) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": health must be at least 1");
                types[kv.Key] = new SectionType(kv.Key, health, sight, Num(o, kv.Key, "capacityContribution"), Num(o, kv.Key, "consumptionPerHex"));
            }
            return new SectionTypeTable(types);
        }

        public static SectionTypeTable Load(string path) => Parse(File.ReadAllText(path));

        /// <summary>Loads Data/section-types.json shipped next to the Core assembly (same rules as ParameterTable.LoadShipped).</summary>
        public static SectionTypeTable LoadShipped()
        {
            var location = typeof(SectionTypeTable).Assembly.Location;
            if (string.IsNullOrEmpty(location))
                throw new InvalidOperationException("Cannot locate the shipped section types: Assembly.Location is empty. Use SectionTypeTable.Parse or Load.");
            return Load(Path.Combine(Path.GetDirectoryName(location)!, "Data", "section-types.json"));
        }

        private static int Num(Dictionary<string, object?> o, string type, string key)
        {
            if (!o.TryGetValue(key, out var v)) throw new SectionTypeFormatException("Section type \"" + type + "\": missing \"" + key + "\"");
            if (!(v is long n) || n < 0 || n > 1000000) throw new SectionTypeFormatException("Section type \"" + type + "\": \"" + key + "\" must be a whole number from 0 to 1000000");
            return (int)n;
        }

        /// <summary>Stable text form, types sorted by id, for hashing (TEC-008).</summary>
        internal string CanonicalText()
        {
            var sb = new StringBuilder();
            foreach (var t in _types.Values)
                sb.Append("type=").Append(t.Id)
                  .Append(" health=").Append(t.Health.ToString(CultureInfo.InvariantCulture))
                  .Append(" sight=").Append(t.Sight.ToString(CultureInfo.InvariantCulture))
                  .Append(" capacity=").Append(t.CapacityContribution.ToString(CultureInfo.InvariantCulture))
                  .Append(" consumption=").Append(t.ConsumptionPerHex.ToString(CultureInfo.InvariantCulture)).Append('\n');
            return sb.ToString();
        }
    }
}
