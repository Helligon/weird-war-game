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

    /// <summary>The Section class (TAG-001).</summary>
    public enum SectionClass { Infantry, Heavy, Artillery, Support }

    /// <summary>
    /// Data for one Section type: class, SP cost, health, Units, sight (FOW-004) and the supply profile (P-SUP-09). Only the data shape
    /// is defined here; the capacity maths comes later. Values are placeholders.
    /// </summary>
    public sealed class SectionType
    {
        internal SectionType(string id, SectionClass sectionClass, int spCost, int healthPerUnit, int maxUnits, int health, int sight, int capacityContribution, int consumptionPerHex, bool slow = false, bool strange = false)
        {
            Class = sectionClass;
            SpCost = spCost;
            HealthPerUnit = healthPerUnit;
            MaxUnits = maxUnits;
            Strange = strange;
            Id = id;
            Health = health;
            Sight = sight;
            CapacityContribution = capacityContribution;
            ConsumptionPerHex = consumptionPerHex;
            Slow = slow;
        }

        public string Id { get; }

        /// <summary>Infantry, Heavy, Artillery or Support (TAG-001).</summary>
        public SectionClass Class { get; }

        /// <summary>The SP cost of one Section of this type (BRG-034, P-BRG-02). Only the data; charging it comes later.</summary>
        public int SpCost { get; }

        /// <summary>Health of one Unit (HIE-002, P-HIE-02). Data only: nothing caps health by <see cref="MaxUnits"/> yet.</summary>
        public int HealthPerUnit { get; }

        /// <summary>Most Units a Section of this type has (P-HIE-01).</summary>
        public int MaxUnits { get; }

        /// <summary>The Strange tag, optional in the data and false when absent. TAG-004 and BRG-030 count Strange Sections.</summary>
        public bool Strange { get; }

        /// <summary>Heavy weaponry (TAG-012) is derived from the class, not stored per type.</summary>
        public bool IsHeavyWeaponry => Class == SectionClass.Heavy || Class == SectionClass.Artillery;

        /// <summary>Units at a given health: they follow health, rounding up, so 0 only at 0 health (HIE-002, HIE-004, HIE-010).</summary>
        public int UnitsAt(int health) => health <= 0 ? 0 : (health + HealthPerUnit - 1) / HealthPerUnit;

        public int Health { get; }
        public int Sight { get; }
        public int CapacityContribution { get; }
        public int ConsumptionPerHex { get; }

        /// <summary>The Slow tag (TAG-010): a Section with it caps its Brigade's hexes per turn (SUP-024, P-SUP-10). Optional in the data, false when absent.</summary>
        public bool Slow { get; }
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

        /// <summary>Units of a Section, from its health (HIE-002, HIE-004, HIE-010).</summary>
        public int UnitsOf(Section section) => Get(section.TypeId).UnitsAt(section.Health);

        /// <summary>TAG-012: true for the Heavy and Artillery classes.</summary>
        public static bool IsHeavyWeaponry(SectionType type) => type.IsHeavyWeaponry;

        public static SectionTypeTable Parse(string json)
        {
            object? root;
            try { root = Json.Parse(json); }
            catch (JsonException e) { throw new SectionTypeFormatException("Section types are not valid JSON: " + e.Message); }
            var obj = root as Dictionary<string, object?> ?? throw new SectionTypeFormatException("Section types must be a JSON object");
            foreach (var key in obj.Keys)
                if (key != "version" && key != "status" && key != "placeholders" && key != "sectionTypes")
                    throw new SectionTypeFormatException("Unknown key \"" + key + "\" in section types (expected version, status, placeholders, sectionTypes)");
            if (!(obj.TryGetValue("version", out var v) && v is long ver && ver == 1))
                throw new SectionTypeFormatException("Section types must have \"version\": 1");
            if (obj.TryGetValue("placeholders", out var ph))
            {
                var markers = ph as Dictionary<string, object?> ?? throw new SectionTypeFormatException("\"placeholders\" must be an object of text markers");
                foreach (var m in markers)
                    if (!(m.Value is string)) throw new SectionTypeFormatException("Placeholder marker \"" + m.Key + "\" must be text");
            }
            var list = obj.TryGetValue("sectionTypes", out var s) ? s as Dictionary<string, object?> : null;
            if (list == null || list.Count == 0) throw new SectionTypeFormatException("\"sectionTypes\" must be an object with at least one type");

            var types = new SortedDictionary<string, SectionType>(StringComparer.Ordinal);
            foreach (var kv in list)
            {
                if (!MapIds.IsValid(kv.Key)) throw new SectionTypeFormatException("Section type id \"" + kv.Key + "\" may only use letters, digits, - and _");
                var o = kv.Value as Dictionary<string, object?> ?? throw new SectionTypeFormatException("Section type \"" + kv.Key + "\" must be an object");
                foreach (var key in o.Keys)
                    if (key != "class" && key != "spCost" && key != "healthPerUnit" && key != "maxUnits" && key != "strange" && key != "health" && key != "sight" && key != "capacityContribution" && key != "consumptionPerHex" && key != "slow")
                        throw new SectionTypeFormatException("Unknown key \"" + key + "\" in section type \"" + kv.Key + "\"");
                if (!o.TryGetValue("class", out var cv) || !(cv is string cs))
                    throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": missing \"class\" (Infantry, Heavy, Artillery or Support, TAG-001)");
                if (!(cs == "Infantry" || cs == "Heavy" || cs == "Artillery" || cs == "Support") || !Enum.TryParse<SectionClass>(cs, out var sectionClass))
                    throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": unknown class \"" + cs + "\" (Infantry, Heavy, Artillery or Support, TAG-001)");
                var spCost = Num(o, kv.Key, "spCost");
                var healthPerUnit = Num(o, kv.Key, "healthPerUnit");
                if (healthPerUnit < 1) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": healthPerUnit must be at least 1 (HIE-002)");
                var maxUnits = Num(o, kv.Key, "maxUnits");
                if (maxUnits < 1) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": maxUnits must be at least 1 (HIE-002)");
                var strange = false;
                if (o.TryGetValue("strange", out var strangeValue))
                    strange = strangeValue as bool? ?? throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": \"strange\" must be true or false (TAG-004)");
                var sight = Num(o, kv.Key, "sight");
                if (sight < 1 || sight > 3) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": sight must be 1 to 3 (FOW-004, P-FOW-01)");
                var health = Num(o, kv.Key, "health");
                if (health < 1) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": health must be at least 1");
                var slow = false;
                if (o.TryGetValue("slow", out var slowValue))
                    slow = slowValue as bool? ?? throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": \"slow\" must be true or false (TAG-010)");
                var consumption = Num(o, kv.Key, "consumptionPerHex");
                if (consumption < 1) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": consumptionPerHex must be at least 1, because a Brigade MUST spend SP to move (SUP-005, RES-004)");
                var capacity = Num(o, kv.Key, "capacityContribution");
                if (capacity < 1) throw new SectionTypeFormatException("Section type \"" + kv.Key + "\": capacityContribution must be at least 1 (SUP-005)");
                types[kv.Key] = new SectionType(kv.Key, sectionClass, spCost, healthPerUnit, maxUnits, health, sight, capacity, consumption, slow, strange);
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
                  .Append(" consumption=").Append(t.ConsumptionPerHex.ToString(CultureInfo.InvariantCulture))
                  .Append(" class=").Append(t.Class.ToString())
                  .Append(" spcost=").Append(t.SpCost.ToString(CultureInfo.InvariantCulture))
                  .Append(" hpu=").Append(t.HealthPerUnit.ToString(CultureInfo.InvariantCulture))
                  .Append(" maxunits=").Append(t.MaxUnits.ToString(CultureInfo.InvariantCulture))
                  // Tags are only written when set.
                  .Append(t.Slow ? " slow=1" : "").Append(t.Strange ? " strange=1" : "").Append('\n');
            return sb.ToString();
        }
    }
}
