using System;
using Unity.Netcode;
using UnityEngine;

public class JengaGameEnding_Scale : BaseJengaGameEnding
{
    
	// ActivePlayer olan Karakter yavaşça küçülüp üstüne jenga düşecek, ses çıkacak, scene yenilenecek
    private Player targetPlayer;
    private Vector3 targetScale = Vector3.one * 0.3f;
    private float shrinkSpeed = 2f;
    private GameObject spawnedJenga;
    [SerializeField] private AudioClip jengaCrushSound;
    [SerializeField] private AudioClip horrorSound;

    private float fallingJengaVerticalVelocityLastFrame; 

    private enum Phase
    {
        Shrink,
        SpawnJenga,
        JengaFall,
        WaitForNextRound
    }
    private Phase phase;


    private void Awake()
    {
        targetPlayer = JengaGameMultiplayerManager.Instance.GetLastTouchedPlayer();
        phase = Phase.Shrink;
    }

    private void Update()
    {

        if (!isEndingStarted) 
        {
            CheckForDelay();    

            if (isEndingStarted)
                SoundManager.Instance.PlaySound(horrorSound, targetPlayer.transform.position + targetPlayer.transform.up * 2);

            return;
        }

        if (!IsServer) return;

        switch (phase)
        {
            case Phase.Shrink:
                ShrinkPlayer();
                break;
            case Phase.SpawnJenga:
                SpawnJenga();
                break;
            case Phase.JengaFall:
                CheckForFallingJenga();
                break;
            case Phase.WaitForNextRound:
                WaitForNextRound();
                break;                
        }

    }

    private void CheckForFallingJenga()
    {
        float verticalVelocity = spawnedJenga.GetComponent<Rigidbody>().velocity.y; // Eğer dikey hızı son kareden beri yavaşlamışsa bir şeye çarpmıştır bu yüzden turu otomatik bitiriyoruz
        if (Vector3.Distance(spawnedJenga.transform.position, targetPlayer.transform.position) <= 0.5f || verticalVelocity > fallingJengaVerticalVelocityLastFrame)
        {
            WaitForEndingClientRpc(spawnedJenga.transform.position);
            phase = Phase.WaitForNextRound;
        }
        fallingJengaVerticalVelocityLastFrame = verticalVelocity;
    }

    [ClientRpc]
    private void WaitForEndingClientRpc(Vector3 position)
    {
        SoundManager.Instance.PlaySound(jengaCrushSound, position);
        ShowWaitForEndingUI();
    }

    private void SpawnJenga()
    {
        spawnedJenga = JengaTable.Instance.SpawnJengaGameObject(targetPlayer.transform.position + targetPlayer.transform.up * 3, Quaternion.identity, null);
        phase = Phase.JengaFall;
    }

    private void ShrinkPlayer()
    {
        Vector3 playerScale = targetPlayer.transform.localScale;
        if (Vector3.Distance(playerScale, targetScale) <= 0.2f)
        {
            phase = Phase.SpawnJenga;
            return;
        }
        
        JengaGameMultiplayerManager.Instance.SetPlayerVisualHeadRotationSmoothlyClientRpc(targetPlayer.NetworkObject, -90f); // Target, herkeste yukarı bakacak
        ShrinkPlayerClientRpc(targetPlayer.NetworkObject);
    }

    [ClientRpc]
    private void ShrinkPlayerClientRpc(NetworkObjectReference targetPlayerNetworkObjectReference)
    {
        targetPlayerNetworkObjectReference.TryGet(out NetworkObject targetPlayerNetworkObject);
        Player targetPlayer = targetPlayerNetworkObject.GetComponent<Player>();
        if (targetPlayer == Player.LocalInstance)
        {
            targetPlayer.SetPlayerCameraHeadRotationSmoothly(-90f);
            targetPlayer.transform.localScale = Vector3.Lerp(targetPlayer.transform.localScale, targetScale, Time.deltaTime * shrinkSpeed);
            if (!Player.LocalInstance.IsPlayerFrozen())
                Player.LocalInstance.FreezePlayer();
        }
    }
	
}
