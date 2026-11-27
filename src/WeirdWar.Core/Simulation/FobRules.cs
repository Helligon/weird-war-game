using System;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// The cost of upgrading a Supply ST to an FOB (MAP-012, MAP-025). Read at construction by parameter ID (DLV-014), so a
    /// missing value fails at game start. P-MAP-03 as "sp=5 cp=1 cpStep=1": the standard SP, the first upgrade's CP, and the CP
    /// added for each earlier upgrade. There is no cap on upgrades (MAP-007); the rising CP is the limit.
    /// </summary>
    public sealed class FobRules
    {
        private readonly int _sp, _cp, _cpStep;

        public FobRules(ParameterTable parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            var v = parameters.GetKeyedInts("P-MAP-03", "sp", "cp", "cpStep");
            foreach (var kv in v)
                if (kv.Value < 0) throw new ParameterFormatException("P-MAP-03: '" + kv.Key + "' must not be negative.");
            if (v["cpStep"] < 1) throw new ParameterFormatException("P-MAP-03: cpStep = " + v["cpStep"] + " must be at least 1, so the CP cost rises with each upgrade (MAP-012, MAP-025).");
            _sp = v["sp"]; _cp = v["cp"]; _cpStep = v["cpStep"];
        }

        /// <summary>SP cost of any upgrade.</summary>
        public int SpCost => _sp;

        /// <summary>CP cost of the next upgrade for a Faction that has made <paramref name="upgradesMade"/> already.</summary>
        public int CpCost(int upgradesMade) => _cp + _cpStep * upgradesMade;
    }
}
