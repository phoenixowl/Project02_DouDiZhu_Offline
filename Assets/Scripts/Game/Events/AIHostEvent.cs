namespace DouDiZhu.Logic.Events
{
    public readonly struct AIHostEvent
    {
        public readonly int PlayerId;
        public readonly bool IsCalling;

        public AIHostEvent(int playerId, bool isCalling)
        {
            PlayerId = playerId;
            IsCalling = isCalling;
        }
    }
}