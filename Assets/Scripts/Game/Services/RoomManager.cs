using System;
using System.Collections.Generic;
using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;

namespace DouDiZhu.Logic.Room
{
    /// <summary>
    /// 房间状态
    /// </summary>
    public enum RoomState
    {
        Lobby,      // 等待玩家准备
        Playing,    // 游戏中
        GameOver    // 游戏结束
    }

    /// <summary>
    /// 房间管理器（负责房间生命周期、玩家准备、游戏会话创建）
    /// </summary>
    public class RoomManager
    {
        // ========== 公开属性 ==========
        public string RoomId { get; private set; }
        public int RoomOwnerId { get; private set; }
        public IReadOnlyList<int> PlayerIds => _playerIds.AsReadOnly();
        public RoomState CurrentRoomState { get; private set; } = RoomState.Lobby;
        public GameState GameState => _gameSession.GameState;

        // ========== 公开事件 ==========
        public event Action<RoomState> OnRoomStateChanged;
        public event Action<GameState> OnGameStateUpdated;
        public event Action<string> OnGameLog;

        // ========== 内部字段 ==========
        private List<int> _playerIds;
        private Dictionary<int, bool> _playerReadyStatus;   // 玩家准备状态
        private Dictionary<int, bool> _aiHostingMap;        // 玩家AI托管状态（初始）
        private GameSession _gameSession;

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

            // 初始化准备状态（房主默认准备）
            _playerReadyStatus = new Dictionary<int, bool>();
            foreach (var pid in _playerIds)
            {
                _playerReadyStatus[pid] = (pid == ownerId);
            }

            // 初始化AI托管映射（默认false）
            _aiHostingMap = new Dictionary<int, bool>();
            foreach (var pid in _playerIds)
            {
                _aiHostingMap[pid] = aiHostingMap?.TryGetValue(pid, out bool v) == true ? v : false;
            }

            // 不立刻订阅事件，游戏会话会自行订阅
            Log($"房间 [{RoomId}] 创建成功，等待玩家准备...");
        }

        // ============================================================
        // 准备功能
        // ============================================================

        public void ToggleReady(int playerId)
        {
            if (!_playerReadyStatus.ContainsKey(playerId))
            {
                Log($"错误：玩家 {playerId} 不在房间中");
                return;
            }

            if (CurrentRoomState != RoomState.Lobby)
            {
                Log($"错误：游戏已开始，无法切换准备状态");
                return;
            }

            _playerReadyStatus[playerId] = !_playerReadyStatus[playerId];
            Log($"玩家 {playerId} {(IsPlayerReady(playerId) ? "准备就绪" : "取消准备")}");

            if (AllPlayersReady())
            {
                Log("所有玩家已准备，游戏即将开始...");
                StartGame();
            }
        }

        public bool IsPlayerReady(int playerId)
        {
            return _playerReadyStatus.TryGetValue(playerId, out bool ready) && ready;
        }

        private bool AllPlayersReady()
        {
            foreach (var pid in _playerIds)
            {
                if (!IsPlayerReady(pid)) return false;
            }
            return true;
        }

        // ============================================================
        // 启动游戏
        // ============================================================

        private void StartGame()
        {
            if (CurrentRoomState != RoomState.Lobby) return;
            if (!AllPlayersReady()) return;

            // 1. 创建游戏会话
            _gameSession = new GameSession(_playerIds, _aiHostingMap, 0.8f);
            _gameSession.OnGameLog += (msg) => Log($"[游戏] {msg}");
            _gameSession.OnStateUpdated += (state) =>
            {
                // 更新本地缓存（可选）
                OnGameStateUpdated?.Invoke(state);
            };
            _gameSession.OnGameOver += OnGameOver;

            // 2. 启动游戏
            _gameSession.Start();

            // 3. 更新房间状态
            CurrentRoomState = RoomState.Playing;
            OnRoomStateChanged?.Invoke(CurrentRoomState);

            Log("游戏已开始");
        }

        // ============================================================
        // 游戏结束回调
        // ============================================================

        private void OnGameOver()
        {
            CurrentRoomState = RoomState.GameOver;
            OnRoomStateChanged?.Invoke(CurrentRoomState);

            // 可选：自动重置准备状态，允许下一局
            // ResetReadyStatus();

            Log("游戏结束，回到房间大厅");
        }

        // ============================================================
        // 外部操作转发（UI 或网络）
        // ============================================================

        public void Update(float deltaTime)
        {
            _gameSession?.Update(deltaTime);
        }

        public void ToggleAIHosting(int playerId)
        {
            if (!_aiHostingMap.ContainsKey(playerId)) return;

            // 先在房间层更新映射（以便重新创建会话时保留）
            _aiHostingMap[playerId] = !_aiHostingMap[playerId];

            // 如果游戏会话存在，转发
            _gameSession?.ToggleAIHosting(playerId);
        }

        public bool IsPlayerAI(int playerId)
        {
            // 优先使用游戏会话的状态（实时），否则使用房间映射
            return _gameSession?.IsPlayerAI(playerId) ?? _aiHostingMap.TryGetValue(playerId, out bool isAI) && isAI;
        }

        // ============================================================
        // 重置（支持重新开局）
        // ============================================================

        public void ResetRoom()
        {
            // 销毁旧会话
            _gameSession?.Dispose();
            _gameSession = null;

            // 重置准备状态（可自定义策略，例如仅房主准备）
            foreach (var pid in _playerIds)
            {
                _playerReadyStatus[pid] = (pid == RoomOwnerId);
            }

            CurrentRoomState = RoomState.Lobby;
            OnRoomStateChanged?.Invoke(CurrentRoomState);
            Log("房间已重置，等待玩家准备");
        }

        // ============================================================
        // 清理
        // ============================================================

        public void Dispose()
        {
            _gameSession?.Dispose();
            Log($"房间 [{RoomId}] 已销毁");
        }

        // ============================================================
        // 工具
        // ============================================================

        private void Log(string msg) => OnGameLog?.Invoke($"[Room:{RoomId}] {msg}");
    }
}