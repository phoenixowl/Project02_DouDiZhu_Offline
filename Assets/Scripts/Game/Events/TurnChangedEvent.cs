namespace DouDiZhu.Logic.Events
{
    public readonly struct TurnChangedEvent
    {
        public readonly int PlayerID;
        public TurnChangedEvent(int playerID) => PlayerID = playerID;
    }
}