using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Moves SP from the Faction's reserve to one of its Brigades (RES-003, SUP-012 reserve part). Accepted only when
    /// the Brigade stands within P-SUP-08 hexes (same or adjacent hex) of the Faction's own HQ or an owned Supply or Major ST
    /// (SUP-013, SUP-017, MAP-015), unless an enemy Brigade stands on that location (SUP-027). It costs nothing else: no movement, no CP, and no limit on how often it is
    /// sent (SUP-018). The Brigade never goes above its capacity through resupply (SUP-021, BRG-019): when the request
    /// is more than the room or the reserve, the smallest of the three is moved (SUP-028). A Brigade with no room
    /// or an empty reserve is rejected, so no no-op reaches the log.
    /// </summary>
    public sealed class DistributeSpCommand : ICommand, IEquatable<DistributeSpCommand>
    {
        public const string TypeName = "distributeSp";

        public DistributeSpCommand(string? faction, int brigadeId, int amount)
        {
            Issuer = faction ?? "";
            BrigadeId = brigadeId;
            Amount = amount;
        }

        public string Type => TypeName;
        public string Issuer { get; }
        public TurnPhase? RequiredPhase => TurnPhase.Actions;
        public int BrigadeId { get; }

        /// <summary>SP asked for. The amount moved is this, or less when the Brigade has less room or the reserve is smaller (SUP-028).</summary>
        public int Amount { get; }

        public string? Validate(GameState state)
        {
            if (state.Map == null || state.Supply == null)
                return "There is no campaign to resupply in";
            if (Amount <= 0) return "The amount to distribute must be at least 1";
            var brigade = state.Brigades.FirstOrDefault(b => b.Id == BrigadeId);
            if (brigade == null) return "There is no Brigade " + BrigadeId;
            if (brigade.Faction != Issuer) return "Brigade " + BrigadeId + " belongs to " + brigade.Faction + ", not " + Issuer;
            var faction = state.Factions.Single(f => f.Id == Issuer);
            var range = state.Supply.ResupplyRange;
            var near = state.Locations.Any(l => l.Owner == Issuer
                && Supply.ResupplyLocationTypes.Contains(l.Type)
                && l.Position.DistanceTo(brigade.Position) <= range
                && !state.Brigades.Any(e => e.Faction != Issuer && e.Position == l.Position));
            if (!near) return "Brigade " + BrigadeId + " is not within " + range + " hex of an owned, unblocked resupply point";
            if (Room(state, brigade) <= 0) return "Brigade " + BrigadeId + " is at or above its capacity";
            if (faction.Reserve <= 0) return "The reserve is empty";
            return null;
        }

        public void Apply(GameState state)
        {
            var brigade = state.Brigades.Single(b => b.Id == BrigadeId);
            var faction = state.Factions.Single(f => f.Id == Issuer);
            var moved = Math.Min(Amount, Math.Min(Room(state, brigade), faction.Reserve));
            faction.Reserve -= moved;
            brigade.SP += moved;
        }

        private static int Room(GameState state, Brigade brigade) => state.Supply!.CapacityOf(brigade) - brigade.SP;

        public Dictionary<string, object?> ToJson() => CommandRegistry.WithIssuer(
            new Dictionary<string, object?> { ["brigade"] = (long)BrigadeId, ["amount"] = (long)Amount }, Issuer);

        public static DistributeSpCommand FromJson(IReadOnlyDictionary<string, object?> json)
        {
            var brigade = CommandRegistry.RequireLong(json, "brigade");
            var amount = CommandRegistry.RequireLong(json, "amount");
            if (brigade < int.MinValue || brigade > int.MaxValue) throw new JsonException("Field 'brigade' out of range");
            if (amount < int.MinValue || amount > int.MaxValue) throw new JsonException("Field 'amount' out of range");
            return new DistributeSpCommand(CommandRegistry.OptionalIssuer(json), (int)brigade, (int)amount);
        }

        public bool Equals(DistributeSpCommand? other) => other != null && other.Issuer == Issuer && other.BrigadeId == BrigadeId && other.Amount == Amount;
        public override bool Equals(object? obj) => Equals(obj as DistributeSpCommand);
        public override int GetHashCode() => HashCode.Combine(Issuer, BrigadeId, Amount);
    }
}
