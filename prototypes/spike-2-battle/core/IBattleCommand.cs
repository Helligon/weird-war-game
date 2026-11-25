namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// A player action as plain data (DLV-013). Input and UI code create commands and never
    /// change state directly. Concrete commands arrive in later Stories.
    /// </summary>
    public interface IBattleCommand
    {
    }
}
