using System;
using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// A command for the player's third-person Officer (PCH-001, PCH-009, DLV-013). Plain data with no Unity types.
    /// It names an Officer (an index into <see cref="BattleState.Officers"/>) and never a Section or a Unit inside one
    /// (PCH-004). Only move, aim and fire exist (PCH-008).
    /// </summary>
    public abstract class OfficerCommand : IBattleCommand
    {
        public int Officer { get; }

        protected OfficerCommand(int officer) { Officer = officer; }

        public override bool Equals(object? obj) => obj != null && obj.GetType() == GetType() && ((OfficerCommand)obj).Officer == Officer;
        public override int GetHashCode() => Officer;
    }

    /// <summary>
    /// Move: walk in a map direction for a number of seconds. The direction is a vector in map metres (X east, Y north);
    /// its length is capped at 1 and scales the speed. The duration is data in the command, so the core never reads a clock (TEC-008).
    /// </summary>
    public sealed class OfficerMoveCommand : OfficerCommand
    {
        public double DirectionX { get; }
        public double DirectionY { get; }
        public double Seconds { get; }

        public OfficerMoveCommand(int officer, double directionX, double directionY, double seconds) : base(officer)
        {
            DirectionX = directionX; DirectionY = directionY; Seconds = seconds;
        }

        public override bool Equals(object? obj) => base.Equals(obj) && obj is OfficerMoveCommand o
            && o.DirectionX == DirectionX && o.DirectionY == DirectionY && o.Seconds == Seconds;
        public override int GetHashCode() => unchecked(base.GetHashCode() * 31 + DirectionX.GetHashCode() * 7 + DirectionY.GetHashCode() * 3 + Seconds.GetHashCode());
    }

    /// <summary>Aim: face a direction, in radians (0 is east, positive turns towards north).</summary>
    public sealed class OfficerAimCommand : OfficerCommand
    {
        public double Radians { get; }

        public OfficerAimCommand(int officer, double radians) : base(officer) { Radians = radians; }

        public override bool Equals(object? obj) => base.Equals(obj) && ((OfficerAimCommand)obj!).Radians == Radians;
        public override int GetHashCode() => unchecked(base.GetHashCode() * 31 + Radians.GetHashCode());
    }

    /// <summary>Fire the stand-in hitscan rifle along the Officer's current aim.</summary>
    public sealed class OfficerFireCommand : OfficerCommand
    {
        public OfficerFireCommand(int officer) : base(officer) { }
    }

    /// <summary>What one rifle shot did.</summary>
    public readonly struct OfficerShot
    {
        /// <summary>The enemy Section that was hit, or null on a miss.</summary>
        public SectionId? Target { get; }
        public int Damage { get; }
        /// <summary>Distance in metres from the Officer to the hit, or to the end of the range on a miss.</summary>
        public double Distance { get; }

        public OfficerShot(SectionId? target, int damage, double distance) { Target = target; Damage = damage; Distance = distance; }

        public bool Hit => Target.HasValue;
    }

    /// <summary>The result of applying an Officer command.</summary>
    public sealed class OfficerCommandResult
    {
        public bool IsAccepted { get; }
        public string? Reason { get; }
        /// <summary>Set only when a fire command was accepted.</summary>
        public OfficerShot? Shot { get; }

        private OfficerCommandResult(bool accepted, string? reason, OfficerShot? shot) { IsAccepted = accepted; Reason = reason; Shot = shot; }

        public static OfficerCommandResult Accepted(OfficerShot? shot = null) => new OfficerCommandResult(true, null, shot);
        public static OfficerCommandResult Rejected(string reason) => new OfficerCommandResult(false, reason, null);
    }

    /// <summary>
    /// Turns one frame of raw input numbers into Officer commands (PCH-009). It takes plain numbers, so it holds no Unity
    /// types and the same mapping is tested in the core. The only commands it can make are aim, move and fire (PCH-008).
    /// </summary>
    public static class OfficerInputMapper
    {
        /// <param name="officer">Index of the Officer.</param>
        /// <param name="currentAim">The Officer's current aim in radians.</param>
        /// <param name="forward">-1 to 1: forward (along the aim) or back.</param>
        /// <param name="strafe">-1 to 1: right of the aim, or left.</param>
        /// <param name="lookDelta">Change of aim this frame, in radians, positive turning left (towards north when aiming east).</param>
        /// <param name="fire">True when the fire button went down this frame.</param>
        /// <param name="seconds">How long this frame lasted.</param>
        public static List<IBattleCommand> Map(int officer, double currentAim, double forward,
            double strafe, double lookDelta, bool fire, double seconds)
        {
            var commands = new List<IBattleCommand>();
            var aim = currentAim;
            if (lookDelta != 0)
            {
                aim = Math.IEEERemainder(currentAim + lookDelta, 2 * Math.PI);
                commands.Add(new OfficerAimCommand(officer, aim));
            }
            if (forward != 0 || strafe != 0)
            {
                var fx = Math.Cos(aim); var fy = Math.Sin(aim);
                // The right of the aim is a quarter turn clockwise: (fy, -fx).
                commands.Add(new OfficerMoveCommand(officer, forward * fx + strafe * fy, forward * fy - strafe * fx, seconds));
            }
            if (fire) commands.Add(new OfficerFireCommand(officer));
            return commands;
        }
    }
}
