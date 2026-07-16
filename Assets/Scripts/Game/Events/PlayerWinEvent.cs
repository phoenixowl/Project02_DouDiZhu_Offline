namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// ÓÐÍæ¼ÒÊ¤³ö
    /// </summary>
    public readonly struct PlayerWinEvent
    {
        public readonly int WinnerID;
        public readonly bool IsLandlordWin;
        public PlayerWinEvent(int winnerID, bool isLandlordWin)
        {
            WinnerID = winnerID;
            IsLandlordWin = isLandlordWin;
        }
    }
}