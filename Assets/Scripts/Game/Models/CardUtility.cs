using System;
using System.Collections.Generic;

namespace DouDiZhu.Logic.Models
{
    /// <summary>
    /// 扑克牌堆工具类（纯数据操作，零Unity依赖）
    /// </summary>
    public static class CardDeckUtility
    {
        // 使用 ThreadStatic 确保多线程安全（单机版非必须，但好习惯）
        [ThreadStatic]
        private static Random _rng;

        private static Random Rng
        {
            get
            {
                if (_rng == null) _rng = new Random();
                return _rng;
            }
        }

        /// <summary>
        /// 创建一副包含 54 张牌的标准完整牌堆
        /// 顺序：3~2各4张（共52张）+ 小王 + 大王
        /// </summary>
        public static List<Card> CreateFullDeck()
        {
            List<Card> deck = new List<Card>(54);

            // 1. 常规花色牌（点数 3~15，对应枚举 Three~Two）
            CardSuit[] normalSuits = { CardSuit.Heart, CardSuit.Diamond, CardSuit.Club, CardSuit.Spade };
            for (int rank = 3; rank <= 15; rank++)  // 3~15 对应 CardRank.Three ~ CardRank.Two
            {
                foreach (var suit in normalSuits)
                {
                    deck.Add(new Card(suit, (CardRank)rank));
                }
            }

            // 2. 大小王（花色为 None）
            deck.Add(new Card(CardSuit.None, CardRank.SmallJoker));
            deck.Add(new Card(CardSuit.None, CardRank.BigJoker));

            return deck;
        }

        /// <summary>
        /// Fisher-Yates 洗牌算法（原地打乱，时间复杂度 O(n)）
        /// </summary>
        public static void Shuffle<T>(IList<T> list)
        {
            if (list == null || list.Count <= 1) return;

            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);        // 0 <= j <= i
                // 交换 list[i] 和 list[j]
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 生成一副随机打乱后的完整牌堆（直接返回洗好的牌）
        /// 便捷方法：CreateFullDeck() + Shuffle() 一步到位
        /// </summary>
        public static List<Card> CreateShuffledDeck()
        {
            List<Card> deck = CreateFullDeck();
            Shuffle(deck);
            return deck;
        }
    }
}