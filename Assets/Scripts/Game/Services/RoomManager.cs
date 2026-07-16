using DouDiZhu.Logic.AI;
using DouDiZhu.Logic.Commands;
using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Services;
using DouDiZhu.Logic.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DouDiZhu.Logic.Room
{
    /// <summary>
    /// 房间管理器（单一入口：所有外部输入通过 RequestXxxEvent）
    /// </summary>
    public class RoomManager
    {
        // ========== 公开属性 ==========
        public string RoomId { get; private set; }
        public int RoomOwnerId { get; private set; }
        public IReadOnlyList<int> PlayerIds => _playerIds.AsReadOnly();
        public GameState GameState => _gameState;
        public GameRoundState CurrentState => _roundController?.CurrentState ?? GameRoundState.Idle;
        public bool IsGameOver => _gameState?.IsGameOver ?? true;

        // ========== 公开事件（下行通知） ==========
        public event Action<GameState> OnStateUpdated;
        public event Action<string> OnGameLog;

        // ========== 内部字段 ==========
        private GameState _gameState;
        private RoundController _roundController;
        private AIPlayerService _aiService;
        private List<int> _playerIds;
        private Dictionary<int, bool> _aiHostingMap;
        private bool _eventsSubscribed = false;
        private bool _pendingAIAction = false;

        // ============================================================
        // 构造函数
        // ============================================================

        public RoomManager(string roomId, int ownerId, List<int> playerIds, Dictionary<int, bool> aiHostingMap = null)
        {
            if (playerIds == null || playerIds.Count != 3)
                throw new ArgumentException("斗地主必须恰好3名玩家");

            RoomId = roomId;
            RoomOwnerId = ownerId;
            _playerIds = new List<int>(playerIds);

            // 初始化AI托管映射（默认为false）
            _aiHostingMap = new Dictionary<int, bool>();
            foreach (var pid in _playerIds)
            {
                _aiHostingMap[pid] = aiHostingMap?.TryGetValue(pid, out bool v) == true ? v : false;
            }

            // 1. 注册映射
            //已经弃用

            // 2. 初始化牌局
            _gameState = GameInitializer.InitializeNewGame(playerIds);

            // 3. 同步AI状态到PlayerData
            SyncPlayerDataAIStatus();

            // 4. 初始化核心
            _roundController = new RoundController(_gameState);
            _aiService = new AIPlayerService(_gameState, 0.3f);

            // 5. 订阅事件
            SubscribeEvents();

            Log($"房间 [{RoomId}] 创建成功");
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
        // 事件订阅（统一订阅所有上行请求 + 下行领域事件）
        // ============================================================

        private void SubscribeEvents()
        {
            if (_eventsSubscribed) return;

            // ===== 上行请求事件（外部输入） =====
            EventBus.Subscribe<RequestPlayCardEvent>(OnRequestPlayCard);
            EventBus.Subscribe<RequestPassEvent>(OnRequestPass);
            EventBus.Subscribe<RequestBidEvent>(OnRequestBid);
            EventBus.Subscribe<RequestStartGameEvent>(OnRequestStartGame);

            // ===== 下行领域事件（内部通知） =====
            EventBus.Subscribe<GameInitializedEvent>(OnGameInitialized);
            EventBus.Subscribe<BidPlacedEvent>(OnBidPlaced);
            EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
            EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<PassEvent>(OnPass);
            EventBus.Subscribe<RoundClearedEvent>(OnRoundCleared);
            EventBus.Subscribe<PlayerWinEvent>(OnPlayerWin);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<PlayRejectedEvent>(OnPlayRejected);
            EventBus.Subscribe<InvalidOperationEvent>(OnInvalidOperation);

            _eventsSubscribed = true;
            Log("已订阅所有事件");
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed) return;

            EventBus.Subscribe<RequestPlayCardEvent>(OnRequestPlayCard);
            EventBus.Subscribe<RequestPassEvent>(OnRequestPass);
            EventBus.Subscribe<RequestBidEvent>(OnRequestBid);
            EventBus.Subscribe<RequestStartGameEvent>(OnRequestStartGame);

            EventBus.Subscribe<GameInitializedEvent>(OnGameInitialized);
            EventBus.Subscribe<BidPlacedEvent>(OnBidPlaced);
            EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
            EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<PassEvent>(OnPass);
            EventBus.Subscribe<RoundClearedEvent>(OnRoundCleared);
            EventBus.Subscribe<PlayerWinEvent>(OnPlayerWin);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<PlayRejectedEvent>(OnPlayRejected);
            EventBus.Subscribe<InvalidOperationEvent>(OnInvalidOperation);

            _eventsSubscribed = false;
        }

        // ============================================================
        // 上行请求事件回调（统一入口：外部→RoomManager）
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

        // ============================================================
        // 下行领域事件回调（内部通知→外部）
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

            if (isAI && !_gameState.IsGameOver)
            {
                _pendingAIAction = true;
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

        private void OnGameOver(GameOverEvent evt)
        {
            _gameState.IsGameOver = true;
            Log($"═══════════════════════════════");
            Log($" 游戏结束！胜利者: {evt.WinnerName}");
            Log($"═══════════════════════════════");
            OnStateUpdated?.Invoke(_gameState);
        }

        private void OnPlayRejected(PlayRejectedEvent evt)
        {
            Log($" {evt.Reason}");
        }

        private void OnInvalidOperation(InvalidOperationEvent evt)
        {
            Log($" {evt.Message}");
        }

        // ============================================================
        // AI 驱动（由外部定时器调用）
        // ============================================================

        public bool HasPendingAIAction => _pendingAIAction && !_gameState.IsGameOver;

        public bool ExecuteAIAction()
        {
            if (!HasPendingAIAction) return false;

            _pendingAIAction = false;

            int playerId = _gameState.CurrentTurnID;

            if (!IsPlayerAI(playerId)) return false;

            // ===== AI 直接调用 RoundController（不走事件层） =====
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
        // AI 托管切换（外部调用）
        // ============================================================

        public void ToggleAIHosting(int playerId)
        {
            if (!_aiHostingMap.ContainsKey(playerId))
            {
                Log($"错误：玩家 {playerId} 不在房间中");
                return;
            }

            bool newState = !_aiHostingMap[playerId];
            _aiHostingMap[playerId] = newState;

            _gameState.PlayerDict[playerId].SetAIHosting(newState);

            Log($"玩家 {playerId} AI托管: {(newState ? "开启" : "关闭")}");

            if (newState && !_gameState.IsGameOver)
            {
                int currentSeat = _gameState.CurrentTurnID;
                if (currentSeat == playerId)
                {
                    _pendingAIAction = true;
                }
            }

            OnStateUpdated?.Invoke(_gameState);
        }

        public bool IsPlayerAI(int playerId)
        {
            return _aiHostingMap.TryGetValue(playerId, out bool isAI) && isAI;
        }

        // ============================================================
        // 工具方法
        // ============================================================

        private string GetPlayerName(int playerID)
        {
            return _gameState.PlayerDict.ContainsKey(playerID)? _gameState.PlayerDict[playerID].PlayerName : "未知玩家";
        }

        private void Log(string message)
        {
            OnGameLog?.Invoke($"[Room:{RoomId}] {message}");
        }

        // ============================================================
        // 清理
        // ============================================================

        public void Dispose()
        {
            UnsubscribeEvents();
            Log($"房间 [{RoomId}] 已销毁");
        }
    }
}