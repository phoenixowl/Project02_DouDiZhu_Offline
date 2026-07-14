using System;
using System.Collections.Generic;
using System.Linq;

public static class CardRule
{
    /// <summary>
    /// 分析手牌，确定类型并生成权重序列
    /// </summary>
    public static void AnalyzeCardGroup(List<Card> inputCards, CardGroup group)
    {
        if (inputCards == null || inputCards.Count == 0)
        {
            group.UpdateGroupInfo(new List<Card>(), CardType.Invalid, new List<int>());
            return;
        }

        // 1. 统计各点数出现的频次
        Dictionary<int, int> rankCounts = new Dictionary<int, int>();
        foreach (var card in inputCards)
        {
            if (!rankCounts.ContainsKey((int)card.Rank))
                rankCounts[(int)card.Rank] = 0;
            rankCounts[(int)card.Rank]++;
        }

        // 2. 核心：判断牌型并生成权重序列
        (CardType type, List<int> weights) = DetermineTypeAndWeights(inputCards.Count, rankCounts);

        // 3. (可选) 对实际打出的卡牌对象进行展示排序：先放主体，再放翅膀
        List<Card> sortedDisplayCards = SortCardsByWeights(inputCards, rankCounts, type, weights);

        group.UpdateGroupInfo(sortedDisplayCards, type, weights);
    }

    /// <summary>
    /// 核心算法：通过剥离主体，精准判断类型并返回 权重序列
    /// 序列格式约定：[主体最大特征点, 翅膀1, 翅膀2...] (翅膀按降序排列)
    /// </summary>
    private static (CardType, List<int>) DetermineTypeAndWeights(int totalCount, Dictionary<int, int> counts)
    {
        // --- 1至4张的常规牌型 ---
        if (totalCount == 1) return (CardType.Single, new List<int> { counts.Keys.First() });
        if (totalCount == 2)
        {
            if (counts.ContainsValue(2)) return (CardType.Pair, new List<int> { counts.Keys.First() });
            if (counts.ContainsKey(16) && counts.ContainsKey(17)) return (CardType.KingBomb, new List<int> { 17 });
        }
        if (totalCount == 3 && counts.ContainsValue(3)) return (CardType.Triple, new List<int> { counts.Keys.First() });
        if (totalCount == 4)
        {
            if (counts.ContainsValue(4)) return (CardType.Bomb, new List<int> { counts.Keys.First() });
            if (counts.ContainsValue(3))
            {
                int tripleRank = counts.First(kv => kv.Value >= 3).Key;
                int singleRank = counts.First(kv => kv.Key != tripleRank || kv.Value == 4).Key;
                return (CardType.TripleWithOne, new List<int> { tripleRank, singleRank });
            }
        }

        // --- 5张牌以上：顺子、连对、飞机、带牌 ---

        // 三带二 (5张)
        if (totalCount == 5 && counts.ContainsValue(3) && counts.ContainsValue(2))
        {
            int tripleRank = counts.First(kv => kv.Value == 3).Key;
            int pairRank = counts.First(kv => kv.Value == 2).Key;
            return (CardType.TripleWithTwo, new List<int> { tripleRank, pairRank });
        }

        // 四带二 (6张，此处指四带两单)
        if (totalCount == 6 && counts.Any(kv => kv.Value == 4))
        {
            int fourRank = counts.First(kv => kv.Value == 4).Key;
            List<int> wings = ExtractWings(counts, new List<int> { fourRank }, 4);
            List<int> weights = new List<int> { fourRank };
            weights.AddRange(wings);
            return (CardType.FourWithTwo, weights);
        }

        // 顺子
        if (totalCount >= 5 && counts.All(kv => kv.Value == 1) && IsContiguous(counts.Keys.ToList()))
        {
            return (CardType.Straight, new List<int> { counts.Keys.Max() });
        }

        // 连对
        if (totalCount >= 6 && totalCount % 2 == 0 && counts.All(kv => kv.Value == 2) && IsContiguous(counts.Keys.ToList()))
        {
            return (CardType.DoubleStraight, new List<int> { counts.Keys.Max() });
        }

        // 纯飞机 (不带牌)
        if (totalCount >= 6 && totalCount % 3 == 0 && counts.All(kv => kv.Value == 3) && IsContiguous(counts.Keys.ToList()))
        {
            return (CardType.Airplane, new List<int> { counts.Keys.Max() });
        }

        // 飞机带单 (重点解决 333 444 3 5 问题)
        if (totalCount >= 8 && totalCount % 4 == 0)
        {
            int L = totalCount / 4; // 飞机长度
            int maxPlaneRank = FindMaxContiguousRanks(counts, L, 3);
            if (maxPlaneRank > 0)
            {
                // 生成连续的机身序列，例如 [3, 4]
                List<int> planeBody = Enumerable.Range(maxPlaneRank - L + 1, L).ToList();
                // 将机身从字典中剥离，获取剩下的所有牌作为翅膀
                List<int> wings = ExtractWings(counts, planeBody, 3);

                List<int> weights = new List<int> { maxPlaneRank };
                weights.AddRange(wings); // 翅膀内部已按降序排列
                return (CardType.AirplaneWithSingle, weights);
            }
        }

        // 飞机带对
        if (totalCount >= 10 && totalCount % 5 == 0)
        {
            int L = totalCount / 5;
            int maxPlaneRank = FindMaxContiguousRanks(counts, L, 3);
            if (maxPlaneRank > 0)
            {
                List<int> planeBody = Enumerable.Range(maxPlaneRank - L + 1, L).ToList();
                // 判断剩下的牌是否全是对子
                Dictionary<int, int> wingCounts = CloneAndDeduct(counts, planeBody, 3);
                if (wingCounts.Values.All(v => v == 0 || v % 2 == 0))
                {
                    List<int> wings = ExtractWings(counts, planeBody, 3);
                    List<int> weights = new List<int> { maxPlaneRank };
                    weights.AddRange(wings);
                    return (CardType.AirplaneWithPair, weights);
                }
            }
        }

        return (CardType.Invalid, new List<int>());
    }

    // ================= 辅助解析工具 =================

    /// <summary>
    /// 寻找长度为 L，且每张牌出现次数 >= minCount 的最大连续点数 (不含2和王)
    /// 返回最大那张牌的点数，如果找不到返回 0
    /// </summary>
    private static int FindMaxContiguousRanks(Dictionary<int, int> counts, int L, int minCount)
    {
        // 斗地主中，2(权重15)及以上不能参与顺子和飞机
        var validRanks = counts.Where(kv => kv.Value >= minCount && kv.Key < 15)
                               .Select(kv => kv.Key).OrderBy(k => k).ToList();
        if (validRanks.Count < L) return 0;

        for (int i = validRanks.Count - 1; i >= L - 1; i--)
        {
            bool isContiguous = true;
            for (int j = 0; j < L - 1; j++)
            {
                if (validRanks[i - j] - validRanks[i - j - 1] != 1)
                {
                    isContiguous = false;
                    break;
                }
            }
            if (isContiguous) return validRanks[i];
        }
        return 0;
    }

    /// <summary>
    /// 剥离主体，提取并降序排列附属翅膀
    /// </summary>
    private static List<int> ExtractWings(Dictionary<int, int> originalCounts, List<int> bodyRanks, int deductAmount)
    {
        Dictionary<int, int> tempCounts = CloneAndDeduct(originalCounts, bodyRanks, deductAmount);
        List<int> wings = new List<int>();
        foreach (var kv in tempCounts)
        {
            for (int i = 0; i < kv.Value; i++)
            {
                wings.Add(kv.Key);
            }
        }
        wings.Sort((a, b) => b.CompareTo(a)); // 翅膀降序排列，方便后续逐一比对
        return wings;
    }

    private static Dictionary<int, int> CloneAndDeduct(Dictionary<int, int> original, List<int> deductKeys, int deductAmount)
    {
        var temp = new Dictionary<int, int>(original);
        foreach (int key in deductKeys)
        {
            temp[key] -= deductAmount;
        }
        return temp;
    }

    private static bool IsContiguous(List<int> ranks)
    {
        if (ranks.Any(r => r >= 15)) return false; // 顺子不能包含2及以上
        ranks.Sort();
        return (ranks.Last() - ranks.First() == ranks.Count - 1);
    }

    /// <summary>
    /// 用于前端展示的重排序：确保无论用户怎么乱点，打出的牌必定是 主体在前，翅膀在后。
    /// </summary>
    private static List<Card> SortCardsByWeights(List<Card> input, Dictionary<int, int> counts, CardType type, List<int> weights)
    {
        if (type == CardType.Invalid) return input;

        // 首个权重是主体特征点，根据主体重排UI（可根据需要细化）
        // 简单实现：按牌出现的频次降序，频次一样按点数降序
        return input.OrderByDescending(c => counts[(int)c.Rank])
                    .ThenByDescending(c => (int)c.Rank).ToList();
    }

    // ================= 核心比牌逻辑 =================

    /// <summary>
    /// 【依据你的设计重构】按列表元素逐一比对权重
    /// </summary>
    public static bool CanBeat(CardGroup target, CardGroup baseGroup)
    {
        if (target.Type == CardType.Invalid) return false;

        // 特权牌型判定
        if (target.Type == CardType.KingBomb) return true;
        if (baseGroup.Type == CardType.KingBomb) return false;
        if (target.Type == CardType.Bomb && baseGroup.Type != CardType.Bomb) return true;
        if (target.Type != CardType.Bomb && baseGroup.Type == CardType.Bomb) return false;

        // 常规比对：牌型必须严格一致，且出牌总数必须相同
        if (target.Type != baseGroup.Type || target.Cards.Count != baseGroup.Cards.Count) return false;

        // 依序比较权重列表 (例如飞机的 [主体Max, 翅膀1, 翅膀2])
        for (int i = 0; i < target.WeightSequence.Count; i++)
        {
            if (target.WeightSequence[i] > baseGroup.WeightSequence[i]) return true;
            if (target.WeightSequence[i] < baseGroup.WeightSequence[i]) return false;
        }

        // 如果权重序列完全一致，说明出了点数一模一样的牌，斗地主规则中不能自己压自己的同点数牌
        return false;
    }
}