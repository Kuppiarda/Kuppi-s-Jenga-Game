using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class TableJengaCreator_Pop : BaseTableJengaCreator
{

    // Jenga spawn
    private int lastPlacedJengaColumn;
    private int lastPlacedJengaRow;

    // Pop
    [SerializeField] private AudioClip jengaPopSound;
    private float jengaPopTimer = 3f;

    
    private void Update()
    {

        if (!IsServer) return;

        jengaPopTimer -= Time.deltaTime;
        if (jengaPopTimer <= 0 && isJengaSpawning)
        {
            SpawnAllJengas();
            HandlePopVisualEffect();
            PlayPopSound();
            Destroy(gameObject, 3f);
        }

    }

    private void PlayPopSound()
    {
        PlayPopSoundClientRpc(transform.position);
    }

    [ClientRpc]
    private void PlayPopSoundClientRpc(Vector3 position)
    {
        SoundManager.Instance.PlaySound(jengaPopSound, position);
    }

    private void HandlePopVisualEffect()
    {
        float jengaTowerVerticalPosition = singleJengaSize * lastPlacedJengaRow * 3f/5;
        transform.localPosition += new Vector3(0, jengaTowerVerticalPosition / 2, 0f); // Kulenin yarısı kadar yukarıya çıkacak
        HandlePopVisualEffectClientRpc();
    }

    [ClientRpc]
    private void HandlePopVisualEffectClientRpc()
    {
        GetComponent<VisualEffect>().Play();
    }

    public override void StartJengaSpawn()
    {
        isJengaSpawning = true;
    }    

    private void SpawnAllJengas()
    {
        while (isJengaSpawning)
        {
            // Eğer önceki sefer en son sütun yerleşmiş ise en başa dönsün
            int nextJengaColumn = (lastPlacedJengaColumn % 3) + 1;
            // Eğer önceki sefer en son sütun yerleşmiş ise bir üst sıraya çık, yerleşmedi ise aynı satırdan devam et
            int nextJengaRow = (lastPlacedJengaColumn % 3 == 0) ? ++lastPlacedJengaRow : lastPlacedJengaRow;
            // Bir sonraki jenganın hangi yöne bakacağı için satır kontrolü
            bool jengaDirectionCheck = nextJengaRow % 2 == 0;

            Vector3 nextJengaPosition = Vector3.zero;
            nextJengaPosition.y = nextJengaRow * singleJengaSize * (3f/5) - (singleJengaSize * 3f/10); // 3f/5 = Jenga boyutlarının 0.25x0.15x0.75 olmasından kaynaklı(boyu genişliğinin 3/5 katı) ve boyutun 3f/10 çıkarılmasının sebebi de bloğun merkezinin merkeze denk gelmesi(tam otursun diye kalan yarım pay kalkıyor)

            Quaternion nextJengaRotation = Quaternion.identity;

            // Jengaların her satırda farklı yönlere bakması gerektiği için yön kontrolü
            if (jengaDirectionCheck)
            {
                nextJengaPosition.x = (nextJengaColumn - 2) * singleJengaSize;
            }
            else
            {
                nextJengaPosition.z = (nextJengaColumn - 2) * singleJengaSize;
                nextJengaRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            JengaTable.Instance.SpawnJengaGameObject(nextJengaPosition, nextJengaRotation, transform.parent.GetComponent<NetworkObject>());

            lastPlacedJengaColumn = nextJengaColumn;
            lastPlacedJengaRow = nextJengaRow;

            if (lastPlacedJengaColumn == 3 && lastPlacedJengaRow == jengaRowsCount)
                isJengaSpawning = false;

        }
    }
	
}
