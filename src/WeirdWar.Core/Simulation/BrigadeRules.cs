using System;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// Brigade size limits read from the parameter table once, when the game starts (DLV-014), so a missing or bad value
    /// fails at once and not in the middle of a command.
    ///   P-BRG-01: the most Sections a Brigade may hold, also through transfers (BRG-021).
    ///   P-BRG-06: how much a Card adds to one Brigade's maximum (BRG-022). The officer bonus is not built yet.
    ///   A Brigade's own maximum is <see cref="MaxSections"/> plus its <see cref="Brigade.SectionBonus"/>.
    /// </summary>
    public sealed class BrigadeRules
    {
        public BrigadeRules(ParameterTable parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            MaxSections = parameters.GetInt("P-BRG-01");
            if (MaxSections < 1) throw new ParameterFormatException("P-BRG-01 = " + MaxSections + " must be at least 1 (BRG-021).");
            BonusSectionCap = parameters.GetInt("P-BRG-06");
            if (BonusSectionCap < 1) throw new ParameterFormatException("P-BRG-06 = " + BonusSectionCap + " must be at least 1 (BRG-022).");
        }

        /// <summary>Most Sections in one Brigade (P-BRG-01, BRG-021).</summary>
        public int MaxSections { get; }

        /// <summary>Sections the Extra Section Card adds to one Brigade's maximum each time it is played (P-BRG-06, BRG-022).</summary>
        public int BonusSectionCap { get; }
    }
}
