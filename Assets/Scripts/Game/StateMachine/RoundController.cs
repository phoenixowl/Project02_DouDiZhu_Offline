using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Services; // 假设你的 CardRule 在这个命名空间
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.VersionControl;

namespace DouDiZhu.Logic.StateMachine
{
    /// <summary>
    /// 回合控制器（状态机核心，纯逻辑）
    /// </summary>
    public class RoundController
    {
        private readonly GameState _state;
        private GameRoundState _currentState;

        public RoundController(GameState state)
        {
            _state = state;
            _currentState = GameRoundState.Idle;
        }

        public GameRoundState CurrentState => _currentState;

        // ========== 1. 开始游戏 ==========
        //TODO : 修改GameInitializedEvent，使其更符合多人模式
        public void StartGame()
        {
            if (_currentState != GameRoundState.Idle)
            {
                EventBus.Emit(new InvalidOperationEvent("游戏已开始，请勿重复初始化"));
                return;
            }

            // 状态切换到叫地主阶段
            _currentState = GameRoundState.Bidding;
            _state.CurrentTurnID = _state.PlayerOrder[0];

            // ---- 构造安全的玩家摘要（不含底牌） ----
            var summaries = new List<PlayerSummary>();
            for (int i = 0; i < _state.PlayerOrder.Count; i++)
            {
                var p = _state.PlayerDict[_state.PlayerOrder[i]];
                summaries.Add(new PlayerSummary(
                    id: _state.PlayerOrder[i],
                    isAI:p.IsAI,
                    name: p.PlayerName,
                    cardCount: p.CardCount,
                    isLocal: true,
                    handCards:new List<Card>(p.HandCards)
                ));
            }

            // ---- 发射安全事件（无底牌泄露） ----
            
            EventBus.Emit(new GameInitializedEvent(summaries));
            EventBus.Emit(new InvalidOperationEvent("注意，多人版本需修改GameInitializedEvent"));
            EventBus.Emit(new TurnChangedEvent(_state.CurrentTurnID));
        }

        // ========== 2. 叫地主 ==========
        public void PlaceBid(int playerID, bool isCalling)
        {
            if (_currentState != GameRoundState.Bidding)
            {
                EventBus.Emit(new InvalidOperationEvent("当前不是叫地主阶段"));
                return;
            }

            if (playerID != _state.CurrentTurnID)
            {
                EventBus.Emit(new InvalidOperationEvent($"未轮到玩家 {playerID} 叫地主"));
                return;
            }

            // 记录叫地主结果
            EventBus.Emit(new BidPlacedEvent(playerID, isCalling));

            if (isCalling)
            {
                // 有人叫地主 -> 直接确定地主（简化版，不实现抢地主）
                ConfirmLandlord(playerID);
                return;
            }

            // 不叫 -> 轮到下一个玩家
            int nextIndex = GetNextPlayerIndex(_state.PlayerOrder.IndexOf(playerID));
            if (nextIndex == 0)
            {
                // 所有人都没叫 -> 强制第一个玩家当地主（或随机，根据需求）
                // 这里简化为强制玩家0当地主
                ConfirmLandlord(_state.PlayerOrder[0]);
            }
            else
            {
                int nextID = _state.GetPlayerByIndex(nextIndex);
                _state.CurrentTurnID = nextID;
                EventBus.Emit(new TurnChangedEvent(nextID));
            }
        }

        // ========== 3. 确认地主（内部方法） ==========
        private void ConfirmLandlord(int landlordID)
        {
            _state.LandlordID = landlordID;
            _state.PlayerDict[landlordID].Identity = PlayerIdentity.Landlord;

            // 底牌加入地主手牌
            var landlord = _state.PlayerDict[landlordID];
            landlord.AddCards(_state.HoleCards);
            landlord.SortHand();

            // 进入出牌阶段
            _currentState = GameRoundState.Playing;
            _state.CurrentTurnID = landlordID;
            _state.LastPlayedID = landlordID;
            _state.TableCards = new CardGroup(new List<Card>());
            _state.PassCount = 0;

            // ---- 安全地公开底牌（此时已合法） ----
            EventBus.Emit(new LandlordConfirmedEvent(
                landlordID,
                new List<Card>(_state.HoleCards) // 传递副本，防止外部修改
            ));
            EventBus.Emit(new TurnChangedEvent(landlordID));
        }

        // ========== 4. 出牌 ==========
        public void PlayCards(int playerID, List<Card> selectedCards)
        {
            // 状态检查
            if (_currentState != GameRoundState.Playing)
            {
                EventBus.Emit(new InvalidOperationEvent("当前不是出牌阶段"));
                return;
            }

            // 回合检查
            if (playerID != _state.CurrentTurnID)
            {
                EventBus.Emit(new InvalidOperationEvent($"未轮到玩家 {playerID} 出牌"));
                return;
            }

            var player = _state.PlayerDict[playerID];

            // ---- 二次校验（严谨的服务器级校验） ----
            // 0. 检查是否有重复卡牌
            if (selectedCards.Count != selectedCards.Distinct().Count())
            {
                EventBus.Emit(new PlayRejectedEvent("不能重复选择同一张牌"));
                return;
            }

            // 1. 检查牌是否都在手牌中
            foreach (var card in selectedCards)
            {
                if (!player.HandCards.Contains(card))
                {
                    EventBus.Emit(new PlayRejectedEvent($"手牌中不存在 {card}"));
                    return;
                }
            }

            //分析牌型
            CardGroup cardGroup = new CardGroup(selectedCards);

            // 2. 检查牌型是否合法
            if (cardGroup.Type == CardType.Invalid)
            {
                EventBus.Emit(new PlayRejectedEvent("非法牌型，无法出牌"));
                return;
            }

            // 3. 检查是否能压过上家（如果是本轮首出，无需压制）
            bool canBeat = CardRule.CanBeat(
                cardGroup,
                _state.TableCards
            );
            if (!canBeat)
            {
                EventBus.Emit(new PlayRejectedEvent("牌型太小，无法压过上家"));
                return;
            }

            // ---- 校验通过，执行出牌 ----
            // 移除手牌
            player.RemoveCards(selectedCards);

            // 更新桌面
            _state.TableCards = cardGroup;
            _state.LastPlayedID = playerID;
            _state.PassCount = 0; // 重置过牌计数

            // 通知 UI 出牌成功
            EventBus.Emit(new CardPlayedEvent(playerID, cardGroup));

            // ---- 检查是否胜利 ----
            if (player.CardCount == 0)
            {
                _currentState = GameRoundState.GameOver;
                bool isLandlordWin = (player.Identity == PlayerIdentity.Landlord);
                EventBus.Emit(new PlayerWinEvent(playerID, isLandlordWin));
                EventBus.Emit(new GameOverEvent(playerID, player.PlayerName));
                return;
            }

            // ---- 切换到下一位玩家 ----
            NextTurn();
        }

        // ========== 5. 过牌（Pass） ==========
        public void Pass(int playerID)
        {
            if (_currentState != GameRoundState.Playing)
            {
                EventBus.Emit(new InvalidOperationEvent("当前不是出牌阶段"));
                return;
            }

            if (playerID != _state.CurrentTurnID)
            {
                EventBus.Emit(new InvalidOperationEvent($"未轮到玩家 {playerID} 过牌"));
                return;
            }

            // 如果是本轮首出（桌面没牌），不能过牌
            if (_state.TableCards.Type == CardType.Invalid)
            {
                EventBus.Emit(new PlayRejectedEvent("你是本轮首出，必须出一张牌"));
                return;
            }

            // 执行过牌
            _state.PassCount++;
            EventBus.Emit(new PassEvent(playerID));

            // 检查是否连续两人过牌 -> 一轮结束，清空桌面
            if (_state.PassCount >= 2)
            {
                // 上一轮最后一个出牌的人获得下一轮首出权
                int lastPlayed = _state.LastPlayedID;
                _state.TableCards = new CardGroup(new List<Card> { });
                _state.PassCount = 0;

                EventBus.Emit(new RoundClearedEvent(lastPlayed));

                // 让最后出牌的人先出
                _state.CurrentTurnID = lastPlayed;
                EventBus.Emit(new TurnChangedEvent(lastPlayed));
                return;
            }

            // 未结束本轮，轮到下一家
            NextTurn();
        }

        // ========== 6. 回合流转（内部工具方法） ==========
        private void NextTurn()
        {
            int next = GetNextPlayerIndex(_state.PlayerOrder.IndexOf(_state.CurrentTurnID));
            _state.CurrentTurnID = _state.GetPlayerByIndex(next);
            EventBus.Emit(new TurnChangedEvent(_state.CurrentTurnID));
        }

        private int GetNextPlayerIndex(int current)
        {
            return (current + 1) % 3; // 0->1, 1->2, 2->0
        }
    }
}