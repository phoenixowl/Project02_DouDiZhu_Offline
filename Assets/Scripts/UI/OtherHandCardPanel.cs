using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;

/// <summary>
/// 敌方玩家手牌区（显示牌背数量 + 玩家信息）
/// 由逻辑事件驱动更新，不持有任何逻辑层引用
/// </summary>
public class OtherHandCardPanel : MonoBehaviour
{
    [Header("绑定配置")]
    [SerializeField] private int playerId;                    // 对应的玩家ID（由外部赋值）
    [SerializeField] private Text playerNameText;             // 玩家名称
    [SerializeField] private Text cardCountText;              // 手牌数量显示（如 "17张"）
    [SerializeField] private Transform cardLayout;     // 牌背堆叠容器
    [SerializeField] private GameObject cardBackPrefab;       // 牌背预制体

    // 缓存
    private int currentCardCount = 0;
    private bool isSubscribed = false;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Start()
    {
        // 初始状态：隐藏所有
        SetVisible(false);

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
        if (isSubscribed) return;

        EventBus.Subscribe<GameInitializedEvent>(OnGameInitialized);
        EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
        EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        EventBus.Subscribe<GameInitializedEvent>(OnGameInitialized);
        EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
        EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);

        isSubscribed = false;
    }

    // ============================================================
    // 事件回调
    // ============================================================

    /// <summary>
    /// 游戏初始化：显示该玩家的手牌数量
    /// </summary>
    private void OnGameInitialized(GameInitializedEvent evt)
    {
        foreach (var player in evt.Players)
        {
            if (player.PlayerId == playerId)
            {
                currentCardCount = player.CardCount;
                UpdateDisplay(player.PlayerName, currentCardCount);
                SetVisible(true);
                break;
            }
        }
    }

    /// <summary>
    /// 地主确认：显示/隐藏地主标识
    /// </summary>
    private void OnLandlordConfirmed(LandlordConfirmedEvent evt)
    {
    }

    /// <summary>
    /// 出牌事件：如果出牌者是该玩家，减少手牌数量
    /// </summary>
    private void OnCardPlayed(CardPlayedEvent evt)
    {
        if (evt.PlayerID == playerId)
        {
            int playedCount = evt.CardGroup.Cards.Count;
            currentCardCount -= playedCount;
            if (currentCardCount < 0) currentCardCount = 0;
            UpdateCardCountDisplay(currentCardCount);
        }
    }

    /// <summary>
    /// 回合切换：高亮当前玩家
    /// </summary>
    private void OnTurnChanged(TurnChangedEvent evt)
    {

    }

    /// <summary>
    /// 游戏结束：取消高亮
    /// </summary>
    private void OnGameOver(GameOverEvent evt)
    {

    }

    // ============================================================
    // UI 更新
    // ============================================================

    private void UpdateDisplay(string playerName, int cardCount)
    {
        // 更新名称
        if (playerNameText != null)
            playerNameText.text = playerName;

        UpdateCardCountDisplay(cardCount);
    }

    private void UpdateCardCountDisplay(int cardCount)
    {
        currentCardCount = cardCount;

        // 更新数字
        if (cardCountText != null)
            cardCountText.text = $"{cardCount}张";

        // 更新牌背堆叠
        UpdateCardBacks(cardCount);
    }

    private void UpdateCardBacks(int cardCount)
    {
        if (cardLayout == null || cardBackPrefab == null)
            return;

        // 清空现有牌背
        foreach (Transform child in cardLayout)
            Destroy(child.gameObject);

        for (int i = 0; i < cardCount; i++)
        {
            GameObject go = Instantiate(cardBackPrefab, cardLayout);
        }

    }

    // ============================================================
    // 工具方法
    // ============================================================

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// 设置玩家ID（在 Start 之前调用）
    /// </summary>
    public void SetPlayerId(int id)
    {
        playerId = id;
    }

    // ============================================================
    // Debug
    // ============================================================

    private void OnValidate()
    {
        // Inspector 中修改时自动更新名称显示（仅编辑器）
#if UNITY_EDITOR
        if (playerNameText != null && !string.IsNullOrEmpty(playerNameText.text))
        {
            // 保持一致性
        }
#endif
    }
}