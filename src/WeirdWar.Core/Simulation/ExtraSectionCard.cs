using System.Globalization;
using System.Linq;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// The "Extra Section" Card (BRG-022): raises one of the player's own Brigades' maximum by P-BRG-06 Sections. The target is the
    /// Brigade's id as text. Plain readings, for Alex to check: the target must be the player's own Brigade (the rule names no owner),
    /// and a Brigade takes the Card bonus only once (BRG-022: "by 1", P-BRG-06 is the cap).
    /// </summary>
    internal sealed class ExtraSectionCard : ICardEffect
    {
        public const string CardId = "extraSection";

        private static Brigade? Find(GameState state, string target)
            => int.TryParse(target, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? state.Brigades.FirstOrDefault(b => b.Id == id) : null;

        public string? CanPlay(GameState state, FactionState player, string target)
        {
            if (state.BrigadeRules == null) return "There is no campaign to play Extra Section in";
            var brigade = Find(state, target);
            if (brigade == null) return "Extra Section needs the id of one of your Brigades as its target";
            if (brigade.Faction != player.Id) return "Brigade " + brigade.Id + " belongs to " + brigade.Faction + ", not " + player.Id;
            if (brigade.SectionBonus >= state.BrigadeRules.BonusSectionCap)
                return "Brigade " + brigade.Id + " already has the Card bonus of " + state.BrigadeRules.BonusSectionCap + " Section(s), the most a Card gives (BRG-022)";
            return null;
        }

        public void Apply(GameState state, FactionState player, string target)
            => Find(state, target)!.SectionBonus += state.BrigadeRules!.BonusSectionCap;
    }
}
