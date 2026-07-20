namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 玩家加入房间成功
    /// </summary>
    public readonly struct JoinRoomEvent
    {
        public readonly int PlayerId;
        public readonly bool IsAI;

        public JoinRoomEvent(int playerId, bool isAI)
        {
            PlayerId = playerId;
            IsAI = isAI;
        }
    }
}