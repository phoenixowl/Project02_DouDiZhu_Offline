namespace DouDiZhu.Logic.Events
{
    public readonly struct BidPlacedEvent
    {
        public readonly int PlayerID;
        public readonly bool IsCalling;
        public readonly bool IsConfirmRound;
        public BidPlacedEvent(int playerID, bool isCalling, bool isConfirmRound = false)
        {
            PlayerID = playerID;
            IsCalling = isCalling;
            IsConfirmRound = isConfirmRound;
        }
    }
}