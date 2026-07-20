using DouDiZhu.Logic.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UI;

public class HoleCardPanel : MonoBehaviour
{

    [SerializeField] private Transform holeCardLayout;
    [SerializeField] private Text holeCardText;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Awake()
    {
        // 订阅事件
        SubscribeEvents();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ============================================================
    // 事件订阅
    // ============================================================

    private void SubscribeEvents()
    {
        EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Subscribe<GameResetEvent>(OnGameReset);
    }

    private void UnsubscribeEvents()
    {
        EventBus.Unsubscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Unsubscribe<GameResetEvent>(OnGameReset);
    }

    // ============================================================
    // 回调函数
    // ============================================================
    private void OnGameReset(GameResetEvent evt)
    {
        holeCardLayout.gameObject.SetActive(false);
        holeCardText.gameObject.SetActive(false);
    }

    private void OnLandlordConfirmed(LandlordConfirmedEvent evt)
    {
        if (holeCardLayout != null)
        {
            for (int i = 0; i < holeCardLayout.childCount; i++)
            {
                CardPanel cardPanel = holeCardLayout.GetChild(i).GetComponent<CardPanel>();
                cardPanel.Init(evt.HoleCards[i]);
                cardPanel.SetInteractable(false);
            }
        }

        holeCardLayout.gameObject.SetActive(true);
        holeCardText.gameObject.SetActive(true);
    }
}
