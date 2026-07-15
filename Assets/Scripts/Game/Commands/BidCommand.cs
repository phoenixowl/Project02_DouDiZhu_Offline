using DouDiZhu.Logic.StateMachine;

namespace DouDiZhu.Logic.Commands
{
    /// <summary>
    /// 叫地主命令，可能是叫地主或不叫地主
    /// </summary>
    public class BidCommand : ICommand
    {
        private readonly RoundController _controller;
        private readonly int _playerId;
        private readonly bool _isCalling;

        public BidCommand(RoundController controller, int playerId, bool isCalling)
        {
            _controller = controller;
            _playerId = playerId;
            _isCalling = isCalling;
        }

        public void Execute()
        {
            // 通过映射器将 PlayerID 转换为 RoundController 需要的座位索引
            int seatIndex = PlayerIdMapper.GetSeatIndex(_playerId);
            _controller.PlaceBid(seatIndex, _isCalling);
        }
    }
}