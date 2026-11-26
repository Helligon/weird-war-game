namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// Hands the player's Officer to the AI, or hands it back (CMD-004, DAI-005). It is a battle-control command, not an Officer
    /// ability, so it is not an <see cref="OfficerCommand"/> (PCH-009 lists the third-person actions: move, aim and fire; PCH-008 allows no active abilities). The state it changes
    /// lives in the core and the command is logged with its step, so <see cref="BattleState.Replay"/> reproduces a battle in which
    /// the Officer was AI-controlled for some steps (TEC-008). Plain data, no Unity types (DLV-013).
    /// </summary>
    public sealed class OfficerControlCommand : IBattleCommand
    {
        /// <summary>Index of the Officer in <see cref="BattleState.Officers"/>.</summary>
        public int Officer { get; }
        /// <summary>True: the AI controls the Officer (it Protects the nearest friendly Section). False: the player does.</summary>
        public bool UnderAi { get; }

        public OfficerControlCommand(int officer, bool underAi) { Officer = officer; UnderAi = underAi; }

        public override bool Equals(object? obj) => obj is OfficerControlCommand o && o.Officer == Officer && o.UnderAi == UnderAi;
        public override int GetHashCode() => unchecked(Officer * 31 + (UnderAi ? 1 : 0));
    }
}
