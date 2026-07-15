namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 游戏完全结束
    /// </summary>
    public readonly struct GameOverEvent
    {
        public readonly int WinnerIndex;
        public readonly string WinnerName;
        public GameOverEvent(int winnerIndex, string winnerName)
        {
            WinnerIndex = winnerIndex;
            WinnerName = winnerName;
        }
    }
}