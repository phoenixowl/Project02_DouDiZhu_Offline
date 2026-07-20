using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuPanel : MonoBehaviour
{
    [SerializeField] Button startGameButton;

    // Start is called before the first frame update
    void Start()
    {

        startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnStartGameButtonClicked()
    {
        SceneManager.LoadScene("RoomScene");
    }
}
