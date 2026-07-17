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
    [SerializeField] private int _ownerId = 0;
    [SerializeField] private bool _allAI = false;

    private RoomManager _room;

    void Start()
    {
        var playerIds = new List<int> { 10000, 10001, 10002 };
        var aiMap = new Dictionary<int, bool>
        {
            { 10000, _allAI },
            { 10001, true },
            { 10002, true }
        };

        _room = new RoomManager(_roomId, _ownerId, playerIds, aiMap);
        _room.OnGameLog += Debug.Log;
        _room.OnGameStateUpdated += (state) => { /* 更新UI */ };
        _room.OnRoomStateChanged += (state) => Debug.Log($"房间状态: {state}");

        // 模拟：房主自动准备，其他玩家通过UI点击准备（此处演示全部自动准备）
        _room.ToggleReady(10000);
        _room.ToggleReady(10001);
        _room.ToggleReady(10002);
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