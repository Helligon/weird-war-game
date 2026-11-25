using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// Canonical text form of a <see cref="GameState"/> and its SHA-256 hash (TEC-005, TEC-008). The field order
    /// is fixed in code, integers use invariant formatting, and nothing depends on Dictionary/HashSet order or
    /// on <c>GetHashCode</c> (which differs between processes). The same state gives the same text and hash on
    /// every platform. Format (compact JSON, no spaces):
    /// <c>{"currentPlayer":1,"config":{"firstPlayer":1,"startingBudget":10,"movementCostPerHex":1,"player1Start":[col,row],"player2Start":[col,row]},
    /// "map":{"width":W,"height":H,"cells":"0123..."},"tokens":[{"owner":1,"q":Q,"r":R,"budget":B},{"owner":2,...}]}</c>.
    /// Map cells are one digit per hex (the <see cref="TerrainType"/> value), row by row, column by column.
    /// </summary>
    public static class CanonicalState
    {
        public static string Serialise(GameState state)
        {
            var sb = new StringBuilder();
            sb.Append("{\"currentPlayer\":").Append(Num((int)state.CurrentPlayer));
            var c = state.Config;
            sb.Append(",\"config\":{\"firstPlayer\":").Append(Num((int)c.FirstPlayer));
            sb.Append(",\"startingBudget\":").Append(Num(c.StartingBudget));
            sb.Append(",\"movementCostPerHex\":").Append(Num(c.MovementCostPerHex));
            sb.Append(",\"player1Start\":[").Append(Num(c.Player1Start.Column)).Append(',').Append(Num(c.Player1Start.Row)).Append(']');
            sb.Append(",\"player2Start\":[").Append(Num(c.Player2Start.Column)).Append(',').Append(Num(c.Player2Start.Row)).Append(']');
            sb.Append("},\"map\":{\"width\":").Append(Num(state.Map.Width));
            sb.Append(",\"height\":").Append(Num(state.Map.Height));
            sb.Append(",\"cells\":\"");
            for (var row = 0; row < state.Map.Height; row++)
            {
                for (var col = 0; col < state.Map.Width; col++)
                {
                    var terrain = state.Map.TerrainAt(HexCoord.FromOffset(new OffsetCoord(col, row)));
                    if ((int)terrain < 0 || (int)terrain > 9)
                    {
                        throw new System.InvalidOperationException("Terrain value " + (int)terrain + " does not fit the one-digit cell format.");
                    }
                    sb.Append(Num((int)terrain));
                }
            }
            sb.Append("\"},\"tokens\":[");
            for (var i = 0; i < state.Tokens.Count; i++)
            {
                var t = state.Tokens[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"owner\":").Append(Num((int)t.Owner));
                sb.Append(",\"q\":").Append(Num(t.Position.Q));
                sb.Append(",\"r\":").Append(Num(t.Position.R));
                sb.Append(",\"budget\":").Append(Num(t.Budget)).Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>SHA-256 of the UTF-8 canonical text, as 64 lower-case hex characters.</summary>
        public static string Hash(GameState state)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(Serialise(state)));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }

        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
