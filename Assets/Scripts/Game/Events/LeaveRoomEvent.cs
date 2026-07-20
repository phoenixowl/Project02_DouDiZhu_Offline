namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 玩家离开房间成功
    /// </summary>
    public readonly struct LeaveRoomEvent
    {
        public readonly int PlayerId;

        public LeaveRoomEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}