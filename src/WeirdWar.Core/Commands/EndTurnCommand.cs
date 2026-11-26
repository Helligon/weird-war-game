using System;
using System.Collections.Generic;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Ends the active Faction's turn (TRN-007, TRN-005): runs its end-of-turn phase, hands the turn to the next
    /// Faction, and runs that Faction's start-of-turn and income phases, so it is ready to act.
    /// Only the active Faction may send it (TRN-006), and only in the Actions phase.
    /// </summary>
    public sealed class EndTurnCommand : ICommand, IEquatable<EndTurnCommand>
    {
        public const string TypeName = "endTurn";

        public EndTurnCommand(string? faction) { Issuer = faction ?? ""; }

        public string Type => TypeName;
        public string Issuer { get; }
        public TurnPhase? RequiredPhase => TurnPhase.Actions;

        public string? Validate(GameState state) => state.ActiveFaction == null ? "There is no campaign turn to end" : null;

        public void Apply(GameState state)
        {
            var next = state.NextFaction(Issuer);
            TurnPhases.FinishTurn(state, Issuer);
            state.ActiveFaction = next;
            state.TurnNumber++;
            TurnPhases.BeginTurn(state, next);
        }

        public Dictionary<string, object?> ToJson() => CommandRegistry.WithIssuer(new Dictionary<string, object?>(), Issuer);

        public static EndTurnCommand FromJson(IReadOnlyDictionary<string, object?> json) =>
            new EndTurnCommand(CommandRegistry.OptionalIssuer(json));

        public bool Equals(EndTurnCommand? other) => other != null && other.Issuer == Issuer;
        public override bool Equals(object? obj) => Equals(obj as EndTurnCommand);
        public override int GetHashCode() => Issuer.GetHashCode();
    }
}
