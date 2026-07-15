using DouDiZhu.Logic.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CardUIState
{
    Idle,       // 闲置状态
    Selected    // 点击选中状态（向上突出）
}

public class CardPanel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform visualContainer;
    [SerializeField] Text cardText;

    private Card cardData;
    private CardUIState currentState = CardUIState.Idle;

    // 状态表现配置
    private float selectOffsetY = 40f; // 向上突出的像素距离
    private float originalY = 0f;

    public Card CardData => cardData;
    public CardUIState CurrentState => currentState;
    private void Awake()
    {
        // 如果未在面板拖拽，默认兜底获取自身
        if (visualContainer == null)
        {
            visualContainer = GetComponent<RectTransform>();
        }
        originalY = visualContainer.anchoredPosition.y;
    }

    void Start()
    {
        SetState(CardUIState.Idle);
    }

    public void Init(Card card)
    {
        cardData = card;
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

        cardText.text = cardData.Suit switch
        {
                CardSuit.Club => "♣",
                CardSuit.Diamond => "♦",
                CardSuit.Heart => "♥",
                CardSuit.Spade => "♠",
                _ => ""
        };
        cardText.text += "\n";
        cardText.text += cardData.Rank switch
        {
            CardRank.Rank10 => "10",
            CardRank.RankJ => "J",
            CardRank.RankQ => "Q",
            CardRank.RankK => "K",
            CardRank.RankA => "A",
            CardRank.Rank2 => "2",
            _ => ((int)cardData.Rank).ToString()
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 实现 Unity 鼠标/手指点击回调
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 状态机状态翻转切换
        if (currentState == CardUIState.Idle)
        {
            SetState(CardUIState.Selected);
        }
        else
        {
            SetState(CardUIState.Idle);
        }
    }

    /// <summary>
    /// 状态机核心控制切换
    /// </summary>
    public void SetState(CardUIState newState)
    {
        currentState = newState;
        Vector2 pos = visualContainer.anchoredPosition;

        switch (currentState)
        {
            case CardUIState.Idle:
                pos.y = originalY;
                break;

            case CardUIState.Selected:
                pos.y = originalY + selectOffsetY;
                break;
        }

        // 改变子容器坐标，完美避开外层 Horizontal Layout Group 的强制约束
        visualContainer.anchoredPosition = pos;
    }
}
