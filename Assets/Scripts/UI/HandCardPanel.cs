using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class HandCardPanel : MonoBehaviour
{
    [SerializeField] private Transform handCardLayout; // 指向嵌套了 Horizontal Layout Group 的节点
    [SerializeField] private Button playCardButton; 
    [SerializeField] private GameObject cardPrefab;      // 卡牌的 Prefab

    // 追踪当前处于激活状态的手牌 UI 脚本列表
    private List<CardPanel> activeCardPanels = new List<CardPanel>();

    void Start()
    {
        playCardButton.onClick.AddListener(onPlayCardBtnClicked);


        var randomHand = CardTest.GenerateRandomCards(17);
        InitializeHand(randomHand);
    }


    /// <summary>
    /// 【一键初始化】根据传入的卡牌数据生成手牌
    /// </summary>
    public void InitializeHand(List<Card> cards)
    {
        // 1. 回收或销毁已有旧手牌
        ClearHand();

        if (cards == null || cards.Count == 0) return;

        // 2. 严格遵守工程规范：手牌显示前按照卡牌权重进行降序排序（大牌在左，小牌在右）
        cards.Sort();

        // 3. 动态生成
        foreach (var cardData in cards)
        {
            if (cardPrefab == null || handCardLayout == null)
            {
                Debug.LogError("[HandCardPanel] 预制体或布局容器未挂载！");
                return;
            }

            GameObject cardObj = Instantiate(cardPrefab, handCardLayout);
            CardPanel cardPanel = cardObj.GetComponent<CardPanel>();

            if (cardPanel != null)
            {
                cardPanel.Init(cardData);
                activeCardPanels.Add(cardPanel);
            }
        }
    }

    /// <summary>
    /// 清理手牌容器
    /// </summary>
    public void ClearHand()
    {
        foreach (var panel in activeCardPanels)
        {
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }
        activeCardPanels.Clear();
    }

    /// <summary>
    /// 【出牌核心逻辑】检查牌型合法性并打出
    /// </summary>
    /// <returns>出牌成功返回 true，非法返回 false</returns>
    public bool PlaySelectedCards()
    {
        List<CardPanel> targetPanels = new List<CardPanel>();
        List<Card> selectedCards = new List<Card>();

        // 1. 筛选出所有处于 Selected 状态且未被隐藏的卡牌
        foreach (var panel in activeCardPanels)
        {
            if (panel.gameObject.activeSelf && panel.CurrentState == CardUIState.Selected)
            {
                targetPanels.Add(panel);
                selectedCards.Add(panel.CardData);
            }
        }

        // 容错：没挑牌就点出牌
        if (selectedCards.Count == 0)
        {
            Debug.LogWarning("[出牌错误] 请先点击选择你想要打出的手牌。");
            return false;
        }

        // 2. 解析牌型（自动在 CardGroup 构造时通过纯逻辑类 CardRule 校验）
        CardGroup cardGroup = new CardGroup(selectedCards);

        // 3. 检查合法性
        if (cardGroup.Type == CardType.Invalid)
        {
            Debug.LogWarning("[出牌错误] 你选择的牌型不符合斗地主规则！");
            // 这里可以向本地事件中心抛出一个通知，让 UI 显示“牌型不合法”的弹窗提示
            return false;
        }

        // 扩展提示：在完整的单机/联机业务中，通过了基础牌型校验后，
        // 还需要在此处调用 CardRule.CanBeat(cardGroup, currentTableGroup) 来比对是否压得过上家。

        // 4. 执行打出表现：按要求暂时设为 setActive(false)
        foreach (var panel in targetPanels)
        {
            panel.gameObject.SetActive(false);
        }

        // 5. 维护当前的活动列表，剔除已被打出的牌
        activeCardPanels.RemoveAll(p => !p.gameObject.activeSelf);

        Debug.Log($"[出牌成功] 成功打出牌型: {cardGroup.Type}, 剩余手牌数: {activeCardPanels.Count}");

        // 当剩余手牌为0时，可以向 GameManager 发送游戏结束/胜利的事件
        return true;
    }

    void onPlayCardBtnClicked()
    {
        PlaySelectedCards();
    }
}