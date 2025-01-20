using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public UserInfo userInfo = new UserInfo();
    
    enum GameState
    {
        NONE = 0,
        MAIN,
        LOBBY,
        PAUSE,
        PLAY
    };

    public GameObject mainMenu = null;
    public GameObject lobbyMenu = null;
    public GameObject pauseMenu = null;

    public GameObject console = null;

    GameState gameState = GameState.NONE;

    private GameLobby gameLobby = null;
    private GameRelay gameRelay = null;

    public void Awake()
    {
        if (gameLobby == null)
        {
            gameLobby = GameObject.FindObjectOfType<GameLobby>();
        }

        if (gameRelay == null)
        {
            gameRelay = GameObject.FindObjectOfType<GameRelay>();
        }
    }

    public void Start()
    {
        // Shut everything off in case it is on from debugging.
        mainMenu.SetActive(true);
        lobbyMenu.SetActive(false);
        pauseMenu.SetActive(false);

        StartMainMenu();
        //StartLobbyMenu();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameState == GameState.PLAY)
                StartPauseMenu();

            else if (gameState == GameState.LOBBY)
                StartMainMenu();

            else if (gameState == GameState.PAUSE)
                StopPauseMenu();
        }
    }

    public bool Paused()
    {
        return gameState == GameState.PAUSE;
    }

    public bool Playing()
    {
        return gameState == GameState.PLAY;
    }

    public void StartMainMenu()
    {
        gameState = GameState.MAIN;

        mainMenu.SetActive(true);
        console.SetActive(true);

        lobbyMenu.SetActive(false);

        ShowCursor(true);
    }
    
    public void StartLobbyMenu()
    {
        gameState = GameState.LOBBY;

        lobbyMenu.SetActive(true);
        mainMenu.SetActive(false);
        console.SetActive(true);

        ShowCursor(true);
    }

    public void StartPauseMenu()
    {
        gameState = GameState.PAUSE;

        pauseMenu.SetActive(true);
        console.SetActive(true);

        ShowCursor(true);
    }

    public void StopPauseMenu()
    {
        gameState = GameState.PLAY;

        pauseMenu.SetActive(false);
        console.SetActive(true);

        ShowCursor(false);
    }

    public void StartGame(string lobbyCode)
    {
        // Network stuff.
        if (lobbyCode == null)
            StartHost();
        else
            StartClient(lobbyCode);

        // The rest of it.
        gameState = GameState.PLAY;

        mainMenu.SetActive(false);
        console.SetActive(true);

        lobbyMenu.SetActive(false);

        ShowCursor(false);
    }

    private async void StartHost()
    {
        string relayCode = await gameRelay.CreateRelay();

        gameLobby.CreateLobby(relayCode);

        // This is now handled by the lobby creation.
//        NetworkManager.Singleton.StartHost();
    }

    private void StartClient(string lobbyCode)
    {
        // Join.
        gameLobby.JoinLobbyById(lobbyCode);

        // This is now handled by the lobby join.
//        NetworkManager.Singleton.StartClient();
    }

    public void ExitGame()
    {
        gameLobby.LeaveLobby();

        gameState = GameState.MAIN;

        pauseMenu.SetActive(false);

        mainMenu.SetActive(true);
        console.SetActive(true);

        lobbyMenu.SetActive(false);

        ShowCursor(true);
    }

    public void QuitGame()
    {
        gameLobby.LeaveLobby();

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    private void ShowCursor(bool visible)
    {
        if (visible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        } else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
