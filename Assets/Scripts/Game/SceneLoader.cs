using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    
    public enum Scene
    {
        MainMenuScene,
        LobbyScene,
        GameScene
    }

    public static void Load(Scene targetScene)
    {
        SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static void LoadNetwork(Scene targetScene)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }    

    public static void NewGame()
    {
        LoadNetwork(Scene.GameScene);
    }

    public static void MainMenu()
    {
        if (CrosshairUI.Instance != null)
            CrosshairUI.Instance.Hide(); // Cursor'u ortaya çıkartmak için

        NetworkManager.Singleton.Shutdown();
        MultiplayerManager.Instance.CleanUp();

        Load(Scene.MainMenuScene);
    }
	
}
