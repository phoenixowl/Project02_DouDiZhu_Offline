using UnityEngine;
using DouDiZhu.Logic.Models;
using DouDiZhu.Logic.Services;

public class TestDeck : MonoBehaviour
{
    void Start()
    {
        var state = GameInitializer.InitializeNewGame();
        foreach (var p in state.Players)
        {
            Debug.Log($"{p.PlayerName} 手牌数: {p.CardCount}");
            Debug.Log(string.Join(", ", p.HandCards));
        }
        Debug.Log($"底牌: {string.Join(", ", state.HoleCards)}");

        // 验证总数
        int total = 0;
        foreach (var p in state.Players) total += p.CardCount;
        total += state.HoleCards.Count;
        Debug.Log($"总牌数验证: {total} (应为54)");
    }
}