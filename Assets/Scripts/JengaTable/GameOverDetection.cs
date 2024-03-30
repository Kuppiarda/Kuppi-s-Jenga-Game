using System;
using UnityEngine;

public class GameOverDetection : MonoBehaviour
{
    
    private void OnTriggerEnter(Collider collider)
    {
        if (!JengaGameMultiplayerManager.Instance.IsActivePlayer()) return;
        if (collider.gameObject.layer == (int) Math.Log(JengaGameManager.Instance.GetJengaLayerMask(), 2) && JengaTable.Instance.IsGameStarted())
        {
            JengaTable.Instance.PlaceSelectedJenga();
            JengaTable.Instance.ChangeTableStateServerRpc(JengaTable.TableState.Fall);
        }
    }
	
	
}
