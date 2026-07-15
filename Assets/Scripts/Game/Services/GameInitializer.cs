using DouDiZhu.Logic.Models;
using System;
using System.Collections.Generic;

namespace DouDiZhu.Logic.Services
{
    /// <summary>
    /// 牌局初始化服务（纯逻辑，零Unity依赖）
    /// </summary>
    public static class GameInitializer
    {
        /// <summary>
        /// 创建一局全新的斗地主牌局
        /// </summary>
        /// <param name="playerNames">三个玩家的名字（可选）</param>
        /// <returns>初始化完成的 GameState</returns>
        public static GameState InitializeNewGame(string[] playerNames = null)
        {
            // ========== 1. 获取洗好的完整牌堆 ==========
            List<Card> shuffledDeck = CardDeckUtility.CreateShuffledDeck(); // 一步到位

            // ========== 2. 创建3名玩家 ==========
            GameState state = new GameState();
            string[] defaultNames = { "我", "AI_小蓝", "AI_小红" };

            for (int i = 0; i < 3; i++)
            {
                string name = (playerNames != null && i < playerNames.Length)
                                ? playerNames[i]
                                : defaultNames[i];
                state.Players.Add(new PlayerData(i, name));
            }

            // ========== 3. 轮循发牌（每人17张，留3张底牌） ==========
            int cardIndex = 0;
            for (int round = 0; round < 17; round++)
            {
                for (int playerIdx = 0; playerIdx < 3; playerIdx++)
                {
                    state.Players[playerIdx].AddCards(new List<Card> { shuffledDeck[cardIndex] });
                    cardIndex++;
                }
            }

            // 剩余3张作为底牌（索引 51, 52, 53）
            state.HoleCards.Add(shuffledDeck[51]);
            state.HoleCards.Add(shuffledDeck[52]);
            state.HoleCards.Add(shuffledDeck[53]);

            // ========== 4. 手牌排序 ==========
            foreach (var player in state.Players)
            {
                player.SortHand();
            }

            // ========== 5. 初始化回合状态 ==========
            state.CurrentTurnIndex = 0;
            state.LandlordIndex = -1;
            state.TableCards = new CardGroup(new List<Card> { });
            state.PassCount = 0;
            state.IsGameOver = false;

            return state;
        }

        /// <summary>
        /// 仅用于单元测试：生成一副随机手牌（不经过完整牌局流程）
        /// </summary>
        public static List<Card> GenerateTestHand(int count = 17)
        {
            if (count < 0) count = 0;
            if (count > 54) count = 54;

            List<Card> deck = CardDeckUtility.CreateShuffledDeck();
            List<Card> result = new List<Card>();
            for (int i = 0; i < count; i++)
            {
                result.Add(deck[i]);
            }
            return result;
        }
    }
}