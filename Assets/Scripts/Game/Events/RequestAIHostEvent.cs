namespace DouDiZhu.Logic.Events
{
    public readonly struct RequestAIHostEvent
    {
        public readonly int PlayerId;

        public RequestAIHostEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}