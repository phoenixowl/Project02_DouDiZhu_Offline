namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// ÎÞÐ§²Ù×÷
    /// </summary>
    public readonly struct InvalidOperationEvent
    {
        public readonly string Message;
        public InvalidOperationEvent(string message) => Message = message;
    }
}