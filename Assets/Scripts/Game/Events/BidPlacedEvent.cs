namespace DouDiZhu.Logic.Events
{
    public readonly struct BidPlacedEvent
    {
        public readonly int PlayerIndex;
        public readonly bool IsCalling;
        public BidPlacedEvent(int playerIndex, bool isCalling)
        {
            PlayerIndex = playerIndex;
            IsCalling = isCalling;
        }
    }
}