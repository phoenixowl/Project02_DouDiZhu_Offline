namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// ÓÐÍæ¼ÒÊ¤³ö
    /// </summary>
    public readonly struct PlayerWinEvent
    {
        public readonly int WinnerIndex;
        public readonly bool IsLandlordWin;
        public PlayerWinEvent(int winnerIndex, bool isLandlordWin)
        {
            WinnerIndex = winnerIndex;
            IsLandlordWin = isLandlordWin;
        }
    }
}