using System;
using System.Linq;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// Fog of war queries (FOW-004, FOW-006). Read-only: it changes no state and nothing here is hashed (MUL-014, TEC-008).
    /// Built once at game start from the Section type data.
    /// </summary>
    public sealed class FogOfWar
    {
        private readonly SectionTypeTable _types;

        public FogOfWar(SectionTypeTable sectionTypes)
        {
            _types = sectionTypes ?? throw new ArgumentNullException(nameof(sectionTypes));
        }

        /// <summary>
        /// A Brigade's sight in hexes: the best <see cref="SectionType.Sight"/> among its current Sections (FOW-006).
        /// 0 for a Brigade with no Sections (it has nothing to see with).
        /// </summary>
        public int BrigadeSight(Brigade brigade)
        {
            if (brigade == null) throw new ArgumentNullException(nameof(brigade));
            return brigade.Sections.Select(s => _types.Get(s.TypeId).Sight).DefaultIfEmpty(0).Max();
        }
    }
}
