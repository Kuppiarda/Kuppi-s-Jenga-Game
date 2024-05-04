using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobbyUI : MonoBehaviour
{

    public static CreateLobbyUI Instance { get; private set; }
    
	[SerializeField] private Button createLobbyButton;
    [SerializeField] private Button cancelLobbyButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;


    private void Awake()
    {
        Instance = this;

        createLobbyButton.onClick.AddListener(() => {
            if (lobbyNameInputField.text == "")
            {
                NotificationUI.Instance.LocalizedNotification("noLobbyNameNotification");
            }
            else
            {
                LobbyServices.Instance.CreateLobby(lobbyNameInputField.text);
                PlayerPrefs.SetString("LobbyName", lobbyNameInputField.text);
                PlayerPrefs.Save();
            }
        });

        cancelLobbyButton.onClick.AddListener(() => {
            Hide();
        });

        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        lobbyNameInputField.text = PlayerPrefs.GetString("LobbyName", "");
    }

    public void Hide()
    {
        lobbyNameInputField.text = "";
        gameObject.SetActive(false);
    }
	
}
