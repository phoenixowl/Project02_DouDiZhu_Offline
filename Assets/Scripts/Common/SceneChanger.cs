using DouDiZhu.Logic.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    void Awake()
    {
        SubscribeEvents();
    }
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ============================================================
    // 事件订阅
    // ============================================================

    private void SubscribeEvents()
    {
        EventBus.Subscribe<LeaveRoomEvent>(OnLeaveRoom);

    }

    private void UnsubscribeEvents()
    {
        EventBus.Unsubscribe<LeaveRoomEvent>(OnLeaveRoom);

    }

    // ============================================================
    // 事件回调
    // ============================================================

    private void OnLeaveRoom(LeaveRoomEvent evt)
    {
        if(evt.PlayerId == 10000)
        {
            SceneManager.LoadScene("StartMenuScene");
        }
    }
}
