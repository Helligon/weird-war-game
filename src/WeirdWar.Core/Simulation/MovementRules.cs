using System;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// Movement values read from the parameter table once, when the game starts (DLV-014), so a missing or bad value
    /// fails at once and not in the middle of a command.
    ///   P-SUP-10: the most hexes a Brigade with a Slow Section may move in one turn, in total (TAG-010, SUP-024, SUP-025).
    /// </summary>
    public sealed class MovementRules
    {
        public MovementRules(ParameterTable parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            SlowCap = parameters.GetInt("P-SUP-10");
            if (SlowCap < 1) throw new ParameterFormatException("P-SUP-10 = " + SlowCap + " must be at least 1 (SUP-024).");
        }

        /// <summary>Hexes per turn for a Brigade with a Slow Section (P-SUP-10).</summary>
        public int SlowCap { get; }
    }
}
