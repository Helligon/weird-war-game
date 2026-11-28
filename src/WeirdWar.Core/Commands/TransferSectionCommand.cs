using System;
using System.Collections.Generic;
using System.Linq;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Moves one Section from one of the Faction's Brigades to another of its Brigades on an adjacent hex (BRG-016,
    /// BRG-017). It costs no SP, and the Section takes none with it (BRG-017, BRG-018): the reserve and both Brigades'
    /// SP are untouched, so a Brigade that now has less capacity keeps its overflow (BRG-019). The Section keeps its health.
    /// The source may drop below 3 Sections (BRG-020), and is disbanded when it has none left (BRG-023). The target may
    /// not go above P-BRG-01 Sections plus its own Card bonus (BRG-021, BRG-022). Officers stay with their Brigade (OFF-015); none exist in the state yet.
    /// Both Brigades must belong to the issuing Faction (an assumption: moving a Section to an enemy Brigade is not described).
    /// </summary>
    public sealed class TransferSectionCommand : ICommand, IEquatable<TransferSectionCommand>
    {
        public const string TypeName = "transferSection";

        public TransferSectionCommand(string? faction, int sourceBrigadeId, int sectionIndex, int targetBrigadeId)
        {
            Issuer = faction ?? "";
            SourceBrigadeId = sourceBrigadeId;
            SectionIndex = sectionIndex;
            TargetBrigadeId = targetBrigadeId;
        }

        public string Type => TypeName;
        public string Issuer { get; }
        public TurnPhase? RequiredPhase => TurnPhase.Actions;
        public int SourceBrigadeId { get; }

        /// <summary>Position of the Section in the source Brigade's list, from 0.</summary>
        public int SectionIndex { get; }
        public int TargetBrigadeId { get; }

        public string? Validate(GameState state)
        {
            if (state.Map == null || state.BrigadeRules == null)
                return "There is no campaign to transfer Sections in";
            var source = state.Brigades.FirstOrDefault(b => b.Id == SourceBrigadeId);
            if (source == null) return "There is no Brigade " + SourceBrigadeId;
            var target = state.Brigades.FirstOrDefault(b => b.Id == TargetBrigadeId);
            if (target == null) return "There is no Brigade " + TargetBrigadeId;
            if (source.Faction != Issuer) return "Brigade " + SourceBrigadeId + " belongs to " + source.Faction + ", not " + Issuer;
            if (target.Faction != Issuer) return "Brigade " + TargetBrigadeId + " belongs to " + target.Faction + ", not " + Issuer;
            if (source == target) return "A Section cannot be moved to the Brigade it is already in";
            if (SectionIndex < 0 || SectionIndex >= source.Sections.Count)
                return "Brigade " + SourceBrigadeId + " has no Section " + SectionIndex;
            if (source.Position.DistanceTo(target.Position) != 1)
                return "Brigades " + SourceBrigadeId + " and " + TargetBrigadeId + " are not on adjacent hexes (BRG-017)";
            var max = state.BrigadeRules.MaxSections + target.SectionBonus;
            if (target.Sections.Count >= max)
                return "Brigade " + TargetBrigadeId + " already has " + max + " Sections, the most allowed (BRG-021)";
            return null;
        }

        public void Apply(GameState state)
        {
            var source = state.Brigades.Single(b => b.Id == SourceBrigadeId);
            var target = state.Brigades.Single(b => b.Id == TargetBrigadeId);
            var section = source.Sections[SectionIndex];
            target.AddSection(section);
            state.RemoveSectionFromBrigade(source, SectionIndex);
        }

        public Dictionary<string, object?> ToJson() => CommandRegistry.WithIssuer(
            new Dictionary<string, object?>
            {
                ["source"] = (long)SourceBrigadeId,
                ["index"] = (long)SectionIndex,
                ["target"] = (long)TargetBrigadeId,
            }, Issuer);

        public static TransferSectionCommand FromJson(IReadOnlyDictionary<string, object?> json)
            => new TransferSectionCommand(CommandRegistry.OptionalIssuer(json), Int(json, "source"), Int(json, "index"), Int(json, "target"));

        private static int Int(IReadOnlyDictionary<string, object?> json, string key)
        {
            var v = CommandRegistry.RequireLong(json, key);
            if (v < int.MinValue || v > int.MaxValue) throw new JsonException("Field '" + key + "' out of range");
            return (int)v;
        }

        public bool Equals(TransferSectionCommand? other) => other != null && other.Issuer == Issuer
            && other.SourceBrigadeId == SourceBrigadeId && other.SectionIndex == SectionIndex && other.TargetBrigadeId == TargetBrigadeId;
        public override bool Equals(object? obj) => Equals(obj as TransferSectionCommand);
        public override int GetHashCode() => HashCode.Combine(Issuer, SourceBrigadeId, SectionIndex, TargetBrigadeId);
    }
}
