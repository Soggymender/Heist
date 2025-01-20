using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameIF;  
    [SerializeField] private Button startMPButton;
    [SerializeField] private Button quitButton;

    GameManager gameManager = null;
private void Awake()
{
    if (gameManager == null)
    {
        gameManager = GameObject.FindObjectOfType<GameManager>();
    }

    usernameIF.onEndEdit.AddListener((string value) =>
    {
        if (value.Length > 0)
        {
            gameManager.userInfo.username = usernameIF.text;
            startMPButton.interactable = true;
        } else
        {
            startMPButton.interactable = false;
        }
    });

    startMPButton.onClick.AddListener(() =>
    {
        gameManager.StartLobbyMenu();
    });

    quitButton.onClick.AddListener(() =>
    {
        gameManager.QuitGame();
    });
}


}
