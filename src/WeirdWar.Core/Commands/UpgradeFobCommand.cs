using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Maps;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Upgrades an owned Supply ST to an FOB (MAP-012, MAP-046). It takes effect at once, needs no Brigade on the hex, and
    /// costs the standard SP from the reserve plus CP that rises with each upgrade the Faction has made (MAP-025, P-MAP-03).
    /// There is no cap on how many (MAP-007, MAP-013). The owner is unchanged. A capture pending on the location is cleared,
    /// so the FOB does not change hands (MAP-010; an assumption, no rule says it).
    /// </summary>
    public sealed class UpgradeFobCommand : ICommand, IEquatable<UpgradeFobCommand>
    {
        public const string TypeName = "upgradeFob";

        public UpgradeFobCommand(string? faction, string locationId)
        {
            Issuer = faction ?? "";
            LocationId = locationId ?? "";
        }

        public string Type => TypeName;
        public string Issuer { get; }
        public TurnPhase? RequiredPhase => TurnPhase.Actions;
        public string LocationId { get; }

        public string? Validate(GameState state)
        {
            if (state.Map == null || state.FobRules == null) return "There is no campaign to upgrade in";
            var loc = state.Locations.FirstOrDefault(l => l.Id == LocationId);
            if (loc == null) return "There is no location " + LocationId;
            if (loc.Type != LocationType.Supply) return "Only a Supply ST can be upgraded to an FOB (MAP-046); " + LocationId + " is " + loc.Type;
            if (loc.Owner != Issuer) return LocationId + " is not owned by " + Issuer;
            var faction = state.Factions.Single(f => f.Id == Issuer);
            var sp = state.FobRules.SpCost;
            var cp = state.FobRules.CpCost(faction.FobUpgrades);
            if (faction.Reserve < sp) return "The upgrade costs " + sp + " SP and the reserve has " + faction.Reserve;
            if (faction.Cp < cp) return "The upgrade costs " + cp + " CP and " + Issuer + " has " + faction.Cp;
            return null;
        }

        public void Apply(GameState state)
        {
            var loc = state.Locations.Single(l => l.Id == LocationId);
            var faction = state.Factions.Single(f => f.Id == Issuer);
            faction.Reserve -= state.FobRules!.SpCost;
            faction.Cp -= state.FobRules.CpCost(faction.FobUpgrades);
            faction.FobUpgrades++;
            loc.Upgraded = true;
            loc.Capturing = null;
            loc.CapturingBrigade = null;
        }

        public Dictionary<string, object?> ToJson() => CommandRegistry.WithIssuer(
            new Dictionary<string, object?> { ["location"] = LocationId }, Issuer);

        public static UpgradeFobCommand FromJson(IReadOnlyDictionary<string, object?> json)
        {
            if (!json.TryGetValue("location", out var v) || !(v is string s)) throw new JsonException("Field 'location' must be a string");
            return new UpgradeFobCommand(CommandRegistry.OptionalIssuer(json), s);
        }

        public bool Equals(UpgradeFobCommand? other) => other != null && other.Issuer == Issuer && other.LocationId == LocationId;
        public override bool Equals(object? obj) => Equals(obj as UpgradeFobCommand);
        public override int GetHashCode() => HashCode.Combine(Issuer, LocationId);
    }
}
