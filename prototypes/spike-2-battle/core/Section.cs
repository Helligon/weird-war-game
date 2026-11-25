using System;

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
        public MapPoint Position { get; }

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

        /// <summary>Reduces health, not below 0.</summary>
        internal void TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Max(0, Health - amount);
        }
    }

    /// <summary>
    /// An Officer (HIE-006): a separate Unit, never part of a Section and never counted in a Section's Unit count.
    /// </summary>
    public sealed class Officer
    {
        public Side Side { get; }
        public int Health { get; }
        public MapPoint Position { get; }

        public Officer(Side side, int health, MapPoint position) { Side = side; Health = health; Position = position; }
    }
}
