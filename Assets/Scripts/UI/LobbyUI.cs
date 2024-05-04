using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    
	[SerializeField] private Button mainMenuButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button changeSkinButton;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private LocalizedString lobbyCodeTextLocalizedString;

    
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

    private void OnEnable()
    {
        lobbyCodeTextLocalizedString.StringChanged += UpdateLobbyCodeText;
    }

    private void OnDisable()
    {
        lobbyCodeTextLocalizedString.StringChanged -= UpdateLobbyCodeText;
    }

    private void UpdateLobbyCodeText(string localizedLobbyCodeText)
    {
        lobbyCodeText.text = string.Format(localizedLobbyCodeText, LobbyServices.Instance.GetCurrentLobby().LobbyCode);
    }

}
