using System.Collections.Generic;
using System.Collections.ObjectModel;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>A Faction in a running game, with its SP reserve (placeholder starting value from the map setup).</summary>
    public sealed class FactionState
    {
        internal FactionState(string id, int reserve) { Id = id; Reserve = reserve; }
        public string Id { get; }
        public int Reserve { get; internal set; }
    }

    /// <summary>A location in a running game with its current owner (MAP-021: only HQs start owned).</summary>
    public sealed class LocationState
    {
        internal LocationState(Location location, string? owner) { Location = location; Owner = owner; }
        public Location Location { get; }
        public string Id => Location.Id;
        public LocationType Type => Location.Type;
        public HexCoord Position => Location.Position;

        /// <summary>Faction id of the owner, or null when neutral.</summary>
        public string? Owner { get; internal set; }
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
            Sections = new ReadOnlyCollection<Section>(sections);
        }

        public int Id { get; }
        public string Faction { get; }

        /// <summary>The Brigade's SP. Sections never hold SP (BRG-018).</summary>
        public int SP { get; internal set; }

        /// <summary>
        /// Where the Brigade stands. The starting Brigade starts on its Faction's HQ hex (by analogy with BRG-026,
        /// which covers formed Brigades; an assumption for the design owner). Moving it is a later Story.
        /// </summary>
        public HexCoord Position { get; internal set; }

        /// <summary>Read-only view: Sections change only through commands (DLV-013).</summary>
        public IReadOnlyList<Section> Sections { get; }
    }
}
