using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseJengaGameEnding : NetworkBehaviour
{
    
    private protected float delayForEnding = 2f;
    private protected bool isEndingStarted;
    private protected float restartTimer = 3f;


    private protected void CheckForDelay()
    {
        delayForEnding -= Time.deltaTime;
        if (delayForEnding <= 0)
        {
            isEndingStarted = true;
        }
    }

    private protected void ShowWaitForEndingUI()
    {
        JengaTable.Instance.TriggerOnGameEnding();
    }

	public override void OnDestroy()
    {
        if (IsServer && NetworkManager.Singleton)
            SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);
    }

    private protected void WaitForNextRound()
    {
        restartTimer -= Time.deltaTime;
        if (restartTimer <= 0f)
        {
            Destroy(this);
        }
    }
	
}
