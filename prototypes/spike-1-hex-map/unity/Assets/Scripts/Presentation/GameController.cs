using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>
    /// Holds the shown <see cref="GameState"/> and turns player input into commands (DLV-013, DLV-012).
    /// Plain C#, no Unity types, so it is tested without clicking. It never edits positions, budgets or the
    /// current player: the only way the state changes is by replacing it with the result of
    /// <see cref="GameState.Apply"/>. All rules (adjacency, cost, contact, turn order) stay in the core.
    /// </summary>
    public sealed class GameController
    {
        public GameState State { get; private set; }

        /// <summary>Owner of the selected token, or null when nothing is selected.</summary>
        public PlayerId? Selected { get; private set; }

        /// <summary>Reason the core gave for the last rejected command; null after an accepted one (and at the start).</summary>
        public string LastRejection { get; private set; }

        public GameController(GameState state)
        {
            State = state;
        }

        public PlayerId CurrentPlayer => State.CurrentPlayer;

        /// <summary>Movement budget left on the selected token; null when nothing is selected.</summary>
        public int? SelectedBudget => Selected.HasValue ? State.TokenOf(Selected.Value).Budget : (int?)null;

        /// <summary>
        /// A click on a hex (TRN-007, SUP-005). The current player's own token: select it. Otherwise, with a token
        /// selected: apply a MoveCommand to that hex and show the core's answer. The other player's token is never selected.
        /// </summary>
        public void ClickHex(HexCoord hex)
        {
            var own = State.TokenOf(State.CurrentPlayer);
            if (own.Position == hex)
            {
                Selected = State.CurrentPlayer;
                LastRejection = null;
                return;
            }
            if (Selected.HasValue)
            {
                Run(new MoveCommand(State.CurrentPlayer, Selected.Value, hex));
            }
        }

        /// <summary>The End Turn button (TRN-007): apply an EndTurnCommand. Selection is dropped on acceptance.</summary>
        public void EndTurn()
        {
            if (Run(new EndTurnCommand(State.CurrentPlayer)))
            {
                Selected = null;
            }
        }

        private bool Run(GameCommand command)
        {
            var result = State.Apply(command);
            State = result.State;
            LastRejection = result.IsAccepted ? null : result.RejectionReason;
            return result.IsAccepted;
        }
    }
}
