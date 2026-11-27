namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// A battle that has started and is waiting to be resolved (TRN-003, TRN-008, TRN-036, DEC-103). It names the attacker
    /// and the defender, both still on their own hexes, and carries no outcome: nothing resolves a battle yet. It is part of
    /// the state hash (TEC-008).
    /// </summary>
    public sealed class PendingBattle
    {
        public PendingBattle(int attackerId, int defenderId) { AttackerId = attackerId; DefenderId = defenderId; }

        public int AttackerId { get; }
        public int DefenderId { get; }
    }
}
