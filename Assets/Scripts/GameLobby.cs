using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public struct LobbyInfo
{
    public string ownerName;
    public int playerCount;
    public int maxPlayerCount;

    public string ownerLocation;

    public string code;
}

public class GameLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;

    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    private string playerName;

    private GameRelay gameRelay;

    private void Awake()
    {
        if (gameRelay == null)
        {
            gameRelay = GameObject.FindObjectOfType<GameRelay>();
        }
    }

    private async void Start() {

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        playerName = "Squishymonster" + UnityEngine.Random.Range(10, 99);
        Debug.Log(playerName);
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby == null)
            return;

        heartbeatTimer += Time.deltaTime;
        if (heartbeatTimer >= 15.0f)
        {         
            heartbeatTimer = 0.0f;

            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }

    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby == null)
            return;

        lobbyUpdateTimer += Time.deltaTime;
        if (lobbyUpdateTimer >= 5.0f)
        {
            lobbyUpdateTimer = 0.0f;

            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            joinedLobby = lobby;
        }
    }


    [Command]
    public async void CreateLobby(string relayCode)
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "CaptureTheFlag") },
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, "hideout") },
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            };
             
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);

            PrintPlayers(hostLobby);
        } catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [Command]
    public async Task<List<LobbyInfo>> ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    //new QueryFilter(QueryFilter.FieldOptions.S1, "CaptureTheFlag", QueryFilter.OpOptions.EQ)
                },

                Order = new List<QueryOrder> {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };  
            
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);

            List<LobbyInfo> lobbies = new List<LobbyInfo>();

            foreach (Lobby lobby in queryResponse.Results)
            {
                LobbyInfo lobbyInfo = new LobbyInfo();

                lobbyInfo.ownerName = lobby.Name;
                lobbyInfo.playerCount = lobby.Players.Count;
                lobbyInfo.maxPlayerCount = lobby.MaxPlayers;
                lobbyInfo.ownerLocation = "Hideout";

                lobbyInfo.code = lobby.Id;

                lobbies.Add(lobbyInfo);

                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }

            return lobbies;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        return null;
    }

    [Command]
    public async void JoinLobbyById(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByIdOptions);
            joinedLobby = lobby;

            gameRelay.JoinRelay(lobby.Data["RelayCode"].Value);

            Debug.Log("Joined Lobby with Id " + lobbyId);

            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [Command]
    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions {
                Player = GetPlayer()
            };
            
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby;

            Debug.Log("Joined Lobby with code " + lobbyCode);

            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    [Command]
    private async void QuickJoinLobby()
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions();

            await LobbyService.Instance.QuickJoinLobbyAsync();
        } catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            //Id = AuthenticationService.Instance.PlayerId,
            Data = new Dictionary<string, PlayerDataObject> {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }

    [Command]
    private void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }

    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in Lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value + " " + lobby.Data["Map"].Value);
        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }

    private async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
                }
            });

            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);
        } catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            playerName = newPlayerName;
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                }
            });

        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async void UpdateLobbyRelayCode(string relayCode)
    {
        try
        {
            // Should this be smashing hostLobby instead?

            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });

            joinedLobby = lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
}
