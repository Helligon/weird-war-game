using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;

namespace WeirdWar.Core.Simulation
{
    /// <summary>Thrown when the Card data is malformed or invalid.</summary>
    public sealed class CardFormatException : Exception
    {
        public CardFormatException(string message) : base(message) { }
    }

    /// <summary>The pool a Card is drawn from (CRD-011). Only the shared Core pool is built; Faction-only pools come with their Cards.</summary>
    public enum CardPool { Core }

    /// <summary>
    /// What a Card does when played (CRD-010). Each Card's own Story supplies one; a Card with none has no play condition and no
    /// effect. Fixed by Card id in code, so it is not state. <see cref="Apply"/> MUST NOT throw after <see cref="CanPlay"/> returned null.
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>Null if the Card may be played now with this target (CRD-010), otherwise the reason it may not. MUST NOT change the state.</summary>
        string? CanPlay(GameState state, FactionState player, string target);

        /// <summary>Carries out the Card's effect. CP has been spent and the Card has left the hand when this runs.</summary>
        void Apply(GameState state, FactionState player, string target);
    }

    /// <summary>Data for one Card: name, pool (CRD-011), CP cost and copies (P-CRD-08), and the single-use flag. Effects come with each Card's own Story.</summary>
    public sealed class CardType
    {
        internal CardType(string id, string name, CardPool pool, int cpCost, int copies, bool singleUse)
        {
            Id = id; Name = name; Pool = pool; CpCost = cpCost; Copies = copies; SingleUse = singleUse;
        }

        public string Id { get; }
        public string Name { get; }
        public CardPool Pool { get; }

        /// <summary>CP to play the Card (P-CRD-08, open: a placeholder in the data).</summary>
        public int CpCost { get; }

        /// <summary>Copies of the Card in each Faction's deck at the start (P-CRD-08, open: a placeholder in the data).</summary>
        public int Copies { get; }

        public bool SingleUse { get; }

        /// <summary>The Card's play condition and effect, or null for none. Attached from the fixed table when the Card table is built; not in the data file.</summary>
        public ICardEffect? Effect { get; internal set; }
    }

    /// <summary>
    /// The Cards, read from Data/cards.json through the strict JSON reader (like section-types.json). The shipped file has no
    /// Cards: each Card is added by the Story that builds its effect. A Card with no copies or no CP cost is a load error.
    /// </summary>
    public sealed class CardTable
    {
        private readonly SortedDictionary<string, CardType> _cards;

        /// <summary>
        /// The effect of each Card, fixed by Card id in code (CRD-010, TEC-008), so a game and its replay cannot be given different
        /// effects. Each Card's own Story adds its entry. A Card with no entry has no play condition and no effect.
        /// </summary>
        private static readonly Dictionary<string, ICardEffect> Effects = new Dictionary<string, ICardEffect>();

        private CardTable(SortedDictionary<string, CardType> cards)
        {
            _cards = cards;
            foreach (var card in cards.Values)
                if (Effects.TryGetValue(card.Id, out var effect)) card.Effect = effect;
        }

        /// <summary>Cards sorted by id (ordinal).</summary>
        public IEnumerable<CardType> All => _cards.Values;

        public int Count => _cards.Count;

        public bool TryGet(string id, out CardType card) => _cards.TryGetValue(id, out card!);

        /// <summary>Test only: replaces a Card's effect. Game code gets its effects from the fixed table when the Card table is built.</summary>
        internal void SetEffect(string id, ICardEffect effect)
        {
            if (!_cards.TryGetValue(id, out var card)) throw new ArgumentException("There is no Card " + id);
            card.Effect = effect;
        }

        public static CardTable Parse(string json)
        {
            object? root;
            try { root = Json.Parse(json); }
            catch (JsonException e) { throw new CardFormatException("Card data is not valid JSON: " + e.Message); }
            var obj = root as Dictionary<string, object?> ?? throw new CardFormatException("Card data must be a JSON object");
            foreach (var key in obj.Keys)
                if (key != "version" && key != "status" && key != "placeholders" && key != "cards")
                    throw new CardFormatException("Unknown key \"" + key + "\" in Card data (expected version, status, placeholders, cards)");
            if (!(obj.TryGetValue("version", out var v) && v is long ver && ver == 1))
                throw new CardFormatException("Card data must have \"version\": 1");
            if (obj.TryGetValue("placeholders", out var ph))
            {
                var markers = ph as Dictionary<string, object?> ?? throw new CardFormatException("\"placeholders\" must be an object of text markers");
                foreach (var m in markers)
                    if (!(m.Value is string)) throw new CardFormatException("Placeholder marker \"" + m.Key + "\" must be text");
            }
            var list = obj.TryGetValue("cards", out var c) ? c as Dictionary<string, object?> : null;
            if (list == null) throw new CardFormatException("\"cards\" must be an object (it may be empty)");

            var cards = new SortedDictionary<string, CardType>(StringComparer.Ordinal);
            foreach (var kv in list)
            {
                if (!MapIds.IsValid(kv.Key)) throw new CardFormatException("Card id \"" + kv.Key + "\" may only use letters, digits, - and _");
                var o = kv.Value as Dictionary<string, object?> ?? throw new CardFormatException("Card \"" + kv.Key + "\" must be an object");
                foreach (var key in o.Keys)
                    if (key != "name" && key != "pool" && key != "cpCost" && key != "copies" && key != "singleUse")
                        throw new CardFormatException("Unknown key \"" + key + "\" in Card \"" + kv.Key + "\"");
                if (!o.TryGetValue("name", out var nv) || !(nv is string name) || name.Length == 0)
                    throw new CardFormatException("Card \"" + kv.Key + "\": missing \"name\"");
                if (name.IndexOf('\n') >= 0 || name.IndexOf('\r') >= 0 || name.Contains(" pool="))
                    throw new CardFormatException("Card \"" + kv.Key + "\": the name must not contain a line break or the text \" pool=\"");
                if (!o.TryGetValue("pool", out var pv) || !(pv is string pool))
                    throw new CardFormatException("Card \"" + kv.Key + "\": missing \"pool\" (Core, CRD-011)");
                if (pool != "Core")
                    throw new CardFormatException("Card \"" + kv.Key + "\": unknown pool \"" + pool + "\" (only Core is built, CRD-011)");
                var cpCost = Num(o, kv.Key, "cpCost");
                if (cpCost < 1) throw new CardFormatException("Card \"" + kv.Key + "\": cpCost must be at least 1 (CRD-001: playing a Card costs CP)");
                var copies = Num(o, kv.Key, "copies");
                if (copies < 1) throw new CardFormatException("Card \"" + kv.Key + "\": copies must be at least 1 (P-CRD-08)");
                var singleUse = false;
                if (o.TryGetValue("singleUse", out var su))
                    singleUse = su as bool? ?? throw new CardFormatException("Card \"" + kv.Key + "\": \"singleUse\" must be true or false");
                cards[kv.Key] = new CardType(kv.Key, name, CardPool.Core, cpCost, copies, singleUse);
            }
            return new CardTable(cards);
        }

        public static CardTable Load(string path) => Parse(File.ReadAllText(path));

        /// <summary>Loads Data/cards.json shipped next to the Core assembly.</summary>
        public static CardTable LoadShipped()
        {
            var location = typeof(CardTable).Assembly.Location;
            if (string.IsNullOrEmpty(location))
                throw new InvalidOperationException("Cannot locate the shipped Card data: Assembly.Location is empty. Use CardTable.Parse or Load.");
            return Load(Path.Combine(Path.GetDirectoryName(location)!, "Data", "cards.json"));
        }

        private static int Num(Dictionary<string, object?> o, string card, string key)
        {
            if (!o.TryGetValue(key, out var v)) throw new CardFormatException("Card \"" + card + "\": missing \"" + key + "\" (P-CRD-08)");
            if (!(v is long n) || n < 0 || n > 1000000) throw new CardFormatException("Card \"" + card + "\": \"" + key + "\" must be a whole number from 0 to 1000000");
            return (int)n;
        }

        /// <summary>Stable text form, Cards sorted by id, for hashing (TEC-008). Whole numbers and ids only (TEC-019).</summary>
        internal string CanonicalText()
        {
            var sb = new StringBuilder();
            foreach (var t in _cards.Values)
                sb.Append("card=").Append(t.Id)
                  .Append(" name=").Append(t.Name)
                  .Append(" pool=").Append(t.Pool.ToString())
                  .Append(" cp=").Append(t.CpCost.ToString(CultureInfo.InvariantCulture))
                  .Append(" copies=").Append(t.Copies.ToString(CultureInfo.InvariantCulture))
                  .Append(t.SingleUse ? " singleuse=1" : "").Append('\n');
            return sb.ToString();
        }
    }
}
