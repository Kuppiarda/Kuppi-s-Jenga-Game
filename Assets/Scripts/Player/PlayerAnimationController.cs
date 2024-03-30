using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerAnimationController : NetworkBehaviour
{
    
	[SerializeField] private Animator playerVisualAnimator;
    private PlayerController playerController;


    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerController = Player.LocalInstance.GetComponentInParent<PlayerController>();
        playerController.OnPlayerStateChanged += PlayerController_OnPlayerStateChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;
        SetBool("IsMoving", playerController.IsPlayerRunning());
        SetBool("IsSitting", playerController.GetComponentInParent<Player>().IsPlayerSitting());
    }

    private void PlayerController_OnPlayerStateChanged(object sender, PlayerController.OnPlayerStateChangedEventArgs e)
    {
        SetBoolOnly("Is" + e.newPlayerState.ToString());
    }

    private void SetBool(string name, bool value)
    {
        playerVisualAnimator.SetBool(name, value);
    }

    public void SetAnimatorAvatar(Avatar avatar)
    {
        playerVisualAnimator.avatar = avatar;
    }

    // Multiplayerda trigger sorunlu olduğu için triggerlar artık bool olarak görev alıyor
    private void SetBoolOnly(string setBoolName) 
    {
        foreach (string _boolName in Enum.GetNames(typeof(PlayerController.PlayerState)))
        {
            string boolName = "Is" + _boolName;
            if (boolName == setBoolName)
            {
                SetBool(boolName, true);
            }
            else
            {
                SetBool(boolName, false);
            }
        }
    }


}
