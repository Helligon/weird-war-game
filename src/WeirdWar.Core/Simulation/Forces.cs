using System.Collections.Generic;
using System.Collections.ObjectModel;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>A Faction in a running game, with its SP reserve and CP (starting values from P-RES-05 and P-RES-06).</summary>
    public sealed class FactionState
    {
        internal FactionState(string id, int reserve, int cp) { Id = id; Reserve = reserve; Cp = cp; }
        public string Id { get; }

        /// <summary>The central SP reserve (RES-001). Sections hold no SP.</summary>
        public int Reserve { get; internal set; }

        /// <summary>Command Points held by the player (RES-005). Spending them on Cards is a later Feature.</summary>
        public int Cp { get; internal set; }

        /// <summary>How many Supply STs this Faction has upgraded to FOBs; the next upgrade costs more CP (MAP-012, MAP-025). In the hash.</summary>
        public int FobUpgrades { get; internal set; }

        /// <summary>Card ids still in the deck, top first (CRD-003). Empty without Card data. In the hash.</summary>
        public IReadOnlyList<string> Deck => _deck;
        internal readonly List<string> _deck = new List<string>();

        /// <summary>Card ids in hand (CRD-003, CRD-004). In the hash.</summary>
        public IReadOnlyList<string> Hand => _hand;
        internal readonly List<string> _hand = new List<string>();

        /// <summary>Card ids in the discard pile (CRD-003). Starts empty. In the hash.</summary>
        public IReadOnlyList<string> Discard => _discard;
        internal readonly List<string> _discard = new List<string>();
    }

    /// <summary>A location in a running game with its current owner (MAP-021: only HQs start owned).</summary>
    public sealed class LocationState
    {
        internal LocationState(Location location, string? owner) { Location = location; Owner = owner; }
        public Location Location { get; }
        public string Id => Location.Id;
        public LocationType Type => Upgraded ? LocationType.Fob : Location.Type;

        /// <summary>True once a Supply ST here has been upgraded to an FOB (MAP-012). In the hash.</summary>
        public bool Upgraded { get; internal set; }
        public HexCoord Position => Location.Position;

        /// <summary>Faction id of the owner, or null when neutral.</summary>
        public string? Owner { get; internal set; }

        /// <summary>
        /// The Faction with a capture pending here (TRN-009): one of its Brigades ended that Faction's own turn on this ST.
        /// It is resolved, and cleared, at the start of that Faction's next turn. Null when no capture is pending.
        /// </summary>
        public string? Capturing { get; internal set; }

        /// <summary>
        /// The id of the Brigade that started the pending capture (TRN-009). Only that Brigade completes it. Null exactly
        /// when <see cref="Capturing"/> is null.
        /// </summary>
        public int? CapturingBrigade { get; internal set; }
    }

    /// <summary>
    /// One Section: a type and its current health. It holds no SP (BRG-018); SP belongs to the Brigade.
    /// The supply profile and sight come from the <see cref="SectionType"/>.
    /// </summary>
    public sealed class Section
    {
        internal Section(string typeId, int health) { TypeId = typeId; Health = health; }
        public string TypeId { get; }
        public int Health { get; internal set; }
    }

    /// <summary>A Brigade: a list of Sections plus its own SP (BRG-018). Officers and titles come later.</summary>
    public sealed class Brigade
    {
        internal Brigade(int id, string faction, int sp, HexCoord position, List<Section> sections)
        {
            Id = id;
            Faction = faction;
            SP = sp;
            Position = position;
            _sections = sections;
            Sections = new ReadOnlyCollection<Section>(sections);
        }

        public int Id { get; }
        public string Faction { get; }

        /// <summary>The Brigade's SP. Sections never hold SP (BRG-018).</summary>
        public int SP { get; internal set; }

        /// <summary>
        /// Where the Brigade stands. The starting Brigade starts on its Faction's HQ hex (by analogy with BRG-026,
        /// which covers formed Brigades; an assumption for the design owner). Only MoveBrigadeCommand moves it.
        /// </summary>
        public HexCoord Position { get; internal set; }

        /// <summary>Hexes moved on the turn <see cref="MovedTurn"/> (TRN-030, SUP-024). Meaningful only when MovedTurn is the current turn.</summary>
        internal int MovedHexes { get; set; }

        /// <summary>The <see cref="GameState.TurnNumber"/> that <see cref="MovedHexes"/> counts. 0 if the Brigade has never moved.</summary>
        internal int MovedTurn { get; set; }

        /// <summary>Hexes this Brigade has moved so far in the turn numbered <paramref name="turn"/> (0 if it has not moved that turn).</summary>
        public int HexesMovedIn(int turn) => MovedTurn == turn ? MovedHexes : 0;

        private readonly List<Section> _sections;

        /// <summary>Removes a Section. Go through <see cref="GameState.RemoveSectionFromBrigade"/>, so an empty Brigade is disbanded.</summary>
        internal void RemoveSection(int index) => _sections.RemoveAt(index);

        /// <summary>Adds a Section at the end (BRG-016). The size limit is checked by the command (BRG-021).</summary>
        internal void AddSection(Section section) => _sections.Add(section);

        /// <summary>Read-only view: Sections change only through commands (DLV-013).</summary>
        public IReadOnlyList<Section> Sections { get; }
    }
}
