namespace DouDiZhu.Logic.Events
{
    public readonly struct RequestReadyEvent
    {
        public readonly int PlayerId;

        public RequestReadyEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}