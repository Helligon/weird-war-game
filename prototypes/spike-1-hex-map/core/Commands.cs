namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// A player action (DLV-013). Commands are plain data: the issuing player plus parameters, with no reference
    /// to the game state or to Unity (TEC-008). Add new commands as subclasses holding only plain fields, then
    /// handle them in <see cref="GameState.Apply"/>.
    /// </summary>
    public abstract class GameCommand
    {
        public PlayerId Player { get; }

        protected GameCommand(PlayerId player)
        {
            Player = player;
        }
    }

    /// <summary>Ends the issuing player's turn (TRN-007). No parameters.</summary>
    public sealed class EndTurnCommand : GameCommand
    {
        public EndTurnCommand(PlayerId player) : base(player) { }
    }
}
