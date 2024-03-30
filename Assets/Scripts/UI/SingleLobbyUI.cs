using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class SingleLobbyUI : MonoBehaviour
{
    
	[SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    private Lobby lobby;


    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            LobbyServices.Instance.JoinWithId(lobby.Id);
        });
    }

    private void SetLobbyProperties(string lobbyName, int playerCount)
    {
        lobbyNameText.text = lobbyName;
        playerCountText.text = playerCount + "/4";
    }

    public void SetLobby(Lobby lobby)
    {
        this.lobby = lobby;
        SetLobbyProperties(lobby.Name, lobby.Players.Count);
    }
	


}
