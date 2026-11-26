using System;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// The phases of one Faction's turn that CC1 builds (TRN-005). The Draw step, timers, upkeep and the undead rot
    /// check are not built yet; their Stories add a phase or a step in the hook below.
    /// </summary>
    public enum TurnPhase
    {
        StartOfTurn = 0,
        Income = 1,
        Actions = 2,
        EndOfTurn = 3,
    }

    /// <summary>
    /// The phase hooks (TRN-005). Each runs once per Faction turn, in order, inside the commands that move the turn on
    /// (see <see cref="GameState"/> and EndTurnCommand). They take the Faction whose turn it is and change the state
    /// directly: they run only inside a command's Apply, so DLV-013 holds.
    /// A later Story adds its rule by adding a call inside the matching method, and nothing else:
    ///   StartOfTurn: resolve captures (TRN-009, CC1-75).
    ///   Income: SP to the reserve (CC1-70). Resupply (CC1-74) is a player action, so it is a command in Actions.
    ///   EndOfTurn: may record capture candidates (TRN-009: ending your own turn on the ST, CC1-75); upkeep and rot are later Features.
    /// </summary>
    internal static class TurnPhases
    {
        internal static void StartOfTurn(GameState state, string faction)
        {
            // Hook: captures (CC1-75).
        }

        internal static void Income(GameState state, string faction)
        {
            // Hook: income (CC1-70).
        }

        internal static void EndOfTurn(GameState state, string faction)
        {
            // Hook: none in CC1.
        }

        /// <summary>Runs the start-of-turn and income phases for <paramref name="faction"/>, then rests in Actions.</summary>
        internal static void BeginTurn(GameState state, string faction)
        {
            Enter(state, TurnPhase.StartOfTurn, faction);
            StartOfTurn(state, faction);
            Enter(state, TurnPhase.Income, faction);
            Income(state, faction);
            Enter(state, TurnPhase.Actions, faction);
        }

        /// <summary>Runs the end-of-turn phase for <paramref name="faction"/>.</summary>
        internal static void FinishTurn(GameState state, string faction)
        {
            Enter(state, TurnPhase.EndOfTurn, faction);
            EndOfTurn(state, faction);
        }

        private static void Enter(GameState state, TurnPhase phase, string faction)
        {
            state.Phase = phase;
            state.PhaseObserver?.Invoke(phase, faction);
        }
    }
}
