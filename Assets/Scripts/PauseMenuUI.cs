using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;

    GameManager gameManager = null;
private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameObject.FindObjectOfType<GameManager>();
        }

        resumeButton.onClick.AddListener(() =>
        {
            gameManager.StopPauseMenu();
        });

        exitButton.onClick.AddListener(() =>
        {
            gameManager.ExitGame();
        });
    }
}
