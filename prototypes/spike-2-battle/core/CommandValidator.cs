using System;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>Outcome of validating a command. A rejection carries a reason.</summary>
    public sealed class ValidationResult
    {
        public bool IsAccepted { get; }
        public string? Reason { get; }

        private ValidationResult(bool accepted, string? reason) { IsAccepted = accepted; Reason = reason; }

        public static readonly ValidationResult Accepted = new ValidationResult(true, null);

        public static ValidationResult Rejected(string reason)
        {
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("A rejection needs a reason.", nameof(reason));
            return new ValidationResult(false, reason);
        }
    }

    /// <summary>Checks a command against the battle and the parameters, without changing anything (DLV-013).</summary>
    public static class CommandValidator
    {
        public static ValidationResult Validate(IBattleCommand command, IBattleView view, BattleParameters parameters)
        {
            if (!(command is SectionCommand c))
            {
                return ValidationResult.Rejected("Unknown command type.");
            }
            if (!view.TryGetSection(c.Section, out var section))
            {
                return ValidationResult.Rejected(c.Section + " does not exist.");
            }
            switch (c)
            {
                case MoveCommand m:
                    if (m.Waypoints.Count < 1)
                    {
                        return ValidationResult.Rejected("A Move needs at least 1 waypoint (CMD-005).");
                    }
                    if (section.QueuedWaypoints + m.Waypoints.Count > parameters.MaxWaypoints)
                    {
                        return ValidationResult.Rejected("Queued plus new waypoints would exceed P-CMD-01 ("
                            + parameters.MaxWaypoints + ") (CMD-020).");
                    }
                    return ValidationResult.Accepted;
                case TargetCommand t:
                    if (!view.TryGetSection(t.TargetSection, out var target))
                    {
                        return ValidationResult.Rejected(t.TargetSection + " does not exist.");
                    }
                    if (target.Side == section.Side)
                    {
                        return ValidationResult.Rejected("A Target must be an enemy Section (CMD-008).");
                    }
                    return ValidationResult.Accepted;
                default:
                    return ValidationResult.Accepted;
            }
        }
    }
}
