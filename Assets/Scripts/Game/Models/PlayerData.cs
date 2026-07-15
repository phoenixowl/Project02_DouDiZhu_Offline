using System;
using System.Collections.Generic;

namespace DouDiZhu.Logic.Models
{
    /// <summary>
    /// 玩家身份枚举（联机版可扩展为 PlayerID）
    /// </summary>
    public enum PlayerIdentity
    {
        Farmer = 0,   // 农民
        Landlord = 1  // 地主
    }

    /// <summary>
    /// 玩家数据实体（纯数据容器，不包含任何逻辑）
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public int PlayerID { get; private set; }          // 0=本地玩家, 1=AI_1, 2=AI_2
        public string PlayerName { get; set; }             // 仅用于UI显示
        public List<Card> HandCards { get; private set; }  // 当前手牌
        public PlayerIdentity Identity { get; set; }       // 身份（地主/农民）
        public bool IsReady { get; set; }                  // 是否准备就绪（联机版用）

        public PlayerData(int id, string name = "")
        {
            PlayerID = id;
            PlayerName = string.IsNullOrEmpty(name) ? $"Player_{id}" : name;
            HandCards = new List<Card>();
            Identity = PlayerIdentity.Farmer; // 默认为农民
            IsReady = false;
        }

        /// <summary>
        /// 添加手牌（发牌或接收底牌时使用）
        /// </summary>
        public void AddCards(IEnumerable<Card> cards)
        {
            HandCards.AddRange(cards);
        }

        /// <summary>
        /// 移除手牌（出牌时使用）
        /// </summary>
        public void RemoveCards(IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                HandCards.Remove(card); // Card 需要重写 Equals/GetHashCode
            }
        }

        /// <summary>
        /// 获取手牌数量（UI显示用）
        /// </summary>
        public int CardCount => HandCards.Count;

        /// <summary>
        /// 清空手牌（游戏重置时使用）
        /// </summary>
        public void ClearHand()
        {
            HandCards.Clear();
        }

        /// <summary>
        /// 按斗地主规则排序手牌（3~2, 小王, 大王）
        /// </summary>
        public void SortHand()
        {
            HandCards.Sort();
        }

        public override string ToString()
        {
            return $"[Player {PlayerID}] {PlayerName} ({Identity}) - Cards: {HandCards.Count}";
        }
    }
}