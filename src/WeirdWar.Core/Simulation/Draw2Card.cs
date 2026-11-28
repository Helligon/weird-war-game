namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// The "Draw 2" Card (CRD-007): no target and no play condition. It draws 2 Cards with the same code as the Draw step, so the
    /// discard pile is reshuffled when the deck runs out (CRD-015). With fewer than 2 Cards left in deck and discard pile it draws
    /// what there is (same assumption as the Draw step). The hand may go above 8; discarding down is CRD-006.
    /// </summary>
    internal sealed class Draw2Card : ICardEffect
    {
        public const string CardId = "draw2";
        public const int Count = 2;

        public string? CanPlay(GameState state, FactionState player, string target) => null;

        public void Apply(GameState state, FactionState player, string target) => TurnPhases.DrawCards(state, player, Count);
    }
}
