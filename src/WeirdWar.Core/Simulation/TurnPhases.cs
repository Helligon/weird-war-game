using System;
using System.Linq;
using WeirdWar.Core.Maps;

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
            // TRN-009, TRN-005: a capture started at the end of this Faction's last turn completes if its Brigade is still
            // on the ST. It resolves before income (TRN-005), so the new ST pays this turn. Marks are always cleared here.
            foreach (var l in state.Locations)
            {
                if (l.Capturing != faction) continue;
                l.Capturing = null;
                if (state.Brigades.Any(b => b.Faction == faction && b.Position.Equals(l.Position))) l.Owner = faction;
            }
        }

        internal static void Income(GameState state, string faction)
        {
            // TRN-001, TRN-005: only the active Faction is paid. SP to the reserve, CP to the player.
            var income = state.IncomeRules!.Calculate(state, faction);
            var f = state.Factions.Single(x => x.Id == faction);
            f.Reserve += income.Sp;
            f.Cp += income.Cp;
        }

        internal static void EndOfTurn(GameState state, string faction)
        {
            // TRN-009: a Brigade that ends its owner's turn on an ST it does not own starts a capture. A capture started
            // this turn is not resolved this turn: it waits for the start of the Faction's next turn. HQs are not STs.
            foreach (var l in state.Locations)
            {
                if (l.Type == LocationType.Hq || l.Owner == faction) continue;
                if (state.Brigades.Any(b => b.Faction == faction && b.Position.Equals(l.Position))) l.Capturing = faction;
            }
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
