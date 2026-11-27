using System;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// Brigade size limits read from the parameter table once, when the game starts (DLV-014), so a missing or bad value
    /// fails at once and not in the middle of a command.
    ///   P-BRG-01: the most Sections a Brigade may hold, also through transfers (BRG-021). The Card or officer bonus
    ///   (P-BRG-06) is not built yet, so the maximum is exactly this.
    /// </summary>
    public sealed class BrigadeRules
    {
        public BrigadeRules(ParameterTable parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            MaxSections = parameters.GetInt("P-BRG-01");
            if (MaxSections < 1) throw new ParameterFormatException("P-BRG-01 = " + MaxSections + " must be at least 1 (BRG-021).");
        }

        /// <summary>Most Sections in one Brigade (P-BRG-01, BRG-021).</summary>
        public int MaxSections { get; }
    }
}
