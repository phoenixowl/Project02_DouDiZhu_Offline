namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 请求离开房间
    /// </summary>
    public readonly struct RequestLeaveRoomEvent
    {
        public readonly int PlayerId;

        public RequestLeaveRoomEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}