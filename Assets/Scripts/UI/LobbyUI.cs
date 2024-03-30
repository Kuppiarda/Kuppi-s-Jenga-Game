using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    
	[SerializeField] private Button mainMenuButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button changeSkinButton;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    
    private void Awake()
    {

        if (!NetworkManager.Singleton.IsHost)
            startGameButton.gameObject.SetActive(false);
            
        mainMenuButton.onClick.AddListener(() => {
            LobbyServices.Instance.LeaveLobby(); // Sadece lobi
            SceneLoader.MainMenu();
        });
        startGameButton.onClick.AddListener(() => {
            LobbyServices.Instance.DeleteLobby();
            SceneLoader.NewGame();
        });
        changeSkinButton.onClick.AddListener(() => {
            MultiplayerManager.Instance.ChangePlayerSkin();
        });
    }

    private void Start()
    {
        lobbyCodeText.text += LobbyServices.Instance.GetCurrentLobby().LobbyCode;
    }

}
