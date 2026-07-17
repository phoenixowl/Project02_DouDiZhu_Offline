using DouDiZhu.Logic.Events;
using DouDiZhu.Logic.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardCounterPanel : MonoBehaviour
{
    [SerializeField] private Transform numLayout;
    [SerializeField] private List<Text> numTexts;
    // Start is called before the first frame update
    private void Awake()
    {
        // 订阅事件
        SubscribeEvents();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
    }

    private void UnsubscribeEvents()
    {
        EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
    }

    /// <summary>
    /// 出牌事件：更新记牌器
    /// </summary>
    private void OnCardPlayed(CardPlayedEvent evt)
    {
        CardGroup cardGroup = evt.CardGroup;

        foreach (var card in cardGroup.Cards)
        {
            // 17→0, 3→14，共15个子物体
            int index = (int)(17 - card.Rank);
            if (index >= 0 && index < numTexts.Count && numTexts[index] != null)
            {
                if (int.TryParse(numTexts[index].text, out int value))
                {
                    numTexts[index].text = (value - 1).ToString();
                }
            }
        }
    }
}
