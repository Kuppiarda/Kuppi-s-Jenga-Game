using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour
{

    public static LobbyListUI Instance { get; private set; }
    
    [SerializeField] private Button mainMenuButton;
	[SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinWithCodeButton;
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;

    [SerializeField] private Transform lobbyListTransform;
    [SerializeField] private Transform lobbyTemplateTransform;


    private void Awake()
    {
        Instance = this;

        createLobbyButton.onClick.AddListener(() => {
            CreateLobbyUI.Instance.Show();
        });
        mainMenuButton.onClick.AddListener(() => {
            Hide();
            CreateLobbyUI.Instance.Hide();
        });
        joinWithCodeButton.onClick.AddListener(() => {
            LobbyServices.Instance.JoinWithCode(lobbyCodeInputField.text);
        });

        lobbyTemplateTransform.gameObject.SetActive(false);
        Hide();
    }

    private void Start()
    {
        playerNameInputField.text = MultiplayerManager.Instance.GetPlayerName();
        playerNameInputField.onValueChanged.AddListener((string newPlayerName) => {
            MultiplayerManager.Instance.SetPlayerName(newPlayerName);
        });


        UpdateLobbyList(new List<Lobby>());
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
	
    public void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach (Transform childTransform in lobbyListTransform)
        {
            if (childTransform == lobbyTemplateTransform) continue;
            Destroy(childTransform.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            Transform lobbyTransform = Instantiate(lobbyTemplateTransform, lobbyListTransform);
            lobbyTransform.gameObject.SetActive(true);
            lobbyTransform.GetComponent<SingleLobbyUI>().SetLobby(lobby);
        }
    }

}
