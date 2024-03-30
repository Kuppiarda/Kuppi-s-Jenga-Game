using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class MultiplayerManager : NetworkBehaviour
{

    public static MultiplayerManager Instance { get; private set; }
    public const int MAX_PLAYER_COUNT = 4;

    private NetworkList<PlayerData> playerDataNetworkList;
    public event EventHandler OnPlayerDataNetworkListChanged;
    private string playerName;

    [SerializeField] private List<PlayerVisual> playerSkinList;
    private int ownPlayerSkinId; // 
    

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);
        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;

        ownPlayerSkinId = PlayerPrefs.GetInt("PlayerSkin", 0);

        playerName = PlayerPrefs.GetString("PlayerName", "Oyuncu " + UnityEngine.Random.Range(0, 10000).ToString());
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CleanUp()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
        Destroy(gameObject);
    }    

	public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Server_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
        SetPlayerVariablesServerRpc(GetPlayerName(), AuthenticationService.Instance.PlayerId, ownPlayerSkinId);
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }

    private void NetworkManager_Server_OnClientConnectedCallback(ulong clientId)
    {
        playerDataNetworkList.Add(new PlayerData {
            clientId = clientId,
        });
    }

    public void StartClient()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId)
    {
        SetPlayerVariablesServerRpc(GetPlayerName(), AuthenticationService.Instance.PlayerId, ownPlayerSkinId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerVariablesServerRpc(string playerName, string playerId, int skinId, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        PlayerData playerData = playerDataNetworkList[playerDataIndex];
        playerData.playerName = playerName;
        playerData.playerId = playerId;
        playerData.skinId = skinId;
        playerDataNetworkList[playerDataIndex] = playerData; // Tekrar atıyoruz(değiştirilemiyor)
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        string reason = NetworkManager.Singleton.DisconnectReason;

        if (reason == "")
        {
            reason = "Oyuna bağlanılamadı";
        }

        if (NotificationUI.Instance != null)
            NotificationUI.Instance.Notification(reason);

        if (NetworkManager.ServerClientId == clientId && SceneManager.GetActiveScene().name != SceneLoader.Scene.MainMenuScene.ToString())
            SceneLoader.MainMenu(); // kicklendiyse menüye dönsün
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {

        if (connectionApprovalRequest.ClientNetworkId == OwnerClientId)
        {
            connectionApprovalResponse.Approved = true;
            return;
        }

        if (SceneManager.GetActiveScene().name != SceneLoader.Scene.LobbyScene.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Bu oyun zaten başladı";
            return;
        }
        else if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_COUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Maksimum kişi sayısına ulaşıldı";            
            return;
        }

            connectionApprovalResponse.Approved = true;
    }

    public bool IsPlayerIndexConnected(int playerIndex)
    {
        return playerIndex < playerDataNetworkList.Count;
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex)
    {
        return playerDataNetworkList[playerIndex];
    }

    public int GetPlayerSkinIdFromClientId(ulong clientId)
    {
        return playerDataNetworkList[GetPlayerDataIndexFromClientId(clientId)].skinId;
    }

    public string GetPlayerNameFromClientId(ulong clientId)
    {
        return playerDataNetworkList[GetPlayerDataIndexFromClientId(clientId)].playerName.ToString();
    }    

    public GameObject GetPlayerSkinGameObjectFromSkinId(int skinId)
    {
        return playerSkinList[skinId].gameObject;
    }

    public int GetSkinCount()
    {
        return playerSkinList.Count;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
                return i;
        }
        return default;
    }

    public void ChangePlayerSkin()
    {
        ownPlayerSkinId = (ownPlayerSkinId >= 0 && ownPlayerSkinId < GetSkinCount() - 1) ? ++ownPlayerSkinId : 0; // Eğer ownPlayerSkinId 0 dan büyük eşitse ve skin idsinden 1 azsa +1 arttırarak yeni id belirle yoksa 0

        PlayerPrefs.SetInt("PlayerSkin", ownPlayerSkinId);
        PlayerPrefs.Save();

        ChangePlayerSkinServerRpc(ownPlayerSkinId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerSkinServerRpc(int skinId, ServerRpcParams serverRpcParams = default)
    {
        int lobbyPlayerIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        PlayerData playerData = playerDataNetworkList[lobbyPlayerIndex]; // Doğrudan erişilemiyor, tekrar atama gerekiyor
        playerData.skinId = skinId;
        playerDataNetworkList[lobbyPlayerIndex] = playerData;
    }

    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId, "Kick");
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }

}
