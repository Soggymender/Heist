using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class LobbyMenuUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    private Transform entryContainer;
    private Transform entryTemplate;

    // This contains and positions the Join button next to the selected lobby.    
    private Transform entrySelection;
    private LobbyEntry selectedEntry = null;
 
    private GameLobby gameLobby = null;

    private float updateCadenceTimer;

    int maxLobbies = 10;
    List<LobbyEntry> lobbyEntries = new List<LobbyEntry>();

    GameManager gameManager = null;

    void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameObject.FindObjectOfType<GameManager>();
        }

        if (gameLobby == null)
        {
            gameLobby = GameObject.FindObjectOfType<GameLobby>();
        }

        entryContainer = transform.Find("LobbyEntryContainer");
        entryTemplate = entryContainer.Find("LobbyEntryTemplate");

        entrySelection = entryContainer.Find("Selection");

        entryTemplate.gameObject.SetActive(false);
        entrySelection.gameObject.SetActive(false);

        RectTransform rect = entryContainer.GetComponent<RectTransform>();

        float templateHeight = rect.sizeDelta.y / maxLobbies;

        for (int i = 0; i < maxLobbies; i++)
        {
            Transform transform = Instantiate(entryTemplate, entryContainer);
            RectTransform entryRectTransform = transform.GetComponent<RectTransform>();
            entryRectTransform.anchoredPosition = new Vector2(0, entryRectTransform.anchoredPosition.y + -templateHeight * i);

            LobbyEntry entry = transform.GetComponent<LobbyEntry>();

            entry.lobbyCode = "";

            lobbyEntries.Add(entry);
        }

        hostButton.onClick.AddListener(() =>
        {
            StartHostGame();
        });

        joinButton.onClick.AddListener(() =>
        {
            StartClientGame();
        });
    }

    public void Update()
    {
        HandleUpdateCadence();   
    }

    private void ClearLobbyEntries()
    {
        for (int i = 0; i < maxLobbies; i++)
        {
            lobbyEntries[i].transform.gameObject.SetActive(false);
            lobbyEntries[i].lobbyCode = "";
        }
    }

    private async void HandleUpdateCadence()
    {
        updateCadenceTimer += Time.deltaTime;
        if (updateCadenceTimer >= 5.0f)
        {
            updateCadenceTimer = 0.0f;
                         
            List<LobbyInfo> lobbies = await gameLobby.ListLobbies();
            if (lobbies != null)
            {
                ClearLobbyEntries();

                for (int i = 0; i < lobbies.Count; i++)
                {
                    lobbyEntries[i].transform.Find("OwnerText").GetComponent<TMP_Text>().text = lobbies[i].ownerName;
                    lobbyEntries[i].transform.gameObject.SetActive(true);
                    lobbyEntries[i].lobbyCode = lobbies[i].code;
                }

                UpdateSelectionPosition();
            }
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        selectedEntry = eventData.selectedObject.GetComponent<LobbyEntry>();

        UpdateSelectionPosition();
        Debug.Log("selected\n");
    }

    
    public void OnDeselect(BaseEventData eventData)
    {
        // We care about deselection due to clicking on something not on the list.
        // We do not care about deselection to mouse-up from a recent selection.
        UpdateSelectionPosition();
        Debug.Log("deselected\n");
    }

    private void UpdateSelectionPosition()
    {
        // If we're null, hide and get out.
        if (selectedEntry == null)
        {
            entrySelection.gameObject.SetActive(false);
            return;
        }

        // Verify the selection is still active.
        if (selectedEntry.gameObject.activeSelf == false)
        {
            entrySelection.gameObject.SetActive(false);
            selectedEntry = null;
            return;
        }

        // Activate and reposition to match the latest selection.

        // Move the Selection object to the same position as the selected object and activate it.
        entrySelection.gameObject.SetActive(true);

        RectTransform selectionRectTransform = entrySelection.GetComponent<RectTransform>();
        selectionRectTransform.anchoredPosition = selectedEntry.GetComponent<RectTransform>().anchoredPosition;
    }

    void StartHostGame()
    {
        gameManager.StartGame(null);
    }

    void StartClientGame()
    {
        gameManager.StartGame(selectedEntry.lobbyCode);
    }
}
