namespace DouDiZhu.Logic.StateMachine
{
    /// <summary>
    /// 斗地主游戏回合状态（状态机核心枚举）
    /// </summary>
    public enum GameRoundState
    {
        Idle,           // 空闲状态（未开始）
        Bidding,        // 叫地主阶段
        Playing,        // 出牌阶段
        RoundEnd,       // 一轮结束（清空桌面准备下一轮）
        GameOver        // 游戏结束
    }
}