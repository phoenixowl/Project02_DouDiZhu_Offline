using System;
using System.Collections.Generic;
using System.Linq;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Services; // 引用 CardRule

namespace DouDiZhu.Logic.AI
{
    /// <summary>
    /// AI 决策结果
    /// </summary>
    public struct AIDecision
    {
        public bool ShouldPlay;           // true=出牌, false=过牌
        public List<Card> SelectedCards;  // 出牌时选中的卡牌
        public bool IsBomb;               // 是否炸弹（用于UI特效）

        public static AIDecision Pass() => new AIDecision { ShouldPlay = false, SelectedCards = null, IsBomb = false };
        public static AIDecision Play(List<Card> cards, bool isBomb = false)
            => new AIDecision { ShouldPlay = true, SelectedCards = cards, IsBomb = isBomb };
    }

    /// <summary>
    /// AI 逻辑服务
    /// </summary>
    public class AIPlayerService
    {
        private readonly GameState _state;
        private readonly Random _random = new Random();

        // 叫地主概率（可配置，单机版默认30%）
        private readonly float _bidProbability;

        public AIPlayerService(GameState state, float bidProbability = 0.3f)
        {
            _state = state;
            _bidProbability = bidProbability;
        }

        // ============================================================
        // 1. 叫地主决策
        // ============================================================
        public bool ShouldCallLandlord(int playerID)
        {
            // 简化策略：根据概率随机叫地主
            return _random.NextDouble() < _bidProbability;
        }

        // ============================================================
        // 2. 出牌决策（核心）
        // ============================================================
        public AIDecision DecideAction(int playerID)
        {
            var player = _state.PlayerDict[playerID];
            var handCards = player.HandCards;

            // 如果手牌为空（理论上不可能，但防御）
            if (handCards.Count == 0)
                return AIDecision.Pass();

            // 判断当前是否有上家牌（桌面牌组是否为空）
            bool isFirstPlay = (_state.TableCards == null || _state.TableCards.Cards.Count == 0);

            if (isFirstPlay)
            {
                // 策略1：首次出牌 -> 出最小的合法牌型
                return FindSmallestPlayable(handCards);
            }
            else
            {
                // 策略2：有上家牌 -> 尝试压制，否则过牌
                var targetGroup = _state.TableCards;
                return FindBestBeat(handCards, targetGroup);
            }
        }

        // ============================================================
        // 3. 首次出牌：找最小的合法牌型
        // ============================================================
        private AIDecision FindSmallestPlayable(List<Card> handCards)
        {
            // 按牌型大小顺序搜索（从最小牌型开始）
            // 顺序：单张 -> 对子 -> 三张 -> 三带一 -> 三带二 -> 顺子 -> 连对 -> 飞机 -> 炸弹 -> 王炸
            var sorted = handCards.OrderBy(c => c.Rank).ToList();

            // 3.9 飞机带单
            var airplaneWithSingle = FindSmallestAirplaneWithSingle(sorted);
            if (airplaneWithSingle != null && IsValidPlay(airplaneWithSingle))
                return AIDecision.Play(airplaneWithSingle);

            // 3.8 飞机（纯飞机，不带翅膀）
            var airplane = FindSmallestAirplane(sorted);
            if (airplane != null && IsValidPlay(airplane))
                return AIDecision.Play(airplane);

            // 3.7 连对（从3对开始，从小到大尝试）
            var doubleStraight = FindSmallestDoubleStraight(sorted);
            if (doubleStraight != null && IsValidPlay(doubleStraight))
                return AIDecision.Play(doubleStraight);

            // 3.6 顺子（从最短的5张开始，从小到大尝试）
            var straight = FindSmallestStraight(sorted);
            if (straight != null && IsValidPlay(straight))
                return AIDecision.Play(straight);

            // 3.5 三带二（最小三张 + 最小对子）
            var tripleWithTwo = FindSmallestTripleWithTwo(sorted);
            if (tripleWithTwo != null && IsValidPlay(tripleWithTwo))
                return AIDecision.Play(tripleWithTwo);

            // 3.4 三带一（最小三张 + 最小单张）
            var tripleWithOne = FindSmallestTripleWithOne(sorted);
            if (tripleWithOne != null && IsValidPlay(tripleWithOne))
                return AIDecision.Play(tripleWithOne);

            // 3.3 三张（最小三张）
            var triple = FindSmallestTriple(sorted);
            if (triple != null && IsValidPlay(triple))
                return AIDecision.Play(triple);

            // 3.2 对子（最小对子）
            var pair = FindSmallestPair(sorted);
            if (pair != null && IsValidPlay(pair))
                return AIDecision.Play(pair);

            // 3.10 飞机带对（未实现，放在最后表示弃用）
            var airplaneWithPair = FindSmallestAirplaneWithPair(sorted);
            if (airplaneWithPair != null && IsValidPlay(airplaneWithPair))
                return AIDecision.Play(airplaneWithPair);

            // 3.11 炸弹（最小炸弹）
            var bomb = FindSmallestBomb(sorted);
            if (bomb != null && IsValidPlay(bomb))
                return AIDecision.Play(bomb, true); // 标记为炸弹

            // 3.12 王炸（最后保底）
            var kingBomb = FindKingBomb(sorted);
            if (kingBomb != null && IsValidPlay(kingBomb))
                return AIDecision.Play(kingBomb, true);

            // 3.1 单张（最小）
            var single = sorted.Take(1).ToList();
            if (IsValidPlay(single))
                return AIDecision.Play(single);

            // 极端情况：手里全是无法组成合法牌型的散牌（理论上不可能，因为单张总是合法）
            // 但以防万一，出最小的单张
            return AIDecision.Play(sorted.Take(1).ToList());
        }

        // ============================================================
        // 4. 跟牌：找能压过上家的牌
        // ============================================================
        private AIDecision FindBestBeat(List<Card> handCards, CardGroup targetGroup)
        {
            // 4.1 如果是王炸，直接认输（无法压制）
            if (targetGroup.Type == CardType.KingBomb)
                return AIDecision.Pass();

            // 4.2 优先找同类型更大的牌
            var sameTypeBeat = FindSameTypeBeat(handCards, targetGroup);
            if (sameTypeBeat != null)
                return AIDecision.Play(sameTypeBeat);

            // 4.3 尝试用炸弹压制（如果上家不是炸弹）
            if (targetGroup.Type != CardType.Bomb && targetGroup.Type != CardType.KingBomb)
            {
                var bomb = FindSmallestBomb(handCards);
                if (bomb != null && IsValidPlay(bomb))
                {
                    var bombGroup = new CardGroup(bomb);
                    if (CardRule.CanBeat(bombGroup, targetGroup))
                        return AIDecision.Play(bomb, true);
                }
            }

            // 4.4 尝试用王炸压制
            var kingBomb = FindKingBomb(handCards);
            if (kingBomb != null && IsValidPlay(kingBomb))
                return AIDecision.Play(kingBomb, true);

            // 4.5 找不到能压的牌 -> 过牌
            return AIDecision.Pass();
        }

        // ============================================================
        // 5. 校验辅助：检查出牌是否合法（调用 CardRule）
        // ============================================================
        private bool IsValidPlay(List<Card> cards)
        {
            if (cards == null || cards.Count == 0) return false;
            var group = new CardGroup(cards);
            return group.Type != CardType.Invalid;
        }

        // ============================================================
        // 6. 查找工具方法（按牌型分类）
        // ============================================================

        // 6.1 找最小的对子
        private List<Card> FindSmallestPair(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            foreach (var kv in rankCounts.OrderBy(k => k.Key))
            {
                if (kv.Value >= 2 && kv.Key < 15) // 2及王不能组成对子出？
                    return sortedCards.Where(c => (int)c.Rank == kv.Key).Take(2).ToList();
            }
            return null;
        }

        // 6.2 找最小的三张
        private List<Card> FindSmallestTriple(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            foreach (var kv in rankCounts.OrderBy(k => k.Key))
            {
                if (kv.Value >= 3 && kv.Key < 15)
                    return sortedCards.Where(c => (int)c.Rank == kv.Key).Take(3).ToList();
            }
            return null;
        }

        // 6.3 找最小的三带一
        private List<Card> FindSmallestTripleWithOne(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            foreach (var kv in rankCounts.OrderBy(k => k.Key))
            {
                if (kv.Value >= 3 && kv.Key < 15)
                {
                    var triple = sortedCards.Where(c => (int)c.Rank == kv.Key).Take(3).ToList();
                    // 找一张最小的单牌（不能和 triple 同点数）
                    var single = sortedCards.FirstOrDefault(c => (int)c.Rank != kv.Key);
                    if (single != null)
                    {
                        var result = new List<Card>(triple) { single };
                        return result;
                    }
                }
            }
            return null;
        }

        // 6.4 找最小的三带二
        private List<Card> FindSmallestTripleWithTwo(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            foreach (var kv in rankCounts.OrderBy(k => k.Key))
            {
                if (kv.Value >= 3 && kv.Key < 15)
                {
                    var triple = sortedCards.Where(c => (int)c.Rank == kv.Key).Take(3).ToList();
                    // 找一对最小的对子（不能和 triple 同点数）
                    foreach (var pairKv in rankCounts.OrderBy(k => k.Key))
                    {
                        if (pairKv.Key != kv.Key && pairKv.Value >= 2)
                        {
                            var pair = sortedCards.Where(c => (int)c.Rank == pairKv.Key).Take(2).ToList();
                            var result = new List<Card>(triple);
                            result.AddRange(pair);
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        // 6.5 找最小的顺子（只找最短的5张，因为这是最小出牌策略）
        private List<Card> FindSmallestStraight(List<Card> sortedCards)
        {
            // 获取所有不同的点数（排除2和王，因为不能参与顺子）
            var ranks = sortedCards.Select(c => (int)c.Rank)
                                   .Distinct()
                                   .Where(r => r >= 3 && r <= 14)
                                   .OrderBy(r => r)
                                   .ToList();

            // 只需要检查长度为5的连续序列
            // 从最小点数开始尝试，找到的第一组就是"最小"的顺子
            for (int i = 0; i <= ranks.Count - 5; i++)
            {
                // 检查从 i 开始的 5 张是否连续
                bool isContiguous = true;
                for (int j = 0; j < 4; j++)
                {
                    if (ranks[i + j + 1] - ranks[i + j] != 1)
                    {
                        isContiguous = false;
                        break;
                    }
                }

                if (isContiguous)
                {
                    // 从手牌中取出这 5 张牌（每个点数取一张）
                    var result = new List<Card>();
                    for (int j = 0; j < 5; j++)
                    {
                        var card = sortedCards.First(c => (int)c.Rank == ranks[i + j]);
                        result.Add(card);
                    }
                    return result;
                }
            }

            return null;
        }

        //找最小的连对
        private List<Card> FindSmallestDoubleStraight(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            var pairRanks = rankCounts.Where(kv => kv.Value >= 2 && kv.Key >= 3 && kv.Key <= 14)
                                      .Select(kv => kv.Key).OrderBy(r => r).ToList();

            // 最小连对是 3 对，从最小点数开始尝试
            for (int i = 0; i <= pairRanks.Count - 3; i++)
            {
                if (pairRanks[i + 2] - pairRanks[i + 1] == 1 && pairRanks[i + 1] - pairRanks[i] == 1)
                {
                    var result = new List<Card>();
                    for (int j = 0; j < 3; j++)
                    {
                        var cards = sortedCards.Where(c => (int)c.Rank == pairRanks[i + j]).Take(2).ToList();
                        result.AddRange(cards);
                    }
                    return result;
                }
            }
            return null;
        }

        //找最小的飞机（不带翅膀
        private List<Card> FindSmallestAirplane(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            var tripleRanks = rankCounts.Where(kv => kv.Value >= 3 && kv.Key >= 3 && kv.Key <= 14)
                                        .Select(kv => kv.Key).OrderBy(r => r).ToList();

            // 最小飞机是 2 组三张，从最小点数开始尝试
            for (int i = 0; i <= tripleRanks.Count - 2; i++)
            {
                if (tripleRanks[i + 1] - tripleRanks[i] == 1)
                {
                    var result = new List<Card>();
                    for (int j = 0; j < 2; j++)
                    {
                        var cards = sortedCards.Where(c => (int)c.Rank == tripleRanks[i + j]).Take(3).ToList();
                        result.AddRange(cards);
                    }
                    return result;
                }
            }
            return null;
        }

        // 6.8 找最小的飞机带单（只搜索最短长度：2组三张 + 2张单牌翅膀）
        private List<Card> FindSmallestAirplaneWithSingle(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            var tripleRanks = rankCounts.Where(kv => kv.Value >= 3 && kv.Key >= 3 && kv.Key <= 14)
                                        .Select(kv => kv.Key)
                                        .OrderBy(r => r)
                                        .ToList();

            // 至少需要2组连续的三张
            if (tripleRanks.Count < 2)
                return null;

            // 只搜索最短长度：2组三张 (6张牌)
            // 从最小点数开始尝试，找到的第一组就是"最小"的飞机带单
            for (int i = 0; i <= tripleRanks.Count - 2; i++)
            {
                // 检查是否连续
                if (tripleRanks[i + 1] - tripleRanks[i] != 1)
                    continue;

                int bodyRank1 = tripleRanks[i];
                int bodyRank2 = tripleRanks[i + 1];

                // ---- 构造机身（6张牌） ----
                var result = new List<Card>();
                result.AddRange(sortedCards.Where(c => (int)c.Rank == bodyRank1).Take(3));
                result.AddRange(sortedCards.Where(c => (int)c.Rank == bodyRank2).Take(3));

                // ---- 从剩余牌中取最小的2张作为翅膀（可以与机身同点数） ----
                var tempCounts = new Dictionary<int, int>(rankCounts);
                tempCounts[bodyRank1] -= 3;
                tempCounts[bodyRank2] -= 3;

                int wingsNeeded = 2;
                var wings = new List<Card>();
                foreach (var kv in tempCounts.Where(k => k.Value > 0).OrderBy(k => k.Key))
                {
                    int take = Math.Min(kv.Value, wingsNeeded - wings.Count);
                    var cardsOfRank = sortedCards.Where(c => (int)c.Rank == kv.Key).ToList();
                    for (int j = 0; j < take; j++)
                    {
                        wings.Add(cardsOfRank[j]);
                    }
                    if (wings.Count >= wingsNeeded)
                        break;
                }

                // 如果翅膀数量足够，组合并返回
                if (wings.Count == wingsNeeded)
                {
                    result.AddRange(wings);
                    return result;
                }

                // 如果翅膀不够，说明剩余牌不足，继续尝试下一组连续三张
            }

            return null;
        }

        // 6.9 找最小的飞机带对（略复杂，先实现基础版）
        private List<Card> FindSmallestAirplaneWithPair(List<Card> sortedCards)
        {
            // 简化实现：先调用 FindSmallestAirplaneWithSingle，然后看能否把翅膀改成对子
            // 实际上更复杂，但作为简化AI，可以暂不实现飞机带对
            // 或者直接复用 FindSmallestAirplaneWithSingle 的逻辑，让AI出飞机带单（更保守）
            // 这里为了完整性，返回 null，让AI转而尝试炸弹
            return null;
        }

        // 6.10 找最小的炸弹
        private List<Card> FindSmallestBomb(List<Card> sortedCards)
        {
            var rankCounts = GetRankCounts(sortedCards);
            foreach (var kv in rankCounts.OrderBy(k => k.Key))
            {
                if (kv.Value >= 4 && kv.Key >= 3 && kv.Key <= 15) // 2也可以炸弹
                    return sortedCards.Where(c => (int)c.Rank == kv.Key).Take(4).ToList();
            }
            return null;
        }

        // 6.11 找王炸
        private List<Card> FindKingBomb(List<Card> sortedCards)
        {
            var smallJoker = sortedCards.FirstOrDefault(c => c.Rank == CardRank.SmallJoker);
            var bigJoker = sortedCards.FirstOrDefault(c => c.Rank == CardRank.BigJoker);
            if (smallJoker != null && bigJoker != null)
                return new List<Card> { smallJoker, bigJoker };
            return null;
        }

        // 6.12 找同类型更大的牌组（核心压制逻辑）
        private List<Card> FindSameTypeBeat(List<Card> handCards, CardGroup targetGroup)
        {
            // 根据目标牌型，分别查找更大的组合
            switch (targetGroup.Type)
            {
                case CardType.Single:
                    return FindBiggerSingle(handCards, targetGroup.WeightSequence[0]);
                case CardType.Pair:
                    return FindBiggerPair(handCards, targetGroup.WeightSequence[0]);
                case CardType.Triple:
                    return FindBiggerTriple(handCards, targetGroup.WeightSequence[0]);
                case CardType.TripleWithOne:
                    return FindBiggerTripleWithOne(handCards, targetGroup);
                case CardType.TripleWithTwo:
                    return FindBiggerTripleWithTwo(handCards, targetGroup);
                case CardType.Straight:
                    return FindBiggerStraight(handCards, targetGroup);
                case CardType.DoubleStraight:
                    return FindBiggerDoubleStraight(handCards, targetGroup);
                case CardType.Airplane:
                    return FindBiggerAirplane(handCards, targetGroup);
                case CardType.AirplaneWithSingle:
                    return FindBiggerAirplaneWithSingle(handCards, targetGroup);
                case CardType.Bomb:
                    return FindBiggerBomb(handCards, targetGroup.WeightSequence[0]);
                default:
                    return null;
            }
        }

        // --- 具体实现（按牌型） ---

        private List<Card> FindBiggerSingle(List<Card> hand, int minRank)
        {
            var card = hand.Where(c => (int)c.Rank > minRank).OrderBy(c => c.Rank).FirstOrDefault();
            return card != null ? new List<Card> { card } : null;
        }

        private List<Card> FindBiggerPair(List<Card> hand, int minRank)
        {
            var rankCounts = GetRankCounts(hand);
            foreach (var kv in rankCounts.Where(k => k.Key > minRank).OrderBy(k => k.Key))
            {
                if (kv.Value >= 2)
                    return hand.Where(c => (int)c.Rank == kv.Key).Take(2).ToList();
            }
            return null;
        }

        private List<Card> FindBiggerTriple(List<Card> hand, int minRank)
        {
            var rankCounts = GetRankCounts(hand);
            foreach (var kv in rankCounts.Where(k => k.Key > minRank).OrderBy(k => k.Key))
            {
                if (kv.Value >= 3)
                    return hand.Where(c => (int)c.Rank == kv.Key).Take(3).ToList();
            }
            return null;
        }

        private List<Card> FindBiggerTripleWithOne(List<Card> hand, CardGroup target)
        {
            // 需要更大的三张 + 任意一张不同点数的牌
            int tripleRank = target.WeightSequence[0];
            int wingRank = target.WeightSequence[1]; // 单张的点数

            var rankCounts = GetRankCounts(hand);
            foreach (var kv in rankCounts.Where(k => k.Key > tripleRank).OrderBy(k => k.Key))
            {
                if (kv.Value >= 3)
                {
                    var triple = hand.Where(c => (int)c.Rank == kv.Key).Take(3).ToList();
                    // 找一张最小的单张（不能和 triple 同点数）
                    var single = hand.FirstOrDefault(c => (int)c.Rank != kv.Key && !triple.Contains(c));
                    if (single != null)
                    {
                        var result = new List<Card>(triple) { single };
                        return result;
                    }
                }
            }
            return null;
        }

        private List<Card> FindBiggerTripleWithTwo(List<Card> hand, CardGroup target)
        {
            // 更大的三张 + 任意一对
            int tripleRank = target.WeightSequence[0];
            int pairRank = target.WeightSequence[1];

            var rankCounts = GetRankCounts(hand);
            foreach (var kv in rankCounts.Where(k => k.Key > tripleRank).OrderBy(k => k.Key))
            {
                if (kv.Value >= 3)
                {
                    var triple = hand.Where(c => (int)c.Rank == kv.Key).Take(3).ToList();
                    // 找一对最小的对子
                    foreach (var pairKv in rankCounts.Where(k => k.Key != kv.Key && k.Value >= 2).OrderBy(k => k.Key))
                    {
                        var pair = hand.Where(c => (int)c.Rank == pairKv.Key).Take(2).ToList();
                        var result = new List<Card>(triple);
                        result.AddRange(pair);
                        return result;
                    }
                }
            }
            return null;
        }
        private List<Card> FindBiggerStraight(List<Card> hand, CardGroup target)
        {
            int length = target.Cards.Count;
            int targetMaxRank = target.WeightSequence[0]; // 上家顺子的最大牌

            // 获取手牌中所有可能的点数（排除2和王）
            var distinctRanks = hand.Select(c => (int)c.Rank).Distinct()
                                    .Where(r => r >= 3 && r <= 14)
                                    .OrderBy(r => r).ToList();

            // 尝试所有可能的起始点（从最小的3开始）
            for (int start = 3; start <= 14 - length + 1; start++)
            {
                bool found = true;
                for (int i = 0; i < length; i++)
                {
                    if (!distinctRanks.Contains(start + i))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    int candidateMaxRank = start + length - 1;
                    // 核心修正：只要最大牌大于上家的最大牌即可
                    if (candidateMaxRank > targetMaxRank)
                    {
                        var result = new List<Card>();
                        for (int i = 0; i < length; i++)
                        {
                            // 从手牌中取出一张对应点数的牌（如果有重复点数，确保取一张）
                            var card = hand.First(c => (int)c.Rank == start + i);
                            result.Add(card);
                        }
                        return result;
                    }
                }
            }
            return null;
        }

        private List<Card> FindBiggerDoubleStraight(List<Card> hand, CardGroup target)
        {
            int length = target.Cards.Count / 2; // 连对的对数
            int targetMaxRank = target.WeightSequence[0]; // 上家连对的最大点数

            var rankCounts = GetRankCounts(hand);
            var pairRanks = rankCounts.Where(kv => kv.Value >= 2 && kv.Key >= 3 && kv.Key <= 14)
                                      .Select(kv => kv.Key).OrderBy(r => r).ToList();

            // 尝试所有可能的起始点（从3开始）
            for (int start = 3; start <= 14 - length + 1; start++)
            {
                bool found = true;
                for (int i = 0; i < length; i++)
                {
                    if (!pairRanks.Contains(start + i))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    int candidateMaxRank = start + length - 1;
                    // 修正：最大牌大于上家的最大牌即可
                    if (candidateMaxRank > targetMaxRank)
                    {
                        var result = new List<Card>();
                        for (int i = 0; i < length; i++)
                        {
                            var cards = hand.Where(c => (int)c.Rank == start + i).Take(2).ToList();
                            result.AddRange(cards);
                        }
                        return result;
                    }
                }
            }
            return null;
        }

        private List<Card> FindBiggerAirplane(List<Card> hand, CardGroup target)
        {
            int length = target.Cards.Count / 3; // 飞机的长度
            int targetMaxRank = target.WeightSequence[0];

            var rankCounts = GetRankCounts(hand);
            var tripleRanks = rankCounts.Where(kv => kv.Value >= 3 && kv.Key >= 3 && kv.Key <= 14)
                                        .Select(kv => kv.Key).OrderBy(r => r).ToList();

            for (int start = 3; start <= 14 - length + 1; start++)
            {
                bool found = true;
                for (int i = 0; i < length; i++)
                {
                    if (!tripleRanks.Contains(start + i))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    int candidateMaxRank = start + length - 1;
                    if (candidateMaxRank > targetMaxRank)
                    {
                        var result = new List<Card>();
                        for (int i = 0; i < length; i++)
                        {
                            var cards = hand.Where(c => (int)c.Rank == start + i).Take(3).ToList();
                            result.AddRange(cards);
                        }
                        return result;
                    }
                }
            }
            return null;
        }

        private List<Card> FindBiggerAirplaneWithSingle(List<Card> hand, CardGroup target)
        {
            int length = target.Cards.Count / 4; // 飞机长度
            int targetMaxRank = target.WeightSequence[0];

            var rankCounts = GetRankCounts(hand);
            var tripleRanks = rankCounts.Where(kv => kv.Value >= 3 && kv.Key >= 3 && kv.Key <= 14)
                                        .Select(kv => kv.Key).OrderBy(r => r).ToList();

            for (int start = 3; start <= 14 - length + 1; start++)
            {
                bool found = true;
                for (int i = 0; i < length; i++)
                {
                    if (!tripleRanks.Contains(start + i))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    int candidateMaxRank = start + length - 1;
                    if (candidateMaxRank > targetMaxRank)
                    {
                        var result = new List<Card>();
                        var bodyRanks = Enumerable.Range(start, length).ToList();

                        // 取机身
                        foreach (var r in bodyRanks)
                        {
                            var cards = hand.Where(c => (int)c.Rank == r).Take(3).ToList();
                            result.AddRange(cards);
                        }

                        // 取翅膀（最小的单张，不能来自机身）
                        var tempCounts = new Dictionary<int, int>(rankCounts);
                        foreach (var r in bodyRanks) tempCounts[r] -= 3;

                        int wingsNeeded = length;
                        var wingRanks = new List<int>();
                        foreach (var kv in tempCounts.Where(k => k.Value > 0).OrderBy(k => k.Key))
                        {
                            int take = Math.Min(kv.Value, wingsNeeded - wingRanks.Count);
                            for (int j = 0; j < take; j++) wingRanks.Add(kv.Key);
                        }

                        if (wingRanks.Count == wingsNeeded)
                        {
                            foreach (var r in wingRanks)
                            {
                                var card = hand.First(c => (int)c.Rank == r && !result.Contains(c));
                                result.Add(card);
                            }
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        private List<Card> FindBiggerBomb(List<Card> hand, int minRank)
        {
            var rankCounts = GetRankCounts(hand);
            foreach (var kv in rankCounts.Where(k => k.Key > minRank).OrderBy(k => k.Key))
            {
                if (kv.Value >= 4)
                    return hand.Where(c => (int)c.Rank == kv.Key).Take(4).ToList();
            }
            return null;
        }

        // ============================================================
        // 7. 通用工具
        // ============================================================
        private Dictionary<int, int> GetRankCounts(List<Card> cards)
        {
            var dict = new Dictionary<int, int>();
            foreach (var c in cards)
            {
                int rank = (int)c.Rank;
                if (!dict.ContainsKey(rank)) dict[rank] = 0;
                dict[rank]++;
            }
            return dict;
        }
    }
}