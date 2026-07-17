namespace DouDiZhu.Logic.Events
{
    public readonly struct RequestHintEvent
    {
        public readonly int PlayerId;

        public RequestHintEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}