using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DouDiZhu.Logic.StateMachine
{
    /// <summary>
    /// 回合控制器（状态机核心，纯逻辑）
    /// </summary>
    public class RoundController
    {
        private readonly GameState _state;
        private GameRoundState _currentState;

        // ========== 抢地主状态 ==========
        private List<int> _biddingCandidates;        // 首轮抢地主的玩家列表（按顺序）
        private int _currentConfirmIndex;            // 确认轮当前处理到第几个候选
        private bool _isConfirmPhase;                // 是否进入确认轮

        public RoundController(GameState state)
        {
            _state = state;
            _currentState = GameRoundState.Idle;
            _biddingCandidates = new List<int>();
            _currentConfirmIndex = 0;
            _isConfirmPhase = false;
        }

        public GameRoundState CurrentState => _currentState;

        // ========== 1. 开始游戏 ==========
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

            // 重置抢地主状态
            _biddingCandidates.Clear();
            _currentConfirmIndex = 0;
            _isConfirmPhase = false;

            // ---- 构造安全的玩家摘要（不含底牌） ----
            var summaries = new List<PlayerSummary>();
            for (int i = 0; i < _state.PlayerOrder.Count; i++)
            {
                var p = _state.PlayerDict[_state.PlayerOrder[i]];
                summaries.Add(new PlayerSummary(
                    id: _state.PlayerOrder[i],
                    isAI: p.IsAI,
                    name: p.PlayerName,
                    cardCount: p.CardCount,
                    isLocal: true,
                    handCards: new List<Card>(p.HandCards)
                ));
            }

            EventBus.Emit(new GameInitializedEvent(summaries));
            EventBus.Emit(new InvalidOperationEvent("注意，多人版本需修改GameInitializedEvent"));
            EventBus.Emit(new TurnChangedEvent(_state.CurrentTurnID));
        }

        // ========== 2. 叫地主（含抢地主完整流程） ==========
        public void PlaceBid(int playerID, bool isCalling)
        {
            if (_currentState != GameRoundState.Bidding)
            {
                EventBus.Emit(new InvalidOperationEvent("当前不是叫地主阶段"));
                return;
            }

            if (playerID != _state.CurrentTurnID)
            {
                EventBus.Emit(new InvalidOperationEvent($"未轮到玩家 {playerID} 操作"));
                return;
            }

            // ---- 根据当前阶段分发处理 ----
            if (!_isConfirmPhase)
            {
                // ===== 阶段一：首轮抢地主 =====
                HandleFirstRoundBid(playerID, isCalling);
            }
            else
            {
                // ===== 阶段二：确认轮 =====
                HandleConfirmRoundBid(playerID, isCalling);
            }
        }

        // ============================================================
        // 阶段一：首轮抢地主
        // ============================================================
        private void HandleFirstRoundBid(int playerID, bool isCalling)
        {
            // 记录本次决策
            EventBus.Emit(new BidPlacedEvent(playerID, isCalling));

            if (isCalling)
            {
                // 抢地主：加入候选列表
                _biddingCandidates.Add(playerID);
                Log($"[抢地主] 玩家 {playerID} 抢地主！");
            }
            else
            {
                Log($"[抢地主] 玩家 {playerID} 不抢");
            }

            // 轮到下一个玩家
            int currentIndex = _state.PlayerOrder.IndexOf(playerID);
            int nextIndex = GetNextPlayerIndex(currentIndex);

            if (nextIndex == 0)
            {
                // ----- 所有玩家都已表态，进入决策阶段 -----
                if (_biddingCandidates.Count == 0)
                {
                    // 无人抢地主 -> 玩家0（索引0）自动成为地主
                    Log("[抢地主] 无人抢地主，玩家0自动成为地主");
                    ConfirmLandlord(_state.PlayerOrder[0]);
                }
                else if (_biddingCandidates.Count == 1)
                {
                    // 只有一人抢地主 -> 直接确认
                    Log($"[抢地主] 仅玩家 {_biddingCandidates[0]} 抢地主，直接确认");
                    ConfirmLandlord(_biddingCandidates[0]);
                }
                else
                {
                    // 多人抢地主 -> 进入确认轮
                    Log($"[抢地主] {_biddingCandidates.Count} 人抢地主，进入确认轮");
                    _isConfirmPhase = true;
                    _currentConfirmIndex = 0;

                    // 从第一个抢地主的人开始询问
                    int firstCandidate = _biddingCandidates[0];
                    _state.CurrentTurnID = firstCandidate;
                    EventBus.Emit(new TurnChangedEvent(firstCandidate));

                    // 额外发射一个事件表示进入确认阶段（UI可据此更新提示文字）
                    EventBus.Emit(new BiddingConfirmPhaseEvent());
                }
            }
            else
            {
                // 继续下一家
                int nextID = _state.GetPlayerByIndex(nextIndex);
                _state.CurrentTurnID = nextID;
                EventBus.Emit(new TurnChangedEvent(nextID));
            }
        }

        // ============================================================
        // 阶段二：确认轮（依次确认抢地主的人是否最终愿意成为地主）
        // ============================================================
        private void HandleConfirmRoundBid(int playerID, bool isCalling)
        {
            // 检查当前玩家是否在候选列表中且是当前索引
            if (_currentConfirmIndex >= _biddingCandidates.Count)
            {
                EventBus.Emit(new InvalidOperationEvent("确认流程已结束"));
                return;
            }

            int expectedPlayer = _biddingCandidates[_currentConfirmIndex];
            if (playerID != expectedPlayer)
            {
                EventBus.Emit(new InvalidOperationEvent($"当前应轮到玩家 {expectedPlayer} 确认"));
                return;
            }

            // 记录确认决策（用新事件或复用 BidPlacedEvent，用额外字段区分）
            EventBus.Emit(new BidPlacedEvent(playerID, isCalling, isConfirmRound: true));

            if (isCalling)
            {
                // 确认成为地主
                Log($"[确认轮] 玩家 {playerID} 确认抢地主，成为地主！");
                ConfirmLandlord(playerID);
                return;
            }
            else
            {
                // 放弃确认
                Log($"[确认轮] 玩家 {playerID} 放弃确认");

                // 移动到下一个候选
                _currentConfirmIndex++;

                // 检查是否所有候选都放弃了
                if (_currentConfirmIndex >= _biddingCandidates.Count)
                {
                    // 所有候选都放弃 -> 最后一个抢地主的人自动成为地主
                    int lastCandidate = _biddingCandidates.Last();
                    Log($"[确认轮] 所有候选放弃，最后一个抢地主的人 {lastCandidate} 自动成为地主");
                    ConfirmLandlord(lastCandidate);
                    return;
                }

                // 询问下一个候选
                int nextCandidate = _biddingCandidates[_currentConfirmIndex];
                _state.CurrentTurnID = nextCandidate;
                EventBus.Emit(new TurnChangedEvent(nextCandidate));
            }
        }

        // ============================================================
        // 3. 确认地主（内部方法）
        // ============================================================
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

            // 清空抢地主状态
            _biddingCandidates.Clear();
            _currentConfirmIndex = 0;
            _isConfirmPhase = false;

            // ---- 安全地公开底牌 ----
            EventBus.Emit(new LandlordConfirmedEvent(
                landlordID,
                new List<Card>(_state.HoleCards)
            ));
            EventBus.Emit(new TurnChangedEvent(landlordID));
        }

        // ========== 4. 出牌 ==========
        public void PlayCards(int playerID, List<Card> selectedCards)
        {
            if (_currentState != GameRoundState.Playing)
            {
                EventBus.Emit(new InvalidOperationEvent("当前不是出牌阶段"));
                return;
            }

            if (playerID != _state.CurrentTurnID)
            {
                EventBus.Emit(new InvalidOperationEvent($"未轮到玩家 {playerID} 出牌"));
                return;
            }

            var player = _state.PlayerDict[playerID];

            // ---- 二次校验 ----
            if (selectedCards.Count != selectedCards.Distinct().Count())
            {
                EventBus.Emit(new PlayRejectedEvent("不能重复选择同一张牌"));
                return;
            }

            foreach (var card in selectedCards)
            {
                if (!player.HandCards.Contains(card))
                {
                    EventBus.Emit(new PlayRejectedEvent($"手牌中不存在 {card}"));
                    return;
                }
            }

            CardGroup cardGroup = new CardGroup(selectedCards);

            if (cardGroup.Type == CardType.Invalid)
            {
                EventBus.Emit(new PlayRejectedEvent("非法牌型，无法出牌"));
                return;
            }

            bool canBeat = CardRule.CanBeat(cardGroup, _state.TableCards);
            if (!canBeat)
            {
                EventBus.Emit(new PlayRejectedEvent("牌型太小，无法压过上家"));
                return;
            }

            // ---- 执行出牌 ----
            player.RemoveCards(selectedCards);
            _state.TableCards = cardGroup;
            _state.LastPlayedID = playerID;
            _state.PassCount = 0;

            EventBus.Emit(new CardPlayedEvent(playerID, cardGroup));

            if (player.CardCount == 0)
            {
                _currentState = GameRoundState.GameOver;
                bool isLandlordWin = (player.Identity == PlayerIdentity.Landlord);
                EventBus.Emit(new PlayerWinEvent(playerID, isLandlordWin));
                EventBus.Emit(new GameOverEvent(playerID, player.PlayerName, player.Identity == PlayerIdentity.Landlord));
                return;
            }

            NextTurn();
        }

        // ========== 5. 过牌 ==========
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

            if (_state.TableCards.Type == CardType.Invalid)
            {
                EventBus.Emit(new PlayRejectedEvent("你是本轮首出，必须出一张牌"));
                return;
            }

            _state.PassCount++;
            EventBus.Emit(new PassEvent(playerID));

            if (_state.PassCount >= 2)
            {
                int lastPlayed = _state.LastPlayedID;
                _state.TableCards = new CardGroup(new List<Card>());
                _state.PassCount = 0;

                EventBus.Emit(new RoundClearedEvent(lastPlayed));

                _state.CurrentTurnID = lastPlayed;
                EventBus.Emit(new TurnChangedEvent(lastPlayed));
                return;
            }

            NextTurn();
        }

        // ========== 6. 回合流转 ==========
        private void NextTurn()
        {
            int next = GetNextPlayerIndex(_state.PlayerOrder.IndexOf(_state.CurrentTurnID));
            _state.CurrentTurnID = _state.GetPlayerByIndex(next);
            EventBus.Emit(new TurnChangedEvent(_state.CurrentTurnID));
        }

        private int GetNextPlayerIndex(int current)
        {
            return (current + 1) % 3;
        }

        // ========== 7. 日志辅助 ==========
        private void Log(string message)
        {
            EventBus.Emit(new GameLogEvent(message));
        }
    }
}