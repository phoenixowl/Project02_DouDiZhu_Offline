namespace DouDiZhu.Logic.Events
{
    public readonly struct RequestPassEvent
    {
        public readonly int PlayerId;

        public RequestPassEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}