using System;
using System.Collections.Generic;

namespace WeirdWar.Spike1.Core
{
    /// <summary>
    /// The campaign game state (DLV-013). Immutable: the only way to get a changed state is
    /// <see cref="Apply"/>, which returns a new state or a rejection. Nothing is time-based (TRN-006), and there
    /// is no randomness or hidden global state (TEC-008). Tokens are held in a fixed order (Player 1, Player 2).
    /// </summary>
    public sealed class GameState
    {
        private readonly Token[] _tokens; // index 0 = Player 1, index 1 = Player 2. Never exposed or mutated.

        public HexMap Map { get; }
        public GameConfig Config { get; }
        public PlayerId CurrentPlayer { get; }

        public IReadOnlyList<Token> Tokens => Array.AsReadOnly(_tokens);

        public Token TokenOf(PlayerId player) => _tokens[Index(player)];

        private GameState(HexMap map, GameConfig config, PlayerId current, Token[] tokens)
        {
            Map = map;
            Config = config;
            CurrentPlayer = current;
            _tokens = tokens;
        }

        /// <summary>
        /// Creates the starting state (TRN-033 stand-in: the first player comes from the config). Throws
        /// <see cref="SetupException"/> if a token is off the map, on an impassable hex, on the other token's hex
        /// or next to it (no battle on contact, DEC-087).
        /// </summary>
        public static GameState NewGame(HexMap map, GameConfig config)
        {
            if (config.FirstPlayer != PlayerId.Player1 && config.FirstPlayer != PlayerId.Player2)
            {
                throw new SetupException("First player must be Player 1 or Player 2.");
            }
            if (config.StartingBudget < 1)
            {
                throw new SetupException("The starting budget must be at least 1.");
            }
            if (config.MovementCostPerHex < 1)
            {
                throw new SetupException("The movement cost per hex must be at least 1.");
            }
            var p1 = HexCoord.FromOffset(config.Player1Start);
            var p2 = HexCoord.FromOffset(config.Player2Start);
            CheckStart(map, PlayerId.Player1, config.Player1Start, p1);
            CheckStart(map, PlayerId.Player2, config.Player2Start, p2);
            if (p1.Equals(p2))
            {
                throw new SetupException("Both tokens are placed on the same hex.");
            }
            if (p1.DistanceTo(p2) == 1)
            {
                throw new SetupException("The two tokens are placed next to each other.");
            }
            var tokens = new[]
            {
                new Token(PlayerId.Player1, p1, config.StartingBudget),
                new Token(PlayerId.Player2, p2, config.StartingBudget),
            };
            return new GameState(map, config, config.FirstPlayer, tokens);
        }

        private static void CheckStart(HexMap map, PlayerId player, OffsetCoord cell, HexCoord hex)
        {
            var where = "(col " + cell.Column + ", row " + cell.Row + ")";
            if (!map.Contains(hex))
            {
                throw new SetupException(player + " token start " + where + " is outside the map.");
            }
            if (!map.IsPassable(hex))
            {
                throw new SetupException(player + " token start " + where + " is on impassable " + map.TerrainAt(hex) + ".");
            }
        }

        /// <summary>
        /// Applies a command. Returns the new state, or a rejection with a reason (the state is then unchanged).
        /// This instance is never modified.
        /// </summary>
        public CommandResult Apply(GameCommand command)
        {
            if (command == null)
            {
                return CommandResult.Reject(this, "No command given.");
            }
            if (command.Player != PlayerId.Player1 && command.Player != PlayerId.Player2)
            {
                return CommandResult.Reject(this, "Unknown player.");
            }
            if (command.Player != CurrentPlayer)
            {
                return CommandResult.Reject(this, "It is " + CurrentPlayer + "'s turn, not " + command.Player + "'s.");
            }
            switch (command)
            {
                case MoveCommand move:
                    return ApplyMove(move);
                case EndTurnCommand _:
                    return ApplyEndTurn();
                default:
                    return CommandResult.Reject(this, "Unknown command type " + command.GetType().Name + ".");
            }
        }

        // TRN-007: hand over to the other player. Stand-in: only the new current player's token gets its budget refilled.
        private CommandResult ApplyEndTurn()
        {
            var next = CurrentPlayer == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
            var tokens = (Token[])_tokens.Clone();
            tokens[Index(next)] = tokens[Index(next)].WithBudget(Config.StartingBudget);
            return CommandResult.Accept(new GameState(Map, Config, next, tokens));
        }

        // SUP-005, SUP-006, TRN-020, TRN-023, TRN-024, TRN-029: one hex per Move; the budget is spent straight away.
        private CommandResult ApplyMove(MoveCommand move)
        {
            if (move.TokenOwner != PlayerId.Player1 && move.TokenOwner != PlayerId.Player2)
            {
                return CommandResult.Reject(this, "Unknown token.");
            }
            if (move.TokenOwner != move.Player)
            {
                return CommandResult.Reject(this, "A player can only move their own token.");
            }
            var token = TokenOf(move.TokenOwner);
            var enemy = TokenOf(move.TokenOwner == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1);
            var target = move.Target;
            if (!Map.Contains(target))
            {
                return CommandResult.Reject(this, "Target is outside the map.");
            }
            if (!Map.IsPassable(target))
            {
                return CommandResult.Reject(this, "Target is impassable " + Map.TerrainAt(target) + ".");
            }
            if (target.Equals(enemy.Position))
            {
                return CommandResult.Reject(this, "Target is occupied by the other token.");
            }
            if (token.Position.DistanceTo(target) != 1)
            {
                return CommandResult.Reject(this, "Target is not next to the token.");
            }
            if (target.DistanceTo(enemy.Position) == 1)
            {
                return CommandResult.Reject(this, "contact: battles not in spike");
            }
            if (token.Budget < Config.MovementCostPerHex)
            {
                return CommandResult.Reject(this, "Not enough movement budget (" + token.Budget + " left, " + Config.MovementCostPerHex + " needed).");
            }
            return CommandResult.Accept(WithToken(token.WithPosition(target, token.Budget - Config.MovementCostPerHex)));
        }

        /// <summary>A new state with one token replaced.</summary>
        internal GameState WithToken(Token token)
        {
            var tokens = (Token[])_tokens.Clone();
            tokens[Index(token.Owner)] = token;
            return new GameState(Map, Config, CurrentPlayer, tokens);
        }

        private static int Index(PlayerId player)
        {
            switch (player)
            {
                case PlayerId.Player1: return 0;
                case PlayerId.Player2: return 1;
                default: throw new ArgumentOutOfRangeException(nameof(player), player, "Unknown player.");
            }
        }
    }
}
