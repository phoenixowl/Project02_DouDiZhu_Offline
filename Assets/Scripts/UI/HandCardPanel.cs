using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Services;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class HandCardPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Transform handCardLayout;    // 手牌容器（含 Horizontal Layout Group）
    [SerializeField] private Transform tableCardLayout;   // 桌面牌型容器（用于显示上家出的牌）
    [SerializeField] private GameObject cardPrefab;       // 卡牌预制体
    [SerializeField] private Button playCardButton;
    [SerializeField] private Button passButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button bidButton;
    [SerializeField] private Button notBidButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button aIHostButton;
    [SerializeField] private SpriteRenderer landLordTag;
    [SerializeField] private GameObject aIImage;
    [SerializeField] private GameObject readyImage;
    [SerializeField] private GameObject clock;
    [SerializeField] private Text clockText;
    [SerializeField] private Text informText;
    [Header("本地玩家信息")]
    [SerializeField] private int localPlayerId = 10000;       // 单机版固定为0，联机版由外部赋值

    // 本地手牌数据（单一数据源）
    private List<Card> localHandCards = new List<Card>();

    // 当前桌面牌型（用于本地压制校验）
    private CardGroup currentTableGroup = null;

    // 当前显示的所有卡牌 Panel 引用（便于刷新时销毁）
    private List<CardPanel> activeCardPanels = new List<CardPanel>();

    // 当前选中的卡牌列表（用于出牌）
    private List<Card> selectedCards = new List<Card>();

    //时钟相关
    private int currentNumber = 30; // 当前数字
    private float timer = 0f;       // 累计时间的计时器
    private bool isCounting = false;// 是否在倒计时中

    //信息提示文本相关
    private float informLifeTime = 0f;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Awake()
    {
        // 订阅事件
        SubscribeEvents();
    }

    void Start()
    {
        // 绑定按钮事件
        if (playCardButton != null)
            playCardButton.onClick.AddListener(OnPlayButtonClick);

        if (passButton != null)
            passButton.onClick.AddListener(OnPassButtonClick);

        if (hintButton != null)
            hintButton.onClick.AddListener(OnHintButtonClick);

        if (bidButton != null)
            bidButton.onClick.AddListener(OnBidButtonClick);

        if (notBidButton != null)
            notBidButton.onClick.AddListener(OnNotBidButtonClick);

        if (aIHostButton != null)
            aIHostButton.onClick.AddListener(OnAIHostButtonClick);

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyButtonClick);

        // 初始状态：手牌为空，桌面为空
        RefreshHandUI();
        RefreshTableUI(null);
    }

    private void Update()
    {

        RefreshClockText(Time.deltaTime);
        informTextCountdown(Time.deltaTime);
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
        EventBus.Subscribe<ReadyEvent>(OnReady);
        EventBus.Subscribe<GameInitializedEvent>(OnGameInitialized);
        EventBus.Subscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Subscribe<HintEvent>(OnHinted);
        EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
        EventBus.Subscribe<RoundClearedEvent>(OnRoundCleared);
        EventBus.Subscribe<PlayRejectedEvent>(OnPlayRejected);
        EventBus.Subscribe<AIHostEvent>(OnAIHost);
        EventBus.Subscribe<GameOverEvent>(OnGameOver); // 可选，用于清空选中状态
        EventBus.Subscribe<GameResetEvent>(OnGameReset);
    }

    private void UnsubscribeEvents()
    {
        EventBus.Unsubscribe<ReadyEvent>(OnReady);
        EventBus.Unsubscribe<GameInitializedEvent>(OnGameInitialized);
        EventBus.Unsubscribe<LandlordConfirmedEvent>(OnLandlordConfirmed);
        EventBus.Unsubscribe<HintEvent>(OnHinted);
        EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
        EventBus.Unsubscribe<RoundClearedEvent>(OnRoundCleared);
        EventBus.Unsubscribe<PlayRejectedEvent>(OnPlayRejected);
        EventBus.Unsubscribe<AIHostEvent>(OnAIHost);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<GameResetEvent>(OnGameReset);
    }

    // ============================================================
    // 事件回调（下行领域事件 → UI 更新）
    // ============================================================

    private void OnReady(ReadyEvent evt)
    {
        if (evt.PlayerId == localPlayerId)
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
    /// 游戏初始化：设置初始手牌
    /// </summary>
    private void OnGameInitialized(GameInitializedEvent evt)
    {
        // 从事件中获取本地玩家的手牌
        foreach (var player in evt.Players)
        {
            if (player.PlayerId == localPlayerId && player.HandCards != null)
            {
                localHandCards = new List<Card>(player.HandCards);
                RefreshHandUI();
                break;
            }
        }

        readyImage.SetActive(false);
        readyButton.gameObject.SetActive(false);
        bidButton.gameObject.SetActive(true);
        notBidButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 地主确认：如果本地玩家是地主，加入底牌
    /// </summary>
    private void OnLandlordConfirmed(LandlordConfirmedEvent evt)
    {
        // 检查地主是否是自己
        int landlordSeat = evt.LandlordID;
        if (landlordSeat == localPlayerId)
        {
            //显示地主UI标记
            landLordTag.enabled = true;
            // 将底牌加入手牌
            localHandCards.AddRange(evt.HoleCards);
            // 排序
            localHandCards.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            RefreshHandUI();
        }

        bidButton.gameObject.SetActive(false);
        notBidButton.gameObject.SetActive(false);
        playCardButton.gameObject.SetActive(true);
        passButton.gameObject.SetActive(true);
        hintButton.gameObject.SetActive(true);
    }


    /// <summary>
    /// 提示事件回调，自动选择建议的牌，或者提示不出
    /// </summary>
    private void OnHinted(HintEvent evt)
    {
        if(evt.PlayerId == localPlayerId)
        {
            if (evt.AIDecision.ShouldPlay)
            {
                ClearSelectedState();
                foreach (var panel in activeCardPanels)
                {
                    foreach(Card c in evt.AIDecision.SelectedCards)
                    {
                        if (panel != null && panel.CurrentState == CardUIState.Idle && c.Equals(panel.CardData))
                        {
                            if (!selectedCards.Contains(panel.CardData))
                            {
                                selectedCards.Add(panel.CardData);
                            }
                            panel.SetState(CardUIState.Selected);
                            break;

                        }
                    }
                }
            }
            else
            {
                UpdateInformText("当前没有可以大过上家的牌");
            }
        }
    }

    /// <summary>
    /// 回合切换：高亮当前玩家
    /// </summary>
    private void OnTurnChanged(TurnChangedEvent evt)
    {
        if (evt.PlayerID == localPlayerId)
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
    /// 有玩家出牌：更新桌面牌型；如果是本地玩家，则移除手牌
    /// </summary>
    private void OnCardPlayed(CardPlayedEvent evt)
    {
        // 1. 更新桌面牌型
        currentTableGroup = evt.CardGroup;
        RefreshTableUI(currentTableGroup);

        // 2. 如果是本地玩家出的牌，更新手牌
        if (evt.PlayerID == localPlayerId)
        {
            // 从本地手牌中移除这些牌
            var playedCards = evt.CardGroup.Cards;
            foreach (var card in playedCards)
            {
                localHandCards.Remove(card);
            }
            RefreshHandUI();
        }
    }

    /// <summary>
    /// 一轮结束：清空桌面牌型
    /// </summary>
    private void OnRoundCleared(RoundClearedEvent evt)
    {
        currentTableGroup = null;
        RefreshTableUI(null);
    }

    /// <summary>
    /// 出牌被拒绝：恢复选中状态（取消选中）
    /// </summary>
    private void OnPlayRejected(PlayRejectedEvent evt)
    {
        // 清除所有卡牌的选中状态
        ClearSelectedState();
        // 提示信息（可由其他UI处理）
        UpdateInformText($"出牌被拒绝: {evt.Reason}");
        Debug.LogWarning($"出牌被拒绝: {evt.Reason}");
    }

    /// <summary>
    /// 切换AI托管
    /// </summary>
    private void OnAIHost(AIHostEvent evt)
    {
        if (evt.PlayerId == localPlayerId)
        {
            aIImage.SetActive(evt.IsCalling);
        }
    }

    /// <summary>
    /// 游戏结束：清除选中状态
    /// </summary>
    private void OnGameOver(GameOverEvent evt)
    {
        ClearSelectedState();
    }

    private void OnGameReset(GameResetEvent evt)
    {
        landLordTag.enabled = false;
        currentTableGroup = null;
        informLifeTime = 0f;
        StopCountdown();
        ClearHand();
        RefreshTableUI(null);
        clock.SetActive(false);
        aIImage.SetActive(false);
        readyImage.SetActive(false);

        playCardButton.gameObject.SetActive(false);
        passButton.gameObject.SetActive(false);
        hintButton.gameObject.SetActive(false);
        bidButton.gameObject.SetActive(false);
        notBidButton.gameObject.SetActive(false);
        readyButton.gameObject.SetActive(true);
    }

    // ============================================================
    // UI 刷新
    // ============================================================

    private void RefreshHandUI()
    {
        // 清空现有手牌显示
        ClearHand();

        if (localHandCards == null || localHandCards.Count == 0)
            return;

        // 按规则排序（大->小）
        localHandCards.Sort((a, b) => b.Rank.CompareTo(a.Rank));

        foreach (var cardData in localHandCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, handCardLayout);
            CardPanel panel = cardObj.GetComponent<CardPanel>();
            if (panel != null)
            {
                panel.Init(cardData);
                // 添加点击事件监听（选中/取消选中）
                panel.OnCardClick += OnCardPanelClick;
                activeCardPanels.Add(panel);
            }
        }
    }

    private void RefreshTableUI(CardGroup group)
    {
        // 清空桌面显示
        if (tableCardLayout != null)
        {
            foreach (Transform child in tableCardLayout)
                Destroy(child.gameObject);
        }

        if (group == null || group.Cards.Count == 0)
            return;

        // 在桌面显示牌（简单显示牌面，也可以显示牌背）
        foreach (var card in group.Cards)
        {
            if (cardPrefab != null && tableCardLayout != null)
            {
                GameObject cardObj = Instantiate(cardPrefab, tableCardLayout);
                CardPanel panel = cardObj.GetComponent<CardPanel>();
                if (panel != null)
                {
                    panel.Init(card);
                    panel.SetInteractable(false); // 桌面牌不可点击
                }
            }
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

    private void ClearHand()
    {
        foreach (var panel in activeCardPanels)
        {
            if (panel != null)
            {
                panel.OnCardClick -= OnCardPanelClick;
                Destroy(panel.gameObject);
            }
        }
        activeCardPanels.Clear();
        selectedCards.Clear();
    }

    private void ClearSelectedState()
    {
        selectedCards.Clear();
        foreach (var panel in activeCardPanels)
        {
            if (panel != null && panel.CurrentState == CardUIState.Selected)
            {
                panel.SetState(CardUIState.Idle);
            }
        }
    }

    // ============================================================
    // 信息提示文本
    // ============================================================
    private void UpdateInformText(string text)
    {
        informText.text = text;
        informLifeTime = 3.0f;
    }

    private void informTextCountdown(float deltatime)
    {
        if(informLifeTime > 0)
        {
            informLifeTime -= deltatime;
        }
        else
        {
            informText.text = "";
        }
    }


    // ============================================================
    // 卡牌点击事件（选中/取消选中）
    // ============================================================

    private void OnCardPanelClick(CardPanel panel)
    {
        if (panel == null) return;

        if (panel.CurrentState == CardUIState.Selected)
        {
            // 取消选中
            panel.SetState(CardUIState.Idle);
            selectedCards.Remove(panel.CardData);
        }
        else
        {
            // 选中
            panel.SetState(CardUIState.Selected);
            if (!selectedCards.Contains(panel.CardData))
                selectedCards.Add(panel.CardData);
        }
    }

    // ============================================================
    // 按钮事件（发射请求事件）
    // ============================================================

    /// <summary>
    /// 出牌按钮点击
    /// </summary>
    private void OnPlayButtonClick()
    {
        if (selectedCards.Count == 0)
        {
            UpdateInformText("请先选择要出的牌");
            Debug.LogWarning("请先选择要出的牌");
            return;
        }

        // ---- 本地预检（仅用于提升体验，最终由逻辑层决定） ----
        // 1. 检查牌型合法性
        CardGroup group = new CardGroup(selectedCards);
        if (group.Type == CardType.Invalid)
        {
            UpdateInformText("牌型非法，请重新选择");
            Debug.LogWarning("牌型非法，请重新选择");
            ClearSelectedState();
            return;
        }

        // 2. 检查是否能压过桌面牌型（如果有）
        if (currentTableGroup != null && currentTableGroup.Cards.Count > 0)
        {
            bool canBeat = CardRule.CanBeat(group, currentTableGroup);
            if (!canBeat)
            {
                UpdateInformText("牌型太小，无法压过上家");
                Debug.LogWarning("牌型太小，无法压过上家");
                ClearSelectedState();
                return;
            }
        }

        // ---- 本地预检通过，发送请求事件 ----
        EventBus.Emit(new RequestPlayCardEvent(localPlayerId, new List<Card>(selectedCards)));
        // 清空选中状态（等待事件回调更新UI）
        ClearSelectedState();
    }

    /// <summary>
    /// 过牌按钮点击
    /// </summary>
    private void OnPassButtonClick()
    {
        EventBus.Emit(new RequestPassEvent(localPlayerId));
        ClearSelectedState();
    }

    private void OnHintButtonClick()
    {
        ClearSelectedState();
        EventBus.Emit(new RequestHintEvent(localPlayerId));
    }

    /// <summary>
    /// 叫地主按钮点击
    /// </summary>
    private void OnBidButtonClick()
    {
        EventBus.Emit(new RequestBidEvent(localPlayerId, true));
    }

    private void OnNotBidButtonClick()
    {
        EventBus.Emit(new RequestBidEvent(localPlayerId, false));
    }

    /// <summary>
    /// AI托管按钮点击
    /// </summary>
    private void OnAIHostButtonClick()
    {
        EventBus.Emit(new RequestAIHostEvent(localPlayerId));
    }

    private void OnReadyButtonClick()
    {
        EventBus.Emit(new RequestReadyEvent(localPlayerId));
    }

    // ============================================================
    // 公共方法（外部可调用）
    // ============================================================

    /// <summary>
    /// 设置本地玩家ID（联机版使用）
    /// </summary>
    public void SetLocalPlayerId(int id)
    {
        localPlayerId = id;
    }
}