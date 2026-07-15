namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// ³öÅÆ±»¾Ü¾ø
    /// </summary>
    public readonly struct PlayRejectedEvent
    {
        public readonly string Reason;
        public PlayRejectedEvent(string reason) => Reason = reason;
    }
}