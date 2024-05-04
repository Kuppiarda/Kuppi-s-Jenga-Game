using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System;

public class LobbyServices : MonoBehaviour
{
    
	public static LobbyServices Instance { get; private set; }
    private Lobby currentLobby;
    private List<Lobby> lobbyList = new List<Lobby>();
    private float lobbyListTimer = 9999f; // Düşük olursa menü tuşuna basıldığı an oluşan güncellemede RateLimit Exceptionu veriyor
    private float keepActiveTimer;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);

        InitializeAuthenticationService();
    }

    private void Update()
    {
        KeepLobbyActive();
        UpdateLobbies();
    }

    private void UpdateLobbies()
    {
        if (currentLobby != null || !AuthenticationService.Instance.IsSignedIn || SceneManager.GetActiveScene().name != SceneLoader.Scene.MainMenuScene.ToString()) return;

        lobbyListTimer -= Time.deltaTime;
        if (lobbyListTimer <= 0f)
        {
            lobbyListTimer = 1.5f; // Her saniye lobiler yenilenecek
            FetchLobbies();
            LobbyListUI.Instance.UpdateLobbyList(lobbyList);
        }
    }

    private void KeepLobbyActive()
    {
        if (IsHost())
        {
            keepActiveTimer -= Time.deltaTime;
            if (keepActiveTimer <= 0)
            {
                keepActiveTimer = 8f; // 8 Saniyede bir aktiflik pingi yollayacak
                LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    private bool IsHost()
    {
        return currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void InitializeAuthenticationService()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized) return; // Eğer unity servislerine zaten bağlanmamışsa

        InitializationOptions initializationOptions = new InitializationOptions();

        await UnityServices.InitializeAsync(initializationOptions); // Unity servisleriyle bağlantı kur
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); 
    }

    public async void CreateLobby(string lobbyName)
    {
        if (currentLobby != null) return;
        try
        {
            currentLobby = new Lobby(); // Bir lobi oluşturuluyor olduğunu göstermesi için
            NotificationUI.Instance.LocalizedNotification("creatingLobbyNotification");

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MultiplayerManager.MAX_PLAYER_COUNT); // Lobi servisinden lobi kur

            Allocation allocation = await RelayCreateAllocation(); // Relayden yer ayırt
            string relayJoinCode = await GetRelayJoinCode(allocation); // Ayırtılan serverın join kodunu al
            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) } // Relayin Join Code'u Lobbydekiler ile paylaştırılıyor
                }
            });
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls")); // Ayırtılan yeri unity transport serverı olarak belirle

            MultiplayerManager.Instance.StartHost(); // Networkmanagerdan hostu başlat
            SceneLoader.LoadNetwork(SceneLoader.Scene.LobbyScene); // Lobi ekranına gir
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
            currentLobby = null;
        }
    }

    public async void JoinWithCode(string lobbyCode)
    {
        try
        {
            NotificationUI.Instance.LocalizedNotification("joiningWithCodeNotification", parameters: new List<String> { lobbyCode });
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value; // Lobbyden RelayJoinCode alınıyor
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode); // Kod ile Relay'e giriliyor(Veri aktarımı için)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls")); // UnityTransport'ta Relay serverı seçiliyor

            MultiplayerManager.Instance.StartClient();
        }
        catch
        {
            NotificationUI.Instance.LocalizedNotification("checkLobbyCodeNotification");
        }
    }

    public async void JoinWithId(string lobbyId)
    {
        try
        {
            NotificationUI.Instance.LocalizedNotification("connectingNotification");
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            string relayJoinCode = currentLobby.Data["RelayJoinCode"].Value; // Lobbyden RelayJoinCode alınıyor
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode); // Kod ile Relay'e giriliyor(Veri aktarımı için)
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls")); // UnityTransport'ta Relay serverı seçiliyor

            MultiplayerManager.Instance.StartClient();
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
        }
    }
	    

    public Lobby GetCurrentLobby()
    {
        return currentLobby;
    }

    public async void DeleteLobby()
    {
        if (currentLobby == null) return;
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            currentLobby = null;
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
        }

    }

    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            currentLobby = null;
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
        }
    }

    public async void KickPlayer(string playerId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId); // Bunu çalıştıracak butonu sadece host görebiliyor
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
        }
    }    

    private async void FetchLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions { // Çekilecek lobi ayarları
                Filters = new List<QueryFilter> { // Lobi filtreleri
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT), // 0'dan fazla kişi varsa göster
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "4", QueryFilter.OpOptions.LT) // 4'ten az kişi varsa göster
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            lobbyList = queryResponse.Results;
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
        }
    }

    public void ResetListTimer()
    {
        lobbyListTimer = 0f;
    }

    private async Task<Allocation> RelayCreateAllocation()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MultiplayerManager.MAX_PLAYER_COUNT - 1);
            return allocation;
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
            return default;
        }
    }    

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        }
        catch
        {
            NotificationUI.Instance.ErrorNotification();
            return default;
        }
    }

}
