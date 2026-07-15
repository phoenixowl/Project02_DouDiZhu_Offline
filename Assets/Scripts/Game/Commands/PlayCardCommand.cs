using System.Collections.Generic;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.StateMachine;

namespace DouDiZhu.Logic.Commands
{
    /// <summary>
    /// ³öÅÆÃüÁî
    /// </summary>
    public class PlayCardCommand : ICommand
    {
        private readonly RoundController _controller;
        private readonly int _playerId;
        private readonly List<Card> _selectedCards;

        public PlayCardCommand(RoundController controller, int playerId, List<Card> selectedCards)
        {
            _controller = controller;
            _playerId = playerId;
            _selectedCards = selectedCards;
        }

        public void Execute()
        {
            int seatIndex = PlayerIdMapper.GetSeatIndex(_playerId);
            _controller.PlayCards(seatIndex, _selectedCards);
        }
    }
}