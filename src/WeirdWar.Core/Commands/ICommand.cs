using System.Collections.Generic;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// A player action (DLV-013). The only way to change <see cref="GameState"/> is to apply a command
    /// through <see cref="GameState.ApplyCommand"/>. Commands are plain data so they can be saved or
    /// sent over a network (TEC-008): they serialise to JSON and read back equal.
    /// Implementations MUST override Equals and GetHashCode by value.
    /// </summary>
    public interface ICommand
    {
        /// <summary>Stable name used in the serialised form. Never rename once saved data exists.</summary>
        string Type { get; }

        /// <summary>
        /// Id of the Faction that issues the command (TRN-006). "" means no Faction: only a state without a campaign
        /// (no map, so no turns) accepts such a command.
        /// </summary>
        string Issuer { get; }

        /// <summary>
        /// The turn phase the command may be sent in (TRN-005), or null for any phase. Action commands (TRN-002)
        /// return <see cref="TurnPhase.Actions"/>. <see cref="GameState.ApplyCommand"/> checks this and the issuer
        /// before <see cref="Validate"/>, so a command cannot forget to.
        /// </summary>
        TurnPhase? RequiredPhase { get; }

        /// <summary>Returns null if the command is legal in this state, otherwise the reason it is not. MUST NOT change the state.</summary>
        string? Validate(GameState state);

        /// <summary>
        /// Changes the state. Only called after <see cref="Validate"/> returned null. MUST NOT throw once
        /// Validate has passed: a throw part-way would leave the state half-changed and the command unlogged.
        /// Do every check in Validate.
        /// </summary>
        void Apply(GameState state);

        /// <summary>The command's fields as JSON values (see Json). The "type" key is added by the serialiser.</summary>
        Dictionary<string, object?> ToJson();
    }
}
