using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Plays a Card from the issuer's hand (TRN-002, CRD-001, CRD-008). It costs the Card's CP (RES-006), needs the Card's play
    /// condition to hold (CRD-010), and has no limit on how many Cards or copies are played in a turn (CRD-009). A played Card goes to
    /// the discard pile, except a single-use Card, which goes to no pile (SUP-014, MAP-011). That a played Card is discarded is the
    /// plain reading: CRD-001 does not say. <see cref="Target"/> is what the Card's effect needs ("" for none).
    /// </summary>
    public sealed class PlayCardCommand : ICommand, IEquatable<PlayCardCommand>
    {
        public const string TypeName = "playCard";

        public PlayCardCommand(string? faction, string cardId, string? target = null)
        {
            Issuer = faction ?? "";
            CardId = cardId ?? "";
            Target = target ?? "";
        }

        public string Type => TypeName;
        public string Issuer { get; }
        public TurnPhase? RequiredPhase => TurnPhase.Actions;
        public string CardId { get; }
        public string Target { get; }

        public string? Validate(GameState state)
        {
            if (state.Map == null || state.Cards == null) return "There is no campaign with Cards to play in";
            var faction = state.Factions.Single(f => f.Id == Issuer);
            if (!faction._hand.Contains(CardId)) return Issuer + " has no " + CardId + " Card in hand";
            if (!state.Cards.TryGet(CardId, out var card)) return "There is no Card " + CardId;
            if (faction.Cp < card.CpCost) return card.Name + " costs " + card.CpCost + " CP and " + Issuer + " has " + faction.Cp;
            return card.Effect?.CanPlay(state, faction, Target);
        }

        public void Apply(GameState state)
        {
            var faction = state.Factions.Single(f => f.Id == Issuer);
            state.Cards!.TryGet(CardId, out var card);
            faction.Cp -= card.CpCost;
            faction._hand.Remove(CardId);
            if (!card.SingleUse) faction._discard.Add(CardId);
            card.Effect?.Apply(state, faction, Target);
        }

        public Dictionary<string, object?> ToJson()
        {
            var json = new Dictionary<string, object?> { ["card"] = CardId };
            if (Target.Length > 0) json["target"] = Target;
            return CommandRegistry.WithIssuer(json, Issuer);
        }

        public static PlayCardCommand FromJson(IReadOnlyDictionary<string, object?> json)
        {
            if (!json.TryGetValue("card", out var v) || !(v is string card)) throw new JsonException("Field 'card' must be a string");
            string? target = null;
            if (json.TryGetValue("target", out var t))
                target = t as string ?? throw new JsonException("Field 'target' must be a string");
            return new PlayCardCommand(CommandRegistry.OptionalIssuer(json), card, target);
        }

        public bool Equals(PlayCardCommand? other) => other != null && other.Issuer == Issuer && other.CardId == CardId && other.Target == Target;
        public override bool Equals(object? obj) => Equals(obj as PlayCardCommand);
        public override int GetHashCode() => HashCode.Combine(Issuer, CardId, Target);
    }
}
