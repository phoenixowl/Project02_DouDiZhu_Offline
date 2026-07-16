using DouDiZhu.Logic.Models;
using System.Collections.Generic;

namespace DouDiZhu.Logic.Events
{
    public readonly struct PlayerSummary
    {
        public readonly int PlayerId;
        public readonly bool IsAI;
        public readonly string PlayerName;
        public readonly int CardCount;
        public readonly bool IsLocalPlayer;   // 是否为本地玩家（索引0）
        public readonly IReadOnlyList<Card> HandCards; // 仅当 IsLocalPlayer=true 时有效，AI玩家为 null

        public PlayerSummary(int id, bool isAI, string name, int cardCount, bool isLocal, IReadOnlyList<Card> handCards = null)
        {
            PlayerId = id;
            IsAI = isAI;
            PlayerName = name;
            CardCount = cardCount;
            IsLocalPlayer = isLocal;
            HandCards = isLocal ? handCards : null; // 非本地玩家绝不暴露手牌
        }
    }

    /// <summary>
    /// 牌局初始化完成（仅包含玩家摘要，不含底牌）
    /// </summary>
    public readonly struct GameInitializedEvent
    {
        public readonly IReadOnlyList<PlayerSummary> Players;

        public GameInitializedEvent(IReadOnlyList<PlayerSummary> players)
        {
            Players = players;
        }
    }
}