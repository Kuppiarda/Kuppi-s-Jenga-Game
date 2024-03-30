using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Player : NetworkBehaviour
{
    
    // Singleton
    public static Player LocalInstance { get; private set; }

    // Player Rotation
	[SerializeField] private Transform playerCameraHead;

    // Chair
    private TableChair sittingChair;
    private float chairVerticalOffset = 1f;

    // Controller
    private PlayerController playerController;
    private bool isPlayerFrozen;

    // Player Visual
    [SerializeField] private Transform playerVisualHolder;
    private PlayerVisual currentPlayerVisual;
    
    // Name Plate
    [SerializeField] private TextMeshPro playerNameText;


    private void Start()
    {
        if (!IsOwner) return;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        transform.position = new Vector3(0, 1, 0);
        LocalInstance = this;
        playerController = GetComponent<PlayerController>();        
    }


    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (TryGetTableChairFromRaycast(out TableChair tableChair) && !sittingChair && JengaTable.Instance.GetTableState() != JengaTable.TableState.Fall)
        {
            if (!tableChair.IsChairEmpty()) return;
            SitChair(tableChair);
        }
        else if (sittingChair)
        {
            GetUpChair();
        }
    }

    private bool TryGetTableChairFromRaycast(out TableChair tableChair)
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(GameInput.Instance.GetMousePosition());
        if (Physics.Raycast(cameraRay, out RaycastHit hit, 10, JengaGameManager.Instance.GetChairLayerMask()))
        {
            tableChair = hit.transform.parent.GetComponent<TableChair>();
            return true;
        }
        else
        {
            tableChair = null;
            return false;
        }
    }

    public bool IsPlayerSitting()
    {
        return sittingChair;
    }

    private void SitChair(TableChair tableChair)
    {
        FreezePlayer();
        CrosshairUI.Instance.Hide();
        ChangeChairServerRpc(tableChair.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeChairServerRpc(NetworkObjectReference tableChairNetworkObjectReference = new NetworkObjectReference(), ServerRpcParams serverRpcParams = default)
    {
        ChangeChairClientRpc(tableChairNetworkObjectReference, NetworkManager.Singleton.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject);
    }

    [ClientRpc]
    private void ChangeChairClientRpc(NetworkObjectReference tableChairNetworkObjectReference, NetworkObjectReference playerObjectReference)
    {
        playerObjectReference.TryGet(out NetworkObject playerNetworkObject);
        Player player = playerNetworkObject.GetComponent<Player>();

        if (player.GetSittingChair()) // Eğer oyuncu oturuyorsa
        {
            player.GetSittingChair().GetUpFromThisChair(); // Kaldır
            player.SetSittingChair(null);
        }
        else // Eğer oyuncu oturmuyorsa
        {
            tableChairNetworkObjectReference.TryGet(out NetworkObject chairNetworkObject);
            TableChair tableChair = chairNetworkObject.gameObject.GetComponent<TableChair>();
            player.SetSittingChair(tableChair);
            tableChair.SitThisChair();

            if (!IsOwner) return;
            currentPlayerVisual.ResetPlayerVisualRotation(); // Visual dönüş sıfırlama
            playerController.TeleportPlayer(sittingChair.transform.position + Vector3.up * chairVerticalOffset); // Doğru konum için ayar
            transform.rotation = tableChair.transform.rotation;                    
        }

    }

    private void SetSittingChair(TableChair newSittingChair)
    {
        sittingChair = newSittingChair;
    }

    public void GetUpChair()
    {
        UnfreezePlayer();
        playerController.TeleportPlayer(sittingChair.transform.right + Vector3.up);
        CrosshairUI.Instance.Show();
        ChangeChairServerRpc();
    }

    public TableChair GetSittingChair()
    {
        return sittingChair;
    } 


    public void SetPlayerCameraHeadRotationSmoothly(float newHeadRotation) // Ending, cutscene için
    {
        playerController.SetPlayerHeadRotationSmoothly(newHeadRotation);
    }


    public void SetPlayerVisualHeadRotation(float verticalRotation)
    {
        currentPlayerVisual.SetPlayerVisualHeadVerticalRotation(verticalRotation);
    }

    public void SetPlayerVisualHeadRotationSmoothly(float verticalRotation)
    {
        currentPlayerVisual.SetPlayerVisualHeadVerticalRotationSmoothly(verticalRotation);
    }    



    public Transform GetPlayerCameraHead()
    {
        return playerCameraHead;
    }   
	
    public PlayerVisual GetPlayerVisual()
    {
        return currentPlayerVisual;
    }

    public void SetPlayerSkin(int skinId)
    {
        DestroyPlayerVisualGameObject();
        currentPlayerVisual = Instantiate(MultiplayerManager.Instance.GetPlayerSkinGameObjectFromSkinId(skinId), playerVisualHolder).GetComponent<PlayerVisual>();                        
        SetPlayerAvatarInAnimator(currentPlayerVisual.GetSkinAvatar());
    }

    public void SetPlayerName(string playerName)
    {
        if (LocalInstance == this) return;
        playerNameText.text = playerName;
    }    

    private void DestroyPlayerVisualGameObject()
    {
        foreach (Transform playerVisual in playerVisualHolder.GetComponentInChildren<Transform>()) // Tüm çocuklara erişmesi için
        {
            Destroy(playerVisual.gameObject);
        }
    }

    private void SetPlayerAvatarInAnimator(Avatar avatar)
    {
        transform.GetComponent<PlayerAnimationController>().SetAnimatorAvatar(avatar); // PlayerVisualHolder'ın avatarına yeni eklenen playerVisual avatarı atanıyor
    }



    public void FreezePlayer()
    {
        isPlayerFrozen = true;
    }
    
    public void UnfreezePlayer()
    {
        isPlayerFrozen = false;
    }

    public bool IsPlayerFrozen()
    {
        return isPlayerFrozen;
    }

}
