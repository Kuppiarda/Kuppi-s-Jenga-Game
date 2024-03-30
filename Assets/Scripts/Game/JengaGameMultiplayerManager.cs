using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JengaGameMultiplayerManager : NetworkBehaviour
{
    
	public static JengaGameMultiplayerManager Instance { get; private set; }

    [SerializeField] private Transform playerPrefab;

    private NetworkVariable<ulong> activePlayerClientId = new NetworkVariable<ulong>(0);
    private Player activePlayer;
    private Quaternion activePlayerCameraHeadLocalRotation;
    private NetworkVariable<ulong> lastTouchedPlayerClientId = new NetworkVariable<ulong>(0);
    private Player lastTouchedPlayer;    
    private bool newGameNextFrame;


    private void Awake()
    {
        Instance = this;        
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }

    public override void OnDestroy() // Eğer atadığımız methodları eventlardan çıkarmazsak çakışır
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
        }        
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab, new Vector3(0, 2, 1 - 0.5f * MultiplayerManager.Instance.GetPlayerDataIndexFromClientId(clientId)), Quaternion.Euler(0, 90, 0));
            NetworkObject playerNetworkObject = playerTransform.GetComponent<NetworkObject>();

            playerNetworkObject.SpawnAsPlayerObject(clientId, true);

            SetPlayerPropertiesClientRpc(MultiplayerManager.Instance.GetPlayerSkinIdFromClientId(clientId), MultiplayerManager.Instance.GetPlayerNameFromClientId(clientId), playerNetworkObject);

        }
        JengaTable.Instance.SpawnTableJengaCreator(); // TableJengaCreator çağırılması
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {

        // Host ise menüye dönüş işi MultiplayerManager'da atanıyor

        if (IsServer)
        {
            if (GetActivePlayerClientId() == clientId)
            {
                if (JengaTable.Instance.GetTableState() != JengaTable.TableState.Select)
                {
                    newGameNextFrame = true; // NextFrame olmaz ise SceneManager timeout süresini bekliyor(NGO bug)
                }

                if (GetActivePlayer().IsPlayerSitting())
                {
                    GetUpActivePlayerClientRpc();
                }

                SelectNextActivePlayerServerRpc();
            }
            else if (GetLastTouchedPlayer() != null && GetLastTouchedPlayer().OwnerClientId == clientId && JengaTable.Instance.GetTableState() == JengaTable.TableState.Fall)
            {
                newGameNextFrame = true;                    
            }

        }
    }

    private void Update()
    {
        if (newGameNextFrame)
            SceneLoader.NewGame();
    }

    // Skins

    [ClientRpc]
    private void SetPlayerPropertiesClientRpc(int skinId, string playerName, NetworkObjectReference playerNetworkObjectReference)
    {
        playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject); 
        Player player = playerNetworkObject.gameObject.GetComponent<Player>(); // Aktarılan referanstan player alınıyor

        player.SetPlayerSkin(skinId); // Alınan player'a skinId atanıyor
        player.SetPlayerName(playerName);
    }
    
    // PlayerVisualHead

    [ClientRpc]
    public void SetPlayerVisualHeadRotationSmoothlyClientRpc(NetworkObjectReference playerNetworkObjectReference, float rotation)
    {
        playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject);
        Player player = playerNetworkObject.gameObject.GetComponent<Player>();
        player.GetPlayerVisual().SetPlayerVisualHeadVerticalRotationSmoothly(rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerVisualHeadRotationServerRpc(float newHeadRotation, ServerRpcParams serverRpcParams = default)
    {
        NetworkObjectReference player = NetworkManager.Singleton.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject;
        SetPlayerVisualHeadRotationClientRpc(player, newHeadRotation);
    }    

    [ClientRpc]
    private void SetPlayerVisualHeadRotationClientRpc(NetworkObjectReference playerNetworkObjectReference, float newHeadRotation)
    {
        playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject);
        playerNetworkObject.GetComponent<Player>().SetPlayerVisualHeadRotation(newHeadRotation); // Visual Head
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerVisualHeadRotationWithPositionServerRpc(Vector3 hitPosition, ServerRpcParams serverRpcParams = default)
    {
        SetPlayerVisualHeadRotationWithPositionClientRpc(hitPosition, NetworkManager.Singleton.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject);
    }

    [ClientRpc]
    private void SetPlayerVisualHeadRotationWithPositionClientRpc(Vector3 hitPosition, NetworkObjectReference playerNetworkObjectReference)
    {
        playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject);
        Player player = playerNetworkObject.gameObject.GetComponent<Player>();
        player.GetPlayerVisual().SetPlayerVisualHeadRotationWithPosition(hitPosition);
    }    

    // Active Player Camera Head
    [ServerRpc(RequireOwnership = false)]
    public void SetActivePlayerCameraHeadLocalRotationServerRpc(Quaternion localRotation)
    {
        activePlayerCameraHeadLocalRotation = localRotation;
        GetActivePlayerCameraHeadLocalRotationClientRpc(activePlayerCameraHeadLocalRotation);
    }

    [ClientRpc]
    private void GetActivePlayerCameraHeadLocalRotationClientRpc(Quaternion rotation)
    {
        if (GetActivePlayer() && GetActivePlayer().GetPlayerCameraHead() && Player.LocalInstance != GetActivePlayer())
        {
            GetActivePlayer().GetPlayerCameraHead().localRotation = rotation;
        }
    }

    // Active player

    [ServerRpc(RequireOwnership = false)]
    public void SelectNextActivePlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        bool selectNextPlayer = false;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (activePlayerClientId.Value == clientId) // Eğer clientId'deki bu oyuncu aktif oyuncu ise
            {
                selectNextPlayer = true;
            }
            else if (selectNextPlayer)
            {
                activePlayerClientId.Value = clientId;
                selectNextPlayer = false;
            }
        }

        if (selectNextPlayer)
        {
            activePlayerClientId.Value = NetworkManager.Singleton.ConnectedClientsIds[0]; // Eğer sonuncu seçilmişse başa atması için 0. index
        }
    }

    private Player GetPlayerWithClientId(ulong clientId)
    {
        NetworkObject playerNetworkObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        Player player = playerNetworkObject.gameObject.GetComponent<Player>();
        return player;
    }

    public ulong GetActivePlayerClientId()
    {
        return activePlayerClientId.Value;
    }

    public Player GetActivePlayer()
    {
        if (!activePlayer || GetActivePlayerClientId() != activePlayer.GetComponent<NetworkObject>().OwnerClientId)
            GetActivePlayerServerRpc();
        return activePlayer;
    }

    [ClientRpc]
    private void GetUpActivePlayerClientRpc()
    {
        GetActivePlayer().GetSittingChair().GetUpFromThisChair();
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetActivePlayerServerRpc()
    {
        NetworkObjectReference activePlayerNetworkObjectReference = GetPlayerWithClientId(GetActivePlayerClientId()).NetworkObject;
        GetActivePlayerClientRpc(activePlayerNetworkObjectReference);
    }

    [ClientRpc]
    private void GetActivePlayerClientRpc(NetworkObjectReference activePlayerNetworkObjectReference)
    {
        activePlayerNetworkObjectReference.TryGet(out NetworkObject activePlayerNetworkObject);
        Player currentActivePlayer = activePlayerNetworkObject.gameObject.GetComponent<Player>();
        activePlayer = currentActivePlayer;
    }

    public void SetActivePlayerClientId(ulong clientId)
    {
        activePlayerClientId.Value = clientId;
    }

    public bool IsActivePlayer()
    {
        return NetworkManager.Singleton.LocalClientId == GetActivePlayerClientId();
    }

    // LastTouched Player

    [ServerRpc(RequireOwnership = false)]
    public void SetActivePlayerAsLastTouchedPlayerServerRpc()
    {
        lastTouchedPlayerClientId.Value = GetActivePlayerClientId();
        SetLastTouchedPlayerServerRpc();
    }

    private ulong GetLastTouchedPlayerClientId()
    {
        return lastTouchedPlayerClientId.Value;
    }

    public Player GetLastTouchedPlayer()
    {
        return lastTouchedPlayer;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetLastTouchedPlayerServerRpc()
    {
        NetworkObject lastTouchedPlayerNetworkObject = NetworkManager.Singleton.ConnectedClients[lastTouchedPlayerClientId.Value].PlayerObject;
        SetLastTouchedPlayerClientRpc(lastTouchedPlayerNetworkObject);
    }    

    [ClientRpc]
    private void SetLastTouchedPlayerClientRpc(NetworkObjectReference lastTouchedPlayerNetworkObjectReference)
    {
        lastTouchedPlayerNetworkObjectReference.TryGet(out NetworkObject lastTouchedPlayerNetworkObject);
        lastTouchedPlayer = lastTouchedPlayerNetworkObject.gameObject.GetComponent<Player>();
    }

}
