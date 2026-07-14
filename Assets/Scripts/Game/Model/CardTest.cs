using System;
using System.Collections.Generic;

public static class CardTest
{
    private static readonly Random random = new Random();

    /// <summary>
    /// 一键随机生成指定数量的 Card 数组（列表）
    /// </summary>
    /// <param name="count">需要生成的卡牌张数（斗地主手牌一般为 17 或 20）</param>
    public static List<Card> GenerateRandomCards(int count)
    {
        // 容错处理
        if (count < 0) count = 0;
        if (count > 54) count = 54;

        // 1. 创建一副完整的 54 张扑克牌堆
        List<Card> fullDeck = CreateFullDeck();

        // 2. 使用 Fisher-Yates 洗牌算法将牌堆完全打乱
        Shuffle(fullDeck);

        // 3. 从打乱后的牌堆中截取指定张数的卡牌
        List<Card> resultCards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            resultCards.Add(fullDeck[i]);
        }

        return resultCards;
    }

    /// <summary>
    /// 创建一副包含 54 张牌的标准完整牌堆
    /// </summary>
    private static List<Card> CreateFullDeck()
    {
        List<Card> deck = new List<Card>();

        // 生成常规的 52 张牌 (点数 3 到 15, 15代表2)
        CardSuit[] normalSuits = { CardSuit.Heart, CardSuit.Diamond, CardSuit.Club, CardSuit.Spade };

        for (int rank = 3; rank <= 15; rank++)
        {
            string name = GetCardName(rank);
            foreach (var suit in normalSuits)
            {
                deck.Add(new Card(suit,(CardRank)rank));
            }
        }

        // 生成小王
        deck.Add(new Card(CardSuit.None, CardRank.SmallJoker));

        // 生成大王
        deck.Add(new Card(CardSuit.None, CardRank.BigJoker));
        return deck;
    }

    /// <summary>
    /// 映射点数的显示文本
    /// </summary>
    private static string GetCardName(int rank)
    {
        if (rank >= 3 && rank <= 10) return rank.ToString();
        switch (rank)
        {
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            case 14: return "A";
            case 15: return "2";
            default: return rank.ToString();
        }
    }

    /// <summary>
    /// 纯 C# 乱序洗牌算法
    /// </summary>
    private static void Shuffle(List<Card> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            Card value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}