namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// Íæ¼Ò¹ýÅÆ
    /// </summary>
    public readonly struct PassEvent
    {
        public readonly int PlayerID;
        public PassEvent(int playerID) => PlayerID = playerID;
    }
}