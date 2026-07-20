using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Room;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    [Header("房间配置")]
    [SerializeField] private string _roomId = "TestRoom";
    [SerializeField] private int _ownerId = 10000;

    [Header("单机版玩家配置")]
    [SerializeField] private List<int> _aiPlayerIds = new List<int> { 10001, 10002 };

    private RoomManager _room;

    void Start()
    {
        Debug.Log("GameEntry : 单机版玩家ID写死在代码中，多人版请修改此处");
        _room = new RoomManager(_roomId, _ownerId);
        _room.OnGameLog += Debug.Log;
        _room.OnGameStateUpdated += (state) => { /* 更新UI */ };
        _room.OnRoomStateChanged += (state) => Debug.Log($"房间状态: {state}");
        _room.OnPlayerJoined += (pid) => Debug.Log($"玩家 {pid} 加入房间");
        _room.OnPlayerLeft += (pid) => Debug.Log($"玩家 {pid} 离开房间");

        //AI 玩家加入房间（模拟多人）
        foreach (var pid in _aiPlayerIds)
        {
            _room.JoinRoom(pid, isAI: true);
        }

        // AI 玩家也自动准备（模拟）
        // 在实际多人版中，AI 玩家不需要手动准备，这里为了演示
        foreach (var pid in _aiPlayerIds)
        {
            if (_room.IsPlayerInRoom(pid))
            {
                _room.ToggleReady(pid);
            }
        }

    }

    void Update()
    {
        _room?.Update(Time.deltaTime);
    }

    void OnDestroy()
    {
        _room?.Dispose();
    }
}