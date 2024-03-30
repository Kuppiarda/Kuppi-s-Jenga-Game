using System;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    
    // Butonlar
	[SerializeField] private Button resumeGameButton;
    [SerializeField] private Button mainMenuButton;


    private void GamePauseUI_OnGamePauseTriggered(object sender, EventArgs e)
    {
        if (JengaGameManager.Instance.IsGamePaused())
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Start()
    {
        JengaGameManager.Instance.OnPauseGameTriggered += GamePauseUI_OnGamePauseTriggered;

        resumeGameButton.onClick.AddListener(() => {
            JengaGameManager.Instance.TriggerPauseGame();
        });

        mainMenuButton.onClick.AddListener(() => {
            SceneLoader.MainMenu();
        });   

        Hide();
    }

    private void Hide()
    {
        CrosshairUI.Instance.Show();
        SettingsUI.Instance.HideSettings();
        gameObject.SetActive(false);
    }

    private void Show()
    {
        CrosshairUI.Instance.Hide();
        gameObject.SetActive(true);
    }
	
}
