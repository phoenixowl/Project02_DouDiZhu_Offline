using System;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 扑克牌花色
/// </summary>
public enum CardSuit
{
    None = 0,    // 无花色（用于大小王）
    Club = 1,    // 梅花 ♣
    Diamond = 2, // 方块 ♦
    Heart = 3,   // 红桃 ♥
    Spade = 4    // 黑桃 ♠
}

/// <summary>
/// 扑克牌逻辑大小 (斗地主规则)
/// 3 < 4 < 5 < 6 < 7 < 8 < 9 < 10 < J < Q < K < A < 2 < 小王 < 大王
/// </summary>
public enum CardRank
{
    Rank3 = 3,
    Rank4 = 4,
    Rank5 = 5,
    Rank6 = 6,
    Rank7 = 7,
    Rank8 = 8,
    Rank9 = 9,
    Rank10 = 10,
    RankJ = 11,
    RankQ = 12,
    RankK = 13,
    RankA = 14,
    Rank2 = 15,
    SmallJoker = 16, // 小王
    BigJoker = 17    // 大王
}

/// <summary>
/// 扑克牌实体类，纯数据结构
/// </summary>
public class Card : IComparable<Card>
{
    /// <summary>
    /// 花色
    /// </summary>
    public CardSuit Suit { get; private set; }

    /// <summary>
    /// 点数
    /// </summary>
    public CardRank Rank { get; private set; }
    /// <summary>
    /// 显示名称 (例如 "红桃3", "大王")
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 构造函数，属性采用 PascalCase 命名规范
    /// </summary>
    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
        Name = GetCardName(Suit, Rank);
    }

    /// <summary>
    /// 根据花色和牌面大小生成扑克牌名称
    /// </summary>
    /// <param name="suit">花色</param>
    /// <param name="rank">牌面大小</param>
    /// <returns>扑克牌名称，如"红桃A"、"小王"</returns>
    public static string GetCardName(CardSuit suit, CardRank rank)
    {
        // 1. 处理大小王（它们没有花色）
        switch (rank)
        {
            case CardRank.SmallJoker:
                return "小王";
            case CardRank.BigJoker:
                return "大王";
        }

        // 2. 获取花色名称
        string suitName = "emptysuit";
        switch (suit)
        {
            case CardSuit.Club:
                suitName = "梅花";
                break;
            case CardSuit.Diamond:
                suitName = "方块";
                break;
            case CardSuit.Heart:
                suitName = "红桃";
                break;
            case CardSuit.Spade:
                suitName = "黑桃";
                break;
            default:
                UnityEngine.Debug.LogError("错误的卡牌花色信息" + suit);break;
        }

        // 3. 获取牌面名称
        string rankName = "emptyrank";
        switch (rank)
        {
            case CardRank.Rank3:
                rankName = "3";
                break;
            case CardRank.Rank4:
                rankName = "4";
                break;
            case CardRank.Rank5:
                rankName = "5";
                break;
            case CardRank.Rank6:
                rankName = "6";
                break;
            case CardRank.Rank7:
                rankName = "7";
                break;
            case CardRank.Rank8:
                rankName = "8";
                break;
            case CardRank.Rank9:
                rankName = "9";
                break;
            case CardRank.Rank10:
                rankName = "10";
                break;
            case CardRank.RankJ:
                rankName = "J";
                break;
            case CardRank.RankQ:
                rankName = "Q";
                break;
            case CardRank.RankK:
                rankName = "K";
                break;
            case CardRank.RankA:
                rankName = "A";
                break;
            case CardRank.Rank2:
                rankName = "2";
                break;
            default:
                UnityEngine.Debug.LogError("错误的卡牌点数信息" + rank); break;
        }

        return suitName + rankName;
    }

    /// <summary>
    /// 实现 IComparable 接口，方便手牌按照斗地主规则排序
    /// 发牌后调用 List<Card>.Sort() 即可自动排序
    /// </summary>
    public int CompareTo(Card other)
        {
            if (other == null) return 1;

            // 优先按照逻辑大小(Weight)降序排列 (大牌在左)
            int result = other.Rank.CompareTo(this.Rank);

            // 如果逻辑大小相同（比如两张3），则按花色降序排列
            if (result == 0)
            {
                result = other.Suit.CompareTo(this.Suit);
            }

            return result;
        }

        /// <summary>
        /// 重写 ToString，方便在日志中输出关键流程，排查问题
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }