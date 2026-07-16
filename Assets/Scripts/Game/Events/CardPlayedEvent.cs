using DouDiZhu.Logic.Models;
using System.Collections.Generic;

namespace DouDiZhu.Logic.Events
{
    /// <summary>
    /// ³öÅÆ³É¹¦
    /// </summary>
    public readonly struct CardPlayedEvent
    {
        public readonly int PlayerID;
        public readonly CardGroup CardGroup;
        public CardPlayedEvent(int playerID, CardGroup cardGroup)
        {
            PlayerID = playerID;
            CardGroup = cardGroup;
        }
    }
}