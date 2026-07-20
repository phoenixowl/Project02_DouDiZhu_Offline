namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 游戏完全结束
    /// </summary>
    public readonly struct GameOverEvent
    {
        public readonly int WinnerID;
        public readonly string WinnerName;
        public readonly bool IsLandLord;
        public GameOverEvent(int winnerID, string winnerName, bool isLandLord)
        {
            WinnerID = winnerID;
            WinnerName = winnerName;
            IsLandLord = isLandLord;
        }
    }
}