using System;
using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// A command given to exactly one Section (CMD-016, HIE-007). Plain data with no state and no Unity
    /// reference (DLV-013, TEC-008). No command takes a list of Sections.
    /// </summary>
    public abstract class SectionCommand : IBattleCommand
    {
        public SectionId Section { get; }

        protected SectionCommand(SectionId section) { Section = section; }

        public override bool Equals(object? obj) => obj != null && obj.GetType() == GetType() && ((SectionCommand)obj).Section.Equals(Section);
        public override int GetHashCode() => Section.GetHashCode();
    }

    /// <summary>Move (CMD-005): 1 or more waypoints, appended to any queued ones (CMD-020). The cap is P-CMD-01.</summary>
    public sealed class MoveCommand : SectionCommand
    {
        public IReadOnlyList<Waypoint> Waypoints { get; }

        public MoveCommand(SectionId section, IEnumerable<Waypoint> waypoints) : base(section)
        {
            Waypoints = new List<Waypoint>(waypoints ?? throw new ArgumentNullException(nameof(waypoints))).AsReadOnly();
        }

        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj)) return false;
            var other = (MoveCommand)obj!;
            if (other.Waypoints.Count != Waypoints.Count) return false;
            for (var i = 0; i < Waypoints.Count; i++)
            {
                if (!Waypoints[i].Equals(other.Waypoints[i])) return false;
            }
            return true;
        }

        public override int GetHashCode() => unchecked(base.GetHashCode() * 31 + Waypoints.Count);
    }

    /// <summary>Stop (CMD-006): carry out queued waypoints, then hold the final position.</summary>
    public sealed class StopCommand : SectionCommand
    {
        public StopCommand(SectionId section) : base(section) { }
    }

    /// <summary>Clear waypoints (CMD-007): the only way to cancel queued movement.</summary>
    public sealed class ClearWaypointsCommand : SectionCommand
    {
        public ClearWaypointsCommand(SectionId section) : base(section) { }
    }

    /// <summary>Target (CMD-008): the Section attacks an enemy Section.</summary>
    public sealed class TargetCommand : SectionCommand
    {
        public SectionId TargetSection { get; }

        public TargetCommand(SectionId section, SectionId targetSection) : base(section) { TargetSection = targetSection; }

        public override bool Equals(object? obj) => base.Equals(obj) && ((TargetCommand)obj!).TargetSection.Equals(TargetSection);
        public override int GetHashCode() => unchecked(base.GetHashCode() * 31 + TargetSection.GetHashCode());
    }

    /// <summary>Fall back (CMD-011): clear waypoints, move to own map edge and hold.</summary>
    public sealed class FallBackCommand : SectionCommand
    {
        public FallBackCommand(SectionId section) : base(section) { }
    }
}
