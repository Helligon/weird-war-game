namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// Scenario data for a new game (DEC-087 stand-ins). Plain data; see maps/README.md for the file format.
    /// Token positions are map (column, row) cells. Validity is checked by <see cref="GameState.NewGame"/>.
    /// </summary>
    public sealed class GameConfig
    {
        /// <summary>Stand-in for the random first player (TRN-033).</summary>
        public PlayerId FirstPlayer { get; }

        /// <summary>Stand-in for SP: each token's movement budget at the start of its owner's turn.</summary>
        public int StartingBudget { get; }

        public OffsetCoord Player1Start { get; }
        public OffsetCoord Player2Start { get; }

        /// <summary>
        /// Budget spent per hex moved. P-SUP-09 (per-hex SP cost) is open, so this is data, not a constant.
        /// Same for every passable hex. Required: no value is built into the code.
        /// </summary>
        public int MovementCostPerHex { get; }

        public GameConfig(PlayerId firstPlayer, int startingBudget, OffsetCoord player1Start, OffsetCoord player2Start,
            int movementCostPerHex)
        {
            MovementCostPerHex = movementCostPerHex;
            FirstPlayer = firstPlayer;
            StartingBudget = startingBudget;
            Player1Start = player1Start;
            Player2Start = player2Start;
        }

        public OffsetCoord StartOf(PlayerId player) => player == PlayerId.Player1 ? Player1Start : Player2Start;
    }
}
