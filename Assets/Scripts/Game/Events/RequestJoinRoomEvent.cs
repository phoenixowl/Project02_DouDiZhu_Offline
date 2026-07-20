namespace DouDiZhu.Logic.Events
{

    /// <summary>
    /// 请求加入房间
    /// </summary>
    public readonly struct RequestJoinRoomEvent
    {
        public readonly int PlayerId;
        public readonly bool IsAI;

        public RequestJoinRoomEvent(int playerId, bool isAI = false)
        {
            PlayerId = playerId;
            IsAI = isAI;
        }
    }
}