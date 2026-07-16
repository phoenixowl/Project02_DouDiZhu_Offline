namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 游戏完全结束
    /// </summary>
    public readonly struct GameOverEvent
    {
        public readonly int WinnerID;
        public readonly string WinnerName;
        public GameOverEvent(int winnerID, string winnerName)
        {
            WinnerID = winnerID;
            WinnerName = winnerName;
        }
    }
}