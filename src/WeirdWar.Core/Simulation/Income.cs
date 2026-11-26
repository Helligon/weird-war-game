using System;
using System.Linq;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>One turn's income for a Faction: SP for the reserve and CP for the player's total (TRN-005, RES-001).</summary>
    public readonly struct IncomeAmount
    {
        public IncomeAmount(int sp, int cp) { Sp = sp; Cp = cp; }
        public int Sp { get; }
        public int Cp { get; }
    }

    /// <summary>
    /// SP and CP income (RES-002, RES-005, RES-010, RES-011, MAP-001, MAP-022, MAP-024, TRN-016). Reads its values by
    /// parameter ID (DLV-014), all at construction, so a missing value fails at once with <see cref="MissingParameterException"/>.
    /// Money is whole numbers.
    ///   P-RES-01: base SP per turn. P-RES-02: base CP per turn. Both are paid by the HQ (MAP-024, RES-011).
    ///   P-RES-04: the bonus one held ST gives, in each currency its type pays: Supply SP, Command CP, Major both.
    /// P-MAP-05 and P-MAP-06 are not read: it is not confirmed that they equal P-RES-01/02 and P-RES-04. They stay in the data file only.
    /// </summary>
    public sealed class IncomeRules
    {
        private readonly int _baseSp;
        private readonly int _baseCp;
        private readonly int _stBonus;

        public IncomeRules(ParameterTable parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            _baseSp = NonNegative(parameters, "P-RES-01");
            _baseCp = NonNegative(parameters, "P-RES-02");
            _stBonus = NonNegative(parameters, "P-RES-04");
        }

        private static int NonNegative(ParameterTable parameters, string id)
        {
            var v = parameters.GetInt(id);
            if (v < 0) throw new ParameterFormatException(id + " = " + v + " must not be negative.");
            return v;
        }

        /// <summary>
        /// The income <paramref name="faction"/> is paid this turn: the HQ's base, plus the bonus of each ST it owns
        /// whose type pays that currency, except an ST with an enemy Brigade on its hex (TRN-016). Neutral STs pay
        /// nothing (MAP-001). The HQ itself always pays (RES-011).
        /// </summary>
        public IncomeAmount Calculate(GameState state, string faction)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var sp = _baseSp;
            var cp = _baseCp;
            foreach (var l in state.Locations)
            {
                if (l.Owner != faction) continue;
                if (l.Type == LocationType.Hq) continue;
                if (state.Brigades.Any(b => b.Faction != faction && b.Position.Equals(l.Position))) continue;
                if (l.Type == LocationType.Supply || l.Type == LocationType.Major) sp += _stBonus;
                if (l.Type == LocationType.Command || l.Type == LocationType.Major) cp += _stBonus;
            }
            return new IncomeAmount(sp, cp);
        }
    }
}
