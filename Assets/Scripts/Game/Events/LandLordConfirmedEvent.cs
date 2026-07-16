using DouDiZhu.Logic.Models;
using System.Collections.Generic;
namespace DouDiZhu.Logic.Events
{
    public readonly struct LandlordConfirmedEvent
    {
        public readonly int LandlordID;
        public readonly IReadOnlyList<Card> HoleCards; // 此时底牌才随事件发出（已翻开）
        public LandlordConfirmedEvent(int landlordID, IReadOnlyList<Card> holeCards)
        {
            LandlordID = landlordID;
            HoleCards = holeCards;
        }
    }
}