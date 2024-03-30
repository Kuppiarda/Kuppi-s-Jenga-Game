using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    
	[SerializeField] private Button playGameButton;
    [SerializeField] private Button exitGameButton;


    private void Awake()
    {
        playGameButton.onClick.AddListener(() => {            
            LobbyServices.Instance.ResetListTimer();
            LobbyListUI.Instance.Show();
        });
        exitGameButton.onClick.AddListener(() => {            
            Application.Quit();
        });        
        CleanUp();
    }

    private void CleanUp()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (MultiplayerManager.Instance != null)
        {
            Destroy(MultiplayerManager.Instance.gameObject);
        }

        if (LobbyServices.Instance != null)
        {
            Destroy(LobbyServices.Instance.gameObject);
        }
    }
	
}
