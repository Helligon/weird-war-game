using System;
using System.Collections.Generic;
using System.Linq;

namespace WeirdWar.Core.Simulation
{
    /// <summary>Supply state of a Brigade (SUP-003).</summary>
    public enum SupplyState
    {
        Overextended,
        Low,
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
        private readonly double _wellSuppliedAbove;
        private readonly double _lowBelow;

        /// <summary>Reads P-SUP-01 (Well Supplied threshold) and P-SUP-02 (Low threshold) from the parameter table (DLV-014).</summary>
        public Supply(SectionTypeTable sectionTypes, ParameterTable parameters)
        {
            _types = sectionTypes ?? throw new ArgumentNullException(nameof(sectionTypes));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            _wellSuppliedAbove = parameters.GetDouble("P-SUP-01");
            _lowBelow = parameters.GetDouble("P-SUP-02");
            if (!(_lowBelow >= 0 && _lowBelow < _wellSuppliedAbove))
                throw new ParameterFormatException("P-SUP-02 must be at least 0 and below P-SUP-01 (SUP-003).");
        }

        /// <summary>Maximum SP: the sum of the Sections' capacity contributions (SUP-001, SUP-019, SUP-021, P-SUP-09).</summary>
        public int CapacityOf(Brigade brigade)
            => Check(brigade).Sections.Sum(s => _types.Get(s.TypeId).CapacityContribution);

        /// <summary>SP cost per hex of movement: the sum of the Sections' consumption (SUP-019, SUP-021, P-SUP-09).</summary>
        public int CostPerHexOf(Brigade brigade)
            => Check(brigade).Sections.Sum(s => _types.Get(s.TypeId).ConsumptionPerHex);

        /// <summary>
        /// SUP-003, measured against the Brigade's own capacity (SUP-002). Overextended: SP below 0.
        /// Well Supplied: SP above P-SUP-01 of capacity. Low: SP below P-SUP-02 of capacity. Otherwise Supplied.
        /// SP exactly on a threshold is neither Well Supplied nor Low, and SP of 0 is Low (or Supplied if capacity is 0), never Overextended.
        /// </summary>
        public SupplyState StateOf(Brigade brigade)
        {
            var capacity = CapacityOf(brigade);
            var sp = brigade.SP;
            if (sp < 0) return SupplyState.Overextended;
            if (sp > _wellSuppliedAbove * capacity) return SupplyState.WellSupplied;
            if (sp < _lowBelow * capacity) return SupplyState.Low;
            return SupplyState.Supplied;
        }

        /// <summary>The supply state of every Brigade in the game, in Brigade id order (SUP-004).</summary>
        public IReadOnlyList<KeyValuePair<int, SupplyState>> StatesOf(GameState game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            return game.Brigades.Select(b => new KeyValuePair<int, SupplyState>(b.Id, StateOf(b))).ToList();
        }

        private static Brigade Check(Brigade brigade) => brigade ?? throw new ArgumentNullException(nameof(brigade));
    }
}
