namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 一轮结束（清空桌面）
    /// </summary>
    public readonly struct RoundClearedEvent
    {
        public readonly int LastPlayedID;
        public RoundClearedEvent(int lastPlayedID) => LastPlayedID = lastPlayedID;
    }
}