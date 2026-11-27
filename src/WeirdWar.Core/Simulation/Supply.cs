using System;
using System.Collections.Generic;
using System.Linq;

namespace WeirdWar.Core.Simulation
{
    /// <summary>Supply state of a Brigade (SUP-003).</summary>
    public enum SupplyState
    {
        Overextended,
        Supplied,
        WellSupplied
    }

    /// <summary>
    /// Supply capacity, cost per hex and supply state of Brigades. Everything is derived on demand from the
    /// Brigade's Sections and SP, so no state is stored and the game hash is unchanged. Supply state is read
    /// from the core here; showing it is a UI matter (SUP-004).
    /// </summary>
    public sealed class Supply
    {
        private readonly SectionTypeTable _types;
        private readonly decimal _wellSuppliedAbove;
        private readonly decimal _overextendedBelow;

        /// <summary>P-SUP-08: how far from an owned Supply ST, Major ST or HQ a Brigade can be resupplied, in hexes (SUP-017).</summary>
        public int ResupplyRange { get; }

        /// <summary>
        /// The owned location types that resupply (SUP-012, SUP-013, SUP-017, MAP-015): HQ, Supply ST, Major ST and FOB.
        /// Command STs do not. FOBs resupply too (MAP-008).
        /// </summary>
        public static readonly IReadOnlyList<Maps.LocationType> ResupplyLocationTypes =
            new[] { Maps.LocationType.Hq, Maps.LocationType.Supply, Maps.LocationType.Major, Maps.LocationType.Fob };

        /// <summary>Reads P-SUP-01 (Well Supplied threshold) and P-SUP-02 (Overextended threshold) from the parameter table (DLV-014).</summary>
        public Supply(SectionTypeTable sectionTypes, ParameterTable parameters)
        {
            _types = sectionTypes ?? throw new ArgumentNullException(nameof(sectionTypes));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            _wellSuppliedAbove = parameters.GetDecimal("P-SUP-01");
            _overextendedBelow = parameters.GetDecimal("P-SUP-02");
            ResupplyRange = parameters.GetInt("P-SUP-08");
            if (ResupplyRange < 0) throw new ParameterFormatException("P-SUP-08 must not be negative.");
            if (!(_overextendedBelow >= 0 && _overextendedBelow < _wellSuppliedAbove))
                throw new ParameterFormatException("P-SUP-02 must be at least 0 and below P-SUP-01 (SUP-003).");
        }

        /// <summary>Maximum SP: the sum of the Sections' capacity contributions (SUP-001, SUP-019, SUP-021, P-SUP-09).</summary>
        public int CapacityOf(Brigade brigade)
            => Check(brigade).Sections.Sum(s => _types.Get(s.TypeId).CapacityContribution);

        /// <summary>SP cost per hex of movement: the sum of the Sections' consumption (SUP-019, SUP-021, P-SUP-09).</summary>
        public int CostPerHexOf(Brigade brigade)
            => Check(brigade).Sections.Sum(s => _types.Get(s.TypeId).ConsumptionPerHex);

        /// <summary>
        /// SUP-003, measured against the Brigade's own capacity (SUP-002). Well Supplied: SP above P-SUP-01 of capacity.
        /// Overextended: SP below P-SUP-02 of capacity (SUP-007), so SP of 0 is Overextended. Otherwise Supplied.
        /// SP cannot go below 0 (SUP-006). Comparisons are exact (decimal), so a threshold like 0.70 is exact on its boundary (TEC-008).
        /// SP exactly on a threshold is neither Well Supplied nor Overextended.
        /// </summary>
        public SupplyState StateOf(Brigade brigade)
        {
            var capacity = CapacityOf(brigade);
            var sp = brigade.SP;
            if ((decimal)sp > _wellSuppliedAbove * capacity) return SupplyState.WellSupplied;
            if ((decimal)sp < _overextendedBelow * capacity) return SupplyState.Overextended;
            return SupplyState.Supplied;
        }

        /// <summary>The supply state of every Brigade in the game, in Brigade id order (SUP-004).</summary>
        public IReadOnlyList<KeyValuePair<int, SupplyState>> StatesOf(GameState game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (game.SectionTypes != null && !ReferenceEquals(game.SectionTypes, _types))
                throw new ArgumentException("This Supply was built with different Section types than the game uses.", nameof(game));
            return game.Brigades.Select(b => new KeyValuePair<int, SupplyState>(b.Id, StateOf(b))).ToList();
        }

        private static Brigade Check(Brigade brigade) => brigade ?? throw new ArgumentNullException(nameof(brigade));
    }
}
