namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>VIC-001: how the battle stands. There is no time-based ending (VIC-004).</summary>
    public enum BattleOutcome
    {
        InProgress = 0,
        PlayerWon = 1,
        EnemyWon = 2,
        /// <summary>Spike stand-in: both sides were destroyed at the same moment.</summary>
        Draw = 3,
    }
}
