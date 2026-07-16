namespace DouDiZhu.Logic.Events
{
    public readonly struct RequestBidEvent
    {
        public readonly int PlayerId;
        public readonly bool IsCalling;

        public RequestBidEvent(int playerId, bool isCalling)
        {
            PlayerId = playerId;
            IsCalling = isCalling;
        }
    }
}