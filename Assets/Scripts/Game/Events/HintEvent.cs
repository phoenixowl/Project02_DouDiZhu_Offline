using DouDiZhu.Logic.AI;

namespace DouDiZhu.Logic.Events
{
    public readonly struct HintEvent
    {
        public readonly int PlayerId;
        public readonly AIDecision AIDecision;

        public HintEvent(int playerId, AIDecision aIDecision)
        {
            PlayerId = playerId;
            AIDecision = aIDecision;
        }
    }
}