using DouDiZhu.Logic.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CardUIState
{
    Idle,       // 闲置状态（正常显示）
    Selected    // 点击选中状态（向上突出）
}

public class CardPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 引用")]
    [SerializeField] private RectTransform visualContainer;
    [SerializeField] private Text cardText;

    [Header("状态表现配置")]
    [SerializeField] private float selectOffsetY = 40f; // 向上突出的像素距离
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.6f); // 选中时的背景色
    [SerializeField] private Color normalColor = Color.white; // 正常状态背景色

    // 数据与状态
    private Card cardData;
    private CardUIState currentState = CardUIState.Idle;
    private float originalY = 0f;
    private bool isInteractable = true;

    // 事件：点击时通知外部（由 HandCardPanel 订阅）
    public event System.Action<CardPanel> OnCardClick;

    public Card CardData => cardData;
    public CardUIState CurrentState => currentState;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Awake()
    {
        // 获取组件引用
        if (visualContainer == null)
            visualContainer = GetComponent<RectTransform>();

        if (cardText == null)
            cardText = GetComponentInChildren<Text>();

        // 记录初始 Y 坐标
        originalY = visualContainer.anchoredPosition.y;
    }

    private void Start()
    {
        SetState(CardUIState.Idle);
    }

    // ============================================================
    // 初始化与数据绑定
    // ============================================================

    public void Init(Card card)
    {
        cardData = card;
        UpdateCardDisplay();
        SetState(CardUIState.Idle);
        isInteractable = true;
    }

    private void UpdateCardDisplay()
    {
        if (cardData == null || cardText == null) return;

        // 大小王特殊处理
        if (cardData.Rank == CardRank.BigJoker)
        {
            cardText.text = "大\n王";
            return;
        }
        if (cardData.Rank == CardRank.SmallJoker)
        {
            cardText.text = "小\n王";
            return;
        }

        // 常规牌：花色 + 点数
        string suitSymbol = cardData.Suit switch
        {
            CardSuit.Club => "♣",
            CardSuit.Diamond => "♦",
            CardSuit.Heart => "♥",
            CardSuit.Spade => "♠",
            _ => ""
        };

        string rankSymbol = cardData.Rank switch
        {
            CardRank.Rank10 => "10",
            CardRank.RankJ => "J",
            CardRank.RankQ => "Q",
            CardRank.RankK => "K",
            CardRank.RankA => "A",
            CardRank.Rank2 => "2",
            _ => ((int)cardData.Rank).ToString()
        };

        cardText.text = $"{suitSymbol}\n{rankSymbol}";
    }

    // ============================================================
    // 状态控制
    // ============================================================

    public void SetState(CardUIState newState)
    {
        currentState = newState;

        // 位置变化
        Vector2 pos = visualContainer.anchoredPosition;
        pos.y = (newState == CardUIState.Selected) ? originalY + selectOffsetY : originalY;
        visualContainer.anchoredPosition = pos;
    }

    /// <summary>
    /// 切换选中状态（方便 HandCardPanel 调用）
    /// </summary>
    public void ToggleSelected()
    {
        SetState(currentState == CardUIState.Idle ? CardUIState.Selected : CardUIState.Idle);
    }

    /// <summary>
    /// 设置是否可交互（桌面牌设为 false）
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    // ============================================================
    // 点击事件（IPointerClickHandler）
    // ============================================================

    public void OnPointerClick(PointerEventData eventData)
    {
        // 不可交互时忽略点击
        if (!isInteractable) return;

        // 触发外部事件（由 HandCardPanel 处理选中逻辑）
        OnCardClick?.Invoke(this);
    }

    // ============================================================
    // 重置（对象池复用时可调用）
    // ============================================================

    public void ResetPanel()
    {
        cardData = null;
        SetState(CardUIState.Idle);
        isInteractable = true;
        OnCardClick = null;
    }
}