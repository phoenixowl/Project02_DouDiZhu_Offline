namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// 通用日志事件（用于内部调试）
    /// </summary>
    public readonly struct GameLogEvent
    {
        public readonly string Message;
        public GameLogEvent(string message) => Message = message;
    }
}