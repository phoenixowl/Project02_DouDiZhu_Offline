using DouDiZhu.Logic.AI;

namespace DouDiZhu.Logic.Events
{
    public readonly struct ReadyEvent
    {
        public readonly int PlayerId;
        public readonly bool IsReady;

        public ReadyEvent(int playerId, bool isReady)
        {
            PlayerId = playerId;
            IsReady = isReady;
        }
    }
}