namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// Íæ¼Ò¹ýÅÆ
    /// </summary>
    public readonly struct PassEvent
    {
        public readonly int PlayerIndex;
        public PassEvent(int playerIndex) => PlayerIndex = playerIndex;
    }
}