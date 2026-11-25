namespace WeirdWar.Spike1.Core
{
    /// <summary>A Brigade token (spike stand-in): one per player. Immutable; a changed token is a new value.</summary>
    public sealed class Token
    {
        public PlayerId Owner { get; }
        public HexCoord Position { get; }

        /// <summary>Movement left this turn (stand-in for SP, DEC-087).</summary>
        public int Budget { get; }

        public Token(PlayerId owner, HexCoord position, int budget)
        {
            Owner = owner;
            Position = position;
            Budget = budget;
        }

        internal Token WithBudget(int budget) => new Token(Owner, Position, budget);
        internal Token WithPosition(HexCoord position, int budget) => new Token(Owner, position, budget);
    }
}
