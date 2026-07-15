using System;
using System.Collections.Generic;

namespace DouDiZhu.Logic.Models
{


    /// <summary>
    /// 斗地主全局状态快照（单一数据源，联机版由服务器下发覆盖）
    /// </summary>
    [Serializable]
    public class GameState
    {
        // ========== 玩家数据 ==========
        public List<PlayerData> Players { get; private set; }  // 固定3人，索引0=本地/玩家

        // ========== 牌桌数据 ==========
        public List<Card> HoleCards { get; private set; }      // 3张底牌（未翻开前背面朝上）
        public CardGroup TableCards { get; set; }             // 当前桌面上的牌（上家出的牌）
        public int CurrentTurnIndex { get; set; }              // 当前轮到谁（0,1,2）
        public int LandlordIndex { get; set; }                 // 地主索引（-1表示未确定）
        public int LastPlayedIndex { get; set; }               // 最后出牌的玩家索引（用于回合控制）
        public bool IsGameOver { get; set; }                   // 游戏是否结束
        public string WinnerName { get; set; }                 // 胜利者名字（结算用）

        // ========== 回合状态 ==========
        public int PassCount { get; set; }                     // 连续过牌次数（达到2时清空桌面）

        public GameState()
        {
            Players = new List<PlayerData>();
            HoleCards = new List<Card>();
            TableCards = new CardGroup(new List<Card> { });
            CurrentTurnIndex = 0;
            LandlordIndex = -1;
            LastPlayedIndex = -1;
            IsGameOver = false;
            PassCount = 0;
        }

        /// <summary>
        /// 根据索引获取玩家
        /// </summary>
        public PlayerData GetPlayer(int index)
        {
            if (index < 0 || index >= Players.Count) return null;
            return Players[index];
        }

        /// <summary>
        /// 获取当前回合玩家
        /// </summary>
        public PlayerData GetCurrentPlayer()
        {
            return GetPlayer(CurrentTurnIndex);
        }

        /// <summary>
        /// 深拷贝（联机版反序列化时使用，此处仅留接口，实际由Json序列化实现）
        /// </summary>
        public GameState DeepCopy()
        {
            // 联机版直接用 Newtonsoft.Json 或 Protobuf 序列化/反序列化
            // 此处留空，单机版不需要深拷贝
            throw new NotImplementedException("联机版请使用序列化工具实现深拷贝");
        }
    }
}