using DouDiZhu.Logic.Models;
using System.Collections.Generic;
namespace DouDiZhu.Logic.Events
{
    public readonly struct RequestPlayCardEvent
    {
        public readonly int PlayerId;
        public readonly List<Card> SelectedCards;

        public RequestPlayCardEvent(int playerId, List<Card> selectedCards)
        {
            PlayerId = playerId;
            SelectedCards = selectedCards;
        }
    }
}