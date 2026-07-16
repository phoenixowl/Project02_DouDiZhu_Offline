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

    [Header("AI调度")]
    [SerializeField] private float _aiInterval = 0.5f;

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
        _room.OnStateUpdated += (state) => { /* 更新UI */ };

        // 发射开始游戏请求
        EventBus.Emit(new RequestStartGameEvent());

        StartCoroutine(AIDispatcher());
    }

    private IEnumerator AIDispatcher()
    {
        while (!_room.IsGameOver)
        {
            yield return new WaitForSeconds(_aiInterval);
            if (_room.HasPendingAIAction)
            {
                _room.ExecuteAIAction();
            }
        }
    }

    void OnDestroy() => _room?.Dispose();
}