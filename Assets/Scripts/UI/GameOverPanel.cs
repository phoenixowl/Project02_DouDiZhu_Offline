using DouDiZhu.Logic.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{

    [SerializeField] private Text whoWonText;
    [SerializeField] private Button nextRoundButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Transform panel;


    [SerializeField] private int loaclPlayerID;


    private void Awake()
    {

        SubscribeEvents();
    }

    void Start()
    {
        // 绑定按钮事件
        if (nextRoundButton != null)
            nextRoundButton.onClick.AddListener(OnNextRoundButtonClick);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClick);
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
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<GameResetEvent>(OnGameReset);

    }

    private void UnsubscribeEvents()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<GameResetEvent>(OnGameReset);

    }

    // ============================================================
    // 按钮事件（发射请求事件）
    // ============================================================

    private void OnNextRoundButtonClick()
    {
        panel.gameObject.SetActive(false);
    }
    private void OnExitButtonClick()
    {
        EventBus.Emit(new RequestLeaveRoomEvent(loaclPlayerID));
    }

    // ============================================================
    // 事件回调
    // ============================================================

    private void OnGameOver(GameOverEvent evt)
    {
        panel.gameObject.SetActive(true);
        if (evt.IsLandLord)
        {
            whoWonText.text = "地主胜利";
        }
        else
        {
            whoWonText.text = "农民胜利";
        }
    }

    private void OnGameReset(GameResetEvent evt)
    {
        //panel.gameObject.SetActive(false);

    }

    // ============================================================
    // Ui显示
    // ============================================================

    private void showGameOverScreen()
    {

    }

}
