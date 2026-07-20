namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 房主变更
    /// </summary>
    public readonly struct RoomOwnerChangedEvent
    {
        public readonly int NewOwnerId;

        public RoomOwnerChangedEvent(int newOwnerId)
        {
            NewOwnerId = newOwnerId;
        }
    }
}