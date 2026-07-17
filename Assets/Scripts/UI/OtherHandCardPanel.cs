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
    [SerializeField] private SpriteRenderer landLordTag;
    [SerializeField] private GameObject clock;
    [SerializeField] private GameObject aIImage;
    [SerializeField] private GameObject readyImage;
    [SerializeField] private Text clockText;

    // 缓存
    private int currentCardCount = 0;
    private bool isSubscribed = false;

    //时钟相关
    private int currentNumber = 30; // 当前数字
    private float timer = 0f;       // 累计时间的计时器
    private bool isCounting = false;// 是否在倒计时中


    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Awake()
    {

        SubscribeEvents();
    }

    private void Update()
    {

        RefreshClockText(Time.deltaTime);
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

        EventBus.Subscribe<ReadyEvent>(OnReady);
        EventBus.Subscribe<GameInitializedEvent>(OnGameInitialized);
        EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
        EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Subscribe<AIHostEvent>(OnAIHost);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<GameResetEvent>(OnGameReset);

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        EventBus.Unsubscribe<ReadyEvent>(OnReady);
        EventBus.Unsubscribe<GameInitializedEvent>(OnGameInitialized);
        EventBus.Unsubscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
        EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Unsubscribe<AIHostEvent>(OnAIHost);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<GameResetEvent>(OnGameReset);

        isSubscribed = false;
    }

    // ============================================================
    // 事件回调
    // ============================================================

    private void OnReady(ReadyEvent evt)
    {
        if (evt.PlayerId == playerId)
        {
            if (evt.IsReady)
            {
                readyImage.SetActive(true);
            }
            else
            {
                readyImage.SetActive(false);
            }
        }
    }

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
                break;
            }
        }

        readyImage.SetActive(false);
    }

    /// <summary>
    /// 地主确认：显示/隐藏地主标识
    /// </summary>
    private void OnLandlordConfirmed(LandlordConfirmedEvent evt)
    {
        int landlordSeat = evt.LandlordID;
        if (landlordSeat == playerId)
        {
            //显示地主UI标记
            landLordTag.enabled = true;
            // 将底牌加入手牌
            currentCardCount +=3;
            UpdateCardCountDisplay(currentCardCount);
        }
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
        if (evt.PlayerID == playerId)
        {
            clock.SetActive(true);
            StartCountdown();
        }
        else
        {
            clock.SetActive(false);
            StopCountdown();
        }
    }

    /// <summary>
    /// 切换AI托管
    /// </summary>
    private void OnAIHost(AIHostEvent evt)
    {
        if (evt.PlayerId == playerId)
        {
            aIImage.SetActive(evt.IsCalling);
        }
    }


    /// <summary>
    /// 游戏结束：取消高亮
    /// </summary>
    private void OnGameOver(GameOverEvent evt)
    {

    }

    private void OnGameReset(GameResetEvent evt)
    {
        landLordTag.enabled = false;
        StopCountdown();
        clock.SetActive(false);
        aIImage.SetActive(false);
        readyImage.SetActive(false);
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

    private void StartCountdown()
    {
        if (clockText == null)
        {
            Debug.LogError("请在 Inspector 面板中拖入 Text 物体！");
            return;
        }

        // 重置状态
        currentNumber = 30;
        timer = 0f;
        isCounting = true;
        clockText.text = currentNumber.ToString();
    }

    private void RefreshClockText(float time)
    {
        if (!isCounting) return; // 未启动倒计时则不执行

        timer += time; // 累计每帧的时间

        // 当累计时间达到1秒
        if (timer >= 1f)
        {
            timer -= 1f; // 减去1秒，保留余数（避免时间误差）
            currentNumber--;
            clockText.text = currentNumber.ToString();

            // 减到0时停止倒计时
            if (currentNumber <= 0)
            {
                isCounting = false;
                clockText.text = "0"; // 确保最终显示0
            }
        }
    }

    private void StopCountdown()
    {
        currentNumber = 30;
        timer = 0f;
        isCounting = false;
        clockText.text = currentNumber.ToString();
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