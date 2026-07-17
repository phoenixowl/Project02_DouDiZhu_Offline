using DouDiZhu.Logic.AI;
using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Services;
using DouDiZhu.Logic.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DouDiZhu.Logic.Room
{
    /// <summary>
    /// 游戏会话（一局斗地主的核心流程控制）
    /// </summary>
    public class GameSession
    {
        // ========== 公开事件 ==========
        public event Action<GameState> OnStateUpdated;
        public event Action<string> OnGameLog;
        public event Action OnGameOver;          // 游戏完全结束

        // ========== 内部核心 ==========
        private GameState _gameState;
        private RoundController _roundController;
        private AIPlayerService _aiService;
        private List<int> _playerIds;
        private Dictionary<int, bool> _aiHostingMap;   // 玩家ID -> 是否AI托管
        private bool _eventsSubscribed = false;

        // ========== 回合计时器 ==========
        private float _turnTimeoutSeconds = 30f;
        private float _currentTurnTimer = 0f;
        private bool _isTimerActive = false;

        // ========== AI 延迟调度 ==========
        private float _aiDelaySeconds = 0.8f;
        private float _scheduledAITime = -1f;
        private int _scheduledAIPlayerId = -1;
        private bool _isAIScheduled = false;

        public GameState GameState => _gameState;

        // ============================================================
        // 构造函数
        // ============================================================

        public GameSession(List<int> playerIds, Dictionary<int, bool> aiHostingMap, float aiDelay = 0.8f)
        {
            _playerIds = playerIds;
            _aiHostingMap = new Dictionary<int, bool>(aiHostingMap);
            _aiDelaySeconds = Mathf.Max(0.1f, aiDelay);

            // 1. 初始化牌局
            _gameState = GameInitializer.InitializeNewGame(playerIds);

            // 2. 同步 AI 状态到 PlayerData
            SyncPlayerDataAIStatus();

            // 3. 初始化核心
            _roundController = new RoundController(_gameState);
            _aiService = new AIPlayerService(_gameState, 0.8f);

            // 4. 订阅事件
            SubscribeEvents();

            Log("GameSession 创建成功");
        }

        private void SyncPlayerDataAIStatus()
        {
            for (int i = 0; i < _playerIds.Count; i++)
            {
                int pid = _playerIds[i];
                bool isAI = _aiHostingMap.TryGetValue(pid, out bool value) && value;
                _gameState.PlayerDict[pid].SetAIHosting(isAI);
            }
        }

        // ============================================================
        // 启动游戏
        // ============================================================

        public void Start()
        {
            _roundController.StartGame();
        }

        // ============================================================
        // 每帧更新（由 RoomManager 转发）
        // ============================================================

        public void Update(float deltaTime)
        {
            // 1. 更新超时计时器
            UpdateTimer(deltaTime);

            // 2. 检查 AI 调度
            if (!_isAIScheduled) return;

            _scheduledAITime -= deltaTime;
            if (_scheduledAITime <= 0f)
            {
                ExecuteScheduledAIAction();
            }
        }

        // ============================================================
        // 事件订阅
        // ============================================================

        private void SubscribeEvents()
        {
            if (_eventsSubscribed) return;

            // 上行请求
            EventBus.Subscribe<RequestPlayCardEvent>(OnRequestPlayCard);
            EventBus.Subscribe<RequestAIHostEvent>(OnRequestAIHost);
            EventBus.Subscribe<RequestPassEvent>(OnRequestPass);
            EventBus.Subscribe<RequestBidEvent>(OnRequestBid);
            EventBus.Subscribe<RequestStartGameEvent>(OnRequestStartGame);
            EventBus.Subscribe<RequestHintEvent>(OnRequestHintCard);

            // 下行领域事件
            EventBus.Subscribe<GameInitializedEvent>(OnGameInitialized);
            EventBus.Subscribe<BidPlacedEvent>(OnBidPlaced);
            EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
            EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<PassEvent>(OnPass);
            EventBus.Subscribe<RoundClearedEvent>(OnRoundCleared);
            EventBus.Subscribe<PlayerWinEvent>(OnPlayerWin);
            EventBus.Subscribe<GameOverEvent>(OnGameOverEvent);
            EventBus.Subscribe<PlayRejectedEvent>(OnPlayRejected);
            EventBus.Subscribe<InvalidOperationEvent>(OnInvalidOperation);

            _eventsSubscribed = true;
            Log("已订阅所有事件");
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed) return;

            EventBus.Unsubscribe<RequestPlayCardEvent>(OnRequestPlayCard);
            EventBus.Unsubscribe<RequestAIHostEvent>(OnRequestAIHost);
            EventBus.Unsubscribe<RequestPassEvent>(OnRequestPass);
            EventBus.Unsubscribe<RequestBidEvent>(OnRequestBid);
            EventBus.Unsubscribe<RequestStartGameEvent>(OnRequestStartGame);
            EventBus.Unsubscribe<RequestHintEvent>(OnRequestHintCard);

            EventBus.Unsubscribe<GameInitializedEvent>(OnGameInitialized);
            EventBus.Unsubscribe<BidPlacedEvent>(OnBidPlaced);
            EventBus.Unsubscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
            EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<PassEvent>(OnPass);
            EventBus.Unsubscribe<RoundClearedEvent>(OnRoundCleared);
            EventBus.Unsubscribe<PlayerWinEvent>(OnPlayerWin);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOverEvent);
            EventBus.Unsubscribe<PlayRejectedEvent>(OnPlayRejected);
            EventBus.Unsubscribe<InvalidOperationEvent>(OnInvalidOperation);

            _eventsSubscribed = false;
        }

        // ============================================================
        // 上行请求事件回调
        // ============================================================

        private void OnRequestStartGame(RequestStartGameEvent evt)
        {
            if (_gameState.IsGameOver)
            {
                Log("游戏已结束，无法重新开始");
                return;
            }
            _roundController.StartGame();
            Log("游戏已启动");
        }

        private void OnRequestBid(RequestBidEvent evt)
        {
            if (_gameState.IsGameOver)
            {
                Log($"游戏已结束，玩家 {evt.PlayerId} 无法叫地主");
                return;
            }
            _roundController.PlaceBid(evt.PlayerId, evt.IsCalling);
        }

        private void OnRequestPlayCard(RequestPlayCardEvent evt)
        {
            if (_gameState.IsGameOver)
            {
                Log($"游戏已结束，玩家 {evt.PlayerId} 无法出牌");
                return;
            }
            _roundController.PlayCards(evt.PlayerId, evt.SelectedCards);
        }

        private void OnRequestPass(RequestPassEvent evt)
        {
            if (_gameState.IsGameOver)
            {
                Log($"游戏已结束，玩家 {evt.PlayerId} 无法过牌");
                return;
            }
            _roundController.Pass(evt.PlayerId);
        }

        private void OnRequestHintCard(RequestHintEvent evt)
        {
            if (_gameState.IsGameOver)
            {
                Log($"游戏已结束，玩家 {evt.PlayerId} 不需要提示");
                return;
            }

            if (_roundController.CurrentState == GameRoundState.Bidding)
            {
                Log($"当前为叫地主阶段，玩家 {evt.PlayerId} 不需要提示");
                return;
            }

            int playerId = evt.PlayerId;
            Log($"正在为玩家 {evt.PlayerId} 获取提示");
            AIDecision decision = _aiService.DecideAction(playerId);
            EventBus.Emit(new HintEvent(playerId, decision));
        }

        private void OnRequestAIHost(RequestAIHostEvent evt)
        {
            // 切换托管
            ToggleAIHosting(evt.PlayerId);
            // 广播托管状态变化（供 UI 更新）
            EventBus.Emit(new AIHostEvent(evt.PlayerId, _aiHostingMap[evt.PlayerId]));
        }

        // ============================================================
        // 下行领域事件回调
        // ============================================================

        private void OnGameInitialized(GameInitializedEvent evt)
        {
            Log($"牌局初始化完成");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnBidPlaced(BidPlacedEvent evt)
        {
            Log($"{GetPlayerName(evt.PlayerID)} {(evt.IsCalling ? "叫地主" : "不叫")}");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnLandlordConfirmed(LandlordConfirmedEvent evt)
        {
            Log($"{GetPlayerName(evt.LandlordID)} 成为地主！");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnTurnChanged(TurnChangedEvent evt)
        {
            int playerId = evt.PlayerID;
            bool isAI = IsPlayerAI(playerId);

            Log($">>> 轮到 {GetPlayerName(playerId)} {(isAI ? "[AI]" : "[玩家]")}");

            if (!_gameState.IsGameOver)
            {
                ResetTimer();
                if (isAI)
                    ScheduleAIAction(playerId);
                else
                    CancelScheduledAIAction();
            }
            else
            {
                StopTimer();
                CancelScheduledAIAction();
            }

            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            string cards = string.Join(", ", evt.CardGroup.Cards.Select(c => c.ToString()));
            Log($"{GetPlayerName(evt.PlayerID)} 出牌: {cards} [{evt.CardGroup.Type}]");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnPass(PassEvent evt)
        {
            Log($"{GetPlayerName(evt.PlayerID)} 过牌");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnRoundCleared(RoundClearedEvent evt)
        {
            Log($"本轮结束，{GetPlayerName(evt.LastPlayedID)} 获得出牌权");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnPlayerWin(PlayerWinEvent evt)
        {
            Log($"{GetPlayerName(evt.WinnerID)} 出完所有手牌！");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnGameOverEvent(GameOverEvent evt)
        {
            _gameState.IsGameOver = true;
            Log($"═══════════════════════════════");
            Log($"🏁 游戏结束！胜利者: {evt.WinnerName}");
            Log($"═══════════════════════════════");
            StopTimer();
            CancelScheduledAIAction();
            OnStateUpdated?.Invoke(_gameState);
            OnGameOver?.Invoke();   // 通知 RoomManager
        }

        private void OnPlayRejected(PlayRejectedEvent evt)
        {
            Log($"❌ {evt.Reason}");
        }

        private void OnInvalidOperation(InvalidOperationEvent evt)
        {
            Log($"⚠️ {evt.Message}");
        }

        // ============================================================
        // AI 托管切换（内部）
        // ============================================================

        public void ToggleAIHosting(int playerId)
        {
            if (!_aiHostingMap.ContainsKey(playerId))
            {
                Log($"错误：玩家 {playerId} 不在游戏中");
                return;
            }

            bool newState = !_aiHostingMap[playerId];
            _aiHostingMap[playerId] = newState;
            _gameState.PlayerDict[playerId].SetAIHosting(newState);

            Log($"玩家 {playerId} AI托管: {(newState ? "开启" : "关闭")}");

            // 如果切换后正好轮到该玩家且为AI，触发调度
            if (newState && !_gameState.IsGameOver && playerId == _gameState.CurrentTurnID)
            {
                ScheduleAIAction(playerId);
            }
            else if (!newState && !_gameState.IsGameOver && playerId == _gameState.CurrentTurnID)
            {
                CancelScheduledAIAction();
                ResetTimer(); // 手动操作时开始计时
            }

            OnStateUpdated?.Invoke(_gameState);
        }

        public bool IsPlayerAI(int playerId)
        {
            return _aiHostingMap.TryGetValue(playerId, out bool isAI) && isAI;
        }

        // ============================================================
        // AI 调度
        // ============================================================

        public void SetAIDelay(float seconds)
        {
            _aiDelaySeconds = Mathf.Max(0.1f, seconds);
        }

        private void ScheduleAIAction(int playerId)
        {
            if (_gameState.IsGameOver) return;
            if (!IsPlayerAI(playerId))
            {
                CancelScheduledAIAction();
                return;
            }
            if (!_gameState.PlayerDict.ContainsKey(playerId)) return;

            _scheduledAITime = _aiDelaySeconds;
            _scheduledAIPlayerId = playerId;
            _isAIScheduled = true;

            Log($"[AI调度] 玩家 {playerId} 将在 {_aiDelaySeconds:F1}s 后操作");
        }

        private void CancelScheduledAIAction()
        {
            if (_isAIScheduled)
            {
                Log($"[AI调度] 取消玩家 {_scheduledAIPlayerId} 的待执行动作");
            }
            _isAIScheduled = false;
            _scheduledAITime = -1f;
            _scheduledAIPlayerId = -1;
        }

        private void ExecuteScheduledAIAction()
        {
            _isAIScheduled = false;
            int playerId = _scheduledAIPlayerId;
            _scheduledAIPlayerId = -1;

            if (_gameState.IsGameOver) return;

            if (playerId != _gameState.CurrentTurnID)
            {
                Log($"[AI调度] 玩家 {playerId} 已过时，当前轮到 {_gameState.CurrentTurnID}");
                return;
            }

            if (!IsPlayerAI(playerId))
            {
                Log($"[AI调度] 玩家 {playerId} 已切换为手动");
                return;
            }

            ExecuteAIForPlayer(playerId);
        }

        private bool ExecuteAIForPlayer(int playerId)
        {
            if (_roundController.CurrentState == GameRoundState.Bidding)
            {
                bool call = _aiService.ShouldCallLandlord(playerId);
                _roundController.PlaceBid(playerId, call);
                Log($"[AI] {GetPlayerName(playerId)} 叫地主: {(call ? "是" : "否")}");
                return true;
            }
            else if (_roundController.CurrentState == GameRoundState.Playing)
            {
                var decision = _aiService.DecideAction(playerId);
                if (decision.ShouldPlay)
                {
                    _roundController.PlayCards(playerId, decision.SelectedCards);
                    Log($"[AI] {GetPlayerName(playerId)} 出 {decision.SelectedCards.Count} 张牌");
                }
                else
                {
                    _roundController.Pass(playerId);
                    Log($"[AI] {GetPlayerName(playerId)} 过牌");
                }
                return true;
            }
            return false;
        }

        // ============================================================
        // 回合计时器
        // ============================================================

        private void ResetTimer()
        {
            _currentTurnTimer = 0f;
            _isTimerActive = true;
        }

        private void StopTimer()
        {
            _isTimerActive = false;
            _currentTurnTimer = 0f;
        }

        private void UpdateTimer(float deltaTime)
        {
            if (!_isTimerActive || _gameState.IsGameOver) return;

            _currentTurnTimer += deltaTime;
            if (_currentTurnTimer >= _turnTimeoutSeconds)
            {
                StopTimer();
                HandleTimeout();
            }
        }

        private void HandleTimeout()
        {
            int playerId = _gameState.CurrentTurnID;
            Log($"玩家 {playerId} 超时未操作，AI 自动托管...");
            CancelScheduledAIAction();
            ExecuteAIForPlayer(playerId);
        }

        // ============================================================
        // 工具
        // ============================================================

        private string GetPlayerName(int playerId)
        {
            return _gameState.PlayerDict.ContainsKey(playerId)
                ? _gameState.PlayerDict[playerId].PlayerName
                : "未知玩家";
        }

        private void Log(string msg) => OnGameLog?.Invoke($"[Game] {msg}");

        // ============================================================
        // 清理
        // ============================================================

        public void Dispose()
        {
            UnsubscribeEvents();
            StopTimer();
            CancelScheduledAIAction();
            Log("GameSession 已销毁");
        }
    }
}