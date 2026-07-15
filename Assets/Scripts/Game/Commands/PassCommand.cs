using DouDiZhu.Logic.StateMachine;

namespace DouDiZhu.Logic.Commands
{
    /// <summary>
    /// ¹ýÅÆÃüÁî
    /// </summary>
    public class PassCommand : ICommand
    {
        private readonly RoundController _controller;
        private readonly int _playerId;

        public PassCommand(RoundController controller, int playerId)
        {
            _controller = controller;
            _playerId = playerId;
        }

        public void Execute()
        {
            int seatIndex = PlayerIdMapper.GetSeatIndex(_playerId);
            _controller.Pass(seatIndex);
        }
    }
}