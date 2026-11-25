using System;
using System.Collections.Generic;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>The Unit types in the spike. Riflemen only (ROS-001); other types arrive with later Stories.</summary>
    public enum UnitType
    {
        Riflemen = 1,
    }

    /// <summary>
    /// A Section (HIE-002): a group of Units of one type. Its size is derived from its health (HIE-004), rounded up (HIE-010).
    /// Units are not stored one by one: a Unit is a single fighting member, counted from health.
    /// </summary>
    public sealed class Section
    {
        public SectionId Id { get; }
        public Side Side { get; }
        public UnitType Type { get; }
        /// <summary>P-HIE-01: Units in a full-strength Section of this type.</summary>
        public int MaxUnits { get; }
        /// <summary>P-HIE-02: health of one Unit of this type.</summary>
        public int HealthPerUnit { get; }
        public int Health { get; private set; }
        public MapPoint Position { get; private set; }
        /// <summary>DAI-004: true while the Section holds its position (set by Stop, CMD-006, or by arriving after Fall back, CMD-011).</summary>
        public bool IsHolding { get; internal set; }
        private readonly List<QueuedWaypoint> _waypoints = new List<QueuedWaypoint>();
        /// <summary>CMD-005, CMD-020: the queued waypoints, in the order they are visited.</summary>
        internal IReadOnlyList<QueuedWaypoint> QueuedWaypoints => _waypoints;
        /// <summary>The positions of the queued waypoints, in order (CMD-005).</summary>
        public IReadOnlyList<MapPoint> Waypoints => _waypoints.ConvertAll(w => w.Point);
        /// <summary>CMD-006: when the last queued waypoint is reached, hold there (a Stop at the end of the orders).</summary>
        public bool StopAtEnd { get; internal set; }
        /// <summary>CMD-005: true once the Section has reached a waypoint next to cover and is stepping into that cover.</summary>
        internal bool TakingCover { get; set; }
        /// <summary>CMD-008: the enemy Section this one was told to attack, or null.</summary>
        public SectionId? OrderedTarget { get; internal set; }
        /// <summary>DAI-006: who last fired at this one, if anyone: an enemy Section or an enemy Officer.</summary>
        public Attacker? LastAttacker { get; internal set; }
        /// <summary>Seconds until this Section may fire its next volley (default behaviour; 0 means ready).</summary>
        public double VolleyCooldown { get; internal set; }

        public Section(SectionId id, Side side, UnitType type, int maxUnits, int healthPerUnit, MapPoint position)
        {
            if (maxUnits < 1) throw new ArgumentOutOfRangeException(nameof(maxUnits), "A Section needs at least 1 Unit (HIE-002).");
            if (healthPerUnit < 1) throw new ArgumentOutOfRangeException(nameof(healthPerUnit), "Health per Unit must be at least 1.");
            Id = id;
            Side = side;
            Type = type;
            MaxUnits = maxUnits;
            HealthPerUnit = healthPerUnit;
            Health = maxUnits * healthPerUnit;
            Position = position;
        }

        /// <summary>HIE-004, HIE-010: health divided by health per Unit, rounded up. 0 when destroyed.</summary>
        public int UnitCount => (Health + HealthPerUnit - 1) / HealthPerUnit;

        /// <summary>HIE-004: a Section at health 0 is destroyed and has no Units.</summary>
        public bool IsDestroyed => Health <= 0;

        internal void MoveTo(MapPoint p) { Position = p; }

        internal void Enqueue(MapPoint point, bool seekCover) { _waypoints.Add(new QueuedWaypoint(point, seekCover)); }
        internal void DequeueWaypoint() { _waypoints.RemoveAt(0); TakingCover = false; }
        internal void ClearWaypoints() { _waypoints.Clear(); StopAtEnd = false; TakingCover = false; }

        /// <summary>A destroyed Section keeps no orders (stand-in: the rules do not say).</summary>
        private void DropOrders() { _waypoints.Clear(); StopAtEnd = false; TakingCover = false; IsHolding = false; OrderedTarget = null; }

        /// <summary>Reduces health, not below 0.</summary>
        internal void TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Max(0, Health - amount);
            if (Health == 0) DropOrders();
        }
    }

    /// <summary>A waypoint in a Section's queue. <see cref="SeekCover"/> is false for the Fall back point (CMD-011: hold at the edge).</summary>
    internal readonly struct QueuedWaypoint
    {
        public MapPoint Point { get; }
        public bool SeekCover { get; }

        public QueuedWaypoint(MapPoint point, bool seekCover) { Point = point; SeekCover = seekCover; }
    }

    /// <summary>DAI-006: who fired at a Section. Exactly one of the two is set: an enemy Section, or an enemy Officer.</summary>
    public readonly struct Attacker
    {
        public SectionId? Section { get; }
        public Officer? Officer { get; }

        public Attacker(SectionId section) { Section = section; Officer = null; }
        public Attacker(Officer officer) { Section = null; Officer = officer; }
    }

    /// <summary>
    /// An Officer (HIE-006): a separate Unit, never part of a Section and never counted in a Section's Unit count.
    /// </summary>
    public sealed class Officer
    {
        public Side Side { get; }
        public int Health { get; private set; }
        public MapPoint Position { get; internal set; }
        /// <summary>Where the Officer aims, in radians. 0 points east (+X); positive turns towards north (+Y).</summary>
        public double AimRadians { get; internal set; }
        /// <summary>
        /// The direction the Officer is walking, as a vector in map metres (X east, Y north) with length 0 to 1. Zero means standing still.
        /// It stays until a new move command changes it, and each battle step moves the Officer along it at the Officer's speed (TEC-008).
        /// </summary>
        public MapPoint MoveDirection { get; internal set; }

        public Officer(Side side, int health, MapPoint position) { Side = side; Health = health; Position = position; }

        /// <summary>True when the Officer's health is 0.</summary>
        public bool IsDestroyed => Health <= 0;

        internal void TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Max(0, Health - amount);
        }
    }
}
