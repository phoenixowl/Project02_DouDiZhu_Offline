namespace DouDiZhu.Logic.Events
{
    public readonly struct BidPlacedEvent
    {
        public readonly int PlayerID;
        public readonly bool IsCalling;
        public BidPlacedEvent(int playerID, bool isCalling)
        {
            PlayerID = playerID;
            IsCalling = isCalling;
        }
    }
}