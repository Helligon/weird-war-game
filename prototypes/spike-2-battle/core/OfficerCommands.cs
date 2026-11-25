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
    /// A command with the battle step it was applied at (TEC-008). The log replays in this order.
    /// </summary>
    public readonly struct LoggedCommand
    {
        public int Step { get; }
        public IBattleCommand Command { get; }

        public LoggedCommand(int step, IBattleCommand command) { Step = step; Command = command; }
    }

    /// <summary>
    /// Move: set the walking direction, a vector in map metres (X east, Y north). The Officer keeps walking that way, one
    /// battle step at a time at the stats speed, until a new move command changes it. (0, 0) stops. Lengths above 1 are cut to 1.
    /// The command carries no duration and no speed: time and speed belong to the core (TEC-008).
    /// </summary>
    public sealed class OfficerMoveCommand : OfficerCommand
    {
        public double DirectionX { get; }
        public double DirectionY { get; }

        public OfficerMoveCommand(int officer, double directionX, double directionY) : base(officer)
        {
            DirectionX = directionX; DirectionY = directionY;
        }

        public override bool Equals(object? obj) => base.Equals(obj) && obj is OfficerMoveCommand o
            && o.DirectionX == DirectionX && o.DirectionY == DirectionY;
        public override int GetHashCode() => unchecked(base.GetHashCode() * 31 + DirectionX.GetHashCode() * 7 + DirectionY.GetHashCode() * 3);
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

    /// <summary>The result of applying a command (Officer or Section).</summary>
    public sealed class CommandResult
    {
        public bool IsAccepted { get; }
        public string? Reason { get; }
        /// <summary>Set only when an Officer fire command was accepted.</summary>
        public OfficerShot? Shot { get; }

        private CommandResult(bool accepted, string? reason, OfficerShot? shot) { IsAccepted = accepted; Reason = reason; Shot = shot; }

        public static CommandResult Accepted(OfficerShot? shot = null) => new CommandResult(true, null, shot);
        public static CommandResult Rejected(string reason) => new CommandResult(false, reason, null);
    }

    /// <summary>
    /// Turns one frame of raw input numbers into Officer commands (PCH-009). It takes plain numbers, so it holds no Unity
    /// types and the same mapping is tested in the core. The only commands it can make are aim, move and fire (PCH-008).
    /// It makes a command only when something changed: an aim when the aim changes, a move when the wanted direction differs
    /// from the one the Officer already has, and a fire when the button went down.
    /// </summary>
    public static class OfficerInputMapper
    {
        /// <param name="officer">Index of the Officer.</param>
        /// <param name="currentAim">The Officer's current aim in radians.</param>
        /// <param name="currentMove">The Officer's current move direction.</param>
        /// <param name="forward">-1 to 1: forward (along the aim) or back.</param>
        /// <param name="strafe">-1 to 1: right of the aim, or left.</param>
        /// <param name="lookDelta">Change of aim this frame, in radians, positive turning left (towards north when aiming east).</param>
        /// <param name="fire">True when the fire button went down this frame.</param>
        public static List<IBattleCommand> Map(int officer, double currentAim, MapPoint currentMove, double forward,
            double strafe, double lookDelta, bool fire)
        {
            var commands = new List<IBattleCommand>();
            var aim = currentAim;
            if (lookDelta != 0)
            {
                aim = Math.IEEERemainder(currentAim + lookDelta, 2 * Math.PI);
                commands.Add(new OfficerAimCommand(officer, aim));
            }
            var fx = Math.Cos(aim); var fy = Math.Sin(aim);
            // The right of the aim is a quarter turn clockwise: (fy, -fx). Compare with what the core will store (length cut to 1),
            // so a held diagonal does not send the same direction again.
            var wantX = forward * fx + strafe * fy;
            var wantY = forward * fy - strafe * fx;
            var len = Math.Sqrt(wantX * wantX + wantY * wantY);
            var scale = len > 1 ? 1 / len : 1;
            if (Math.Abs(wantX * scale - currentMove.X) > 1e-9 || Math.Abs(wantY * scale - currentMove.Y) > 1e-9)
            {
                commands.Add(new OfficerMoveCommand(officer, wantX, wantY));
            }
            if (fire) commands.Add(new OfficerFireCommand(officer));
            return commands;
        }
    }
}
