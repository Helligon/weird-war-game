namespace WeirdWar.Spike2.Battle.Core
{
    public enum Side { Player = 1, Enemy = 2 }

    /// <summary>What validation needs to know about one Section.</summary>
    public readonly struct SectionInfo
    {
        public Side Side { get; }
        public int QueuedWaypoints { get; }

        public SectionInfo(Side side, int queuedWaypoints) { Side = side; QueuedWaypoints = queuedWaypoints; }
    }

    /// <summary>
    /// Read-only view of the battle used to validate commands. Stand-in: the real battle state does not exist
    /// yet, so validation depends only on this minimal interface.
    /// </summary>
    public interface IBattleView
    {
        bool TryGetSection(SectionId id, out SectionInfo info);
    }
}
