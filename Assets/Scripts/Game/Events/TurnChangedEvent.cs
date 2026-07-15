namespace DouDiZhu.Logic.Events
{
    public readonly struct TurnChangedEvent
    {
        public readonly int PlayerIndex;
        public TurnChangedEvent(int playerIndex) => PlayerIndex = playerIndex;
    }
}