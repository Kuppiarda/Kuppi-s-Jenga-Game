using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class TableJengaCreator_Union : BaseTableJengaCreator
{

    // Spawnlanma
    private int lastPlacedJengaColumn;
    private int lastPlacedJengaRow;

    // Dağınık spawnlanma ayarları
    private Vector3 jengaSpawnBounds = new Vector3(5, 5, 5); // X, Y, Z'de 5-5-5 lik farklarla oluşabilecek(Y için sadece yukarı doğru)
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();

    // Spawn sonrası birleşme ve çözülme
    private int lastMovedJengaIndex;
    private float jengaUnionTimer = 3f; // Başladıktan 3 saniye sonra toplanacak
    private List<GameObject> jengaList;
    private float unionSpeed = 50f;
    private bool isEverythingFinished;
    [SerializeField] private AudioClip jengaPlaceSound;

    private void Update()
    {
        
        if (!IsServer) return;

        if (isEverythingFinished) return;

        if (jengaUnionTimer <= 0f) // Eğer birleşme başladıysa
        {

            MoveNextJengaToTable();

            if (jengaList.Count == lastMovedJengaIndex)
            {
                foreach (GameObject jenga in jengaList)
                {
                    jenga.GetComponent<Rigidbody>().isKinematic = false;
                }
                StopVisualEffectClientRpc();
                Destroy(gameObject, 2f);
                isEverythingFinished = true;
            }

        }
        else if (!isJengaSpawning) // Eğer birleşme başlamadıysa ve jenga spawn olmuyorsa
        {
            jengaUnionTimer -= Time.deltaTime;
            if (jengaUnionTimer <= 0f)
            {
                jengaList = JengaTable.Instance.GetJengaList();
            }            
        }
        else if (isJengaSpawning) // Eğer jenga spawn oluyorsa
        {
            SpawnNextJenga();
        }
    }

    [ClientRpc]
    private void StopVisualEffectClientRpc()
    {
        GetComponent<VisualEffect>().Stop();
    }

    private void MoveNextJengaToTable()
    {

        Transform lastMovedJenga = jengaList[lastMovedJengaIndex].transform;
        Vector3 lastMovedJengaOriginalPosition = originalPositions[lastMovedJengaIndex];
        Quaternion lastMovedJengaOriginalRotation = originalRotations[lastMovedJengaIndex];

        lastMovedJenga.localPosition = Vector3.Lerp(lastMovedJenga.localPosition, lastMovedJengaOriginalPosition, unionSpeed * Time.deltaTime);
        lastMovedJenga.localRotation = Quaternion.Lerp(lastMovedJenga.localRotation, lastMovedJengaOriginalRotation, unionSpeed * Time.deltaTime);
        transform.localPosition = lastMovedJenga.localPosition; // VFX Etkisi

        float lastMovedJengaOriginalPositionDistance = Vector3.Distance(lastMovedJenga.transform.localPosition, lastMovedJengaOriginalPosition);
        float lastMovedJengaOriginalRotationAngle = Quaternion.Angle(lastMovedJenga.transform.localRotation, lastMovedJengaOriginalRotation);

        // Yeterince yakın mı diye kontrol
        if (lastMovedJengaOriginalPositionDistance < 0.2f && lastMovedJengaOriginalRotationAngle <= 5f)
        {
            lastMovedJenga.transform.localPosition = originalPositions[lastMovedJengaIndex];
            lastMovedJenga.transform.localRotation = originalRotations[lastMovedJengaIndex];
            lastMovedJengaIndex++;
            PlayPlaceSoundClientRpc(transform.position);
        }

    }

    [ClientRpc]
    private void PlayPlaceSoundClientRpc(Vector3 position)
    {
        SoundManager.Instance.PlaySound(jengaPlaceSound, position);
    }

    public override void StartJengaSpawn()
    {
        isJengaSpawning = true;
    }

    private void SpawnNextJenga()
    {
        // Eğer önceki sefer en son sütun yerleşmiş ise en başa dönsün
        int nextJengaColumn = (lastPlacedJengaColumn % 3) + 1;
        // Eğer önceki sefer en son sütun yerleşmiş ise bir üst sıraya çık, yerleşmedi ise aynı satırdan devam et
        int nextJengaRow = (lastPlacedJengaColumn % 3 == 0) ? ++lastPlacedJengaRow : lastPlacedJengaRow;
        // Bir sonraki jenganın hangi yöne bakacağı için satır kontrolü
        bool jengaDirectionCheck = nextJengaRow % 2 == 0;

        Vector3 nextJengaPosition = Vector3.zero;
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

        nextJengaPosition.y = nextJengaRow * singleJengaSize * (3f/5) - (singleJengaSize * 3f/10); // 3f/5 = Jenga boyutlarının 0.25x0.15x0.75 olmasından kaynaklı(boyu genişliğinin 3/5 katı) ve boyutun 3f/10 çıkarılmasının sebebi de bloğun merkezinin merkeze denk gelmesi(tam otursun diye kalan yarım pay kalkıyor)

        originalPositions.Add(nextJengaPosition); // Orjinal yerleri
        originalRotations.Add(nextJengaRotation); // Orjinal dönüşleri

        Vector3 randomJengaPosition = new Vector3(
            Random.Range(-jengaSpawnBounds.x, jengaSpawnBounds.x),
            Random.Range(0, jengaSpawnBounds.y),
            Random.Range(-jengaSpawnBounds.z, jengaSpawnBounds.z)
        ); // Rastgele pozisyon

        Quaternion randomJengaRotation = Quaternion.Euler(
            Random.Range(0, 360),
            Random.Range(0, 360),
            Random.Range(0, 360)
        ); // Rastgele dönüş

        GameObject spawnedJenga = JengaTable.Instance.SpawnJengaGameObject(randomJengaPosition, randomJengaRotation, transform.parent.GetComponent<NetworkObject>());
        spawnedJenga.GetComponent<Rigidbody>().isKinematic = true; // Havada donuk durmaları için

        lastPlacedJengaColumn = nextJengaColumn;
        lastPlacedJengaRow = nextJengaRow;

        if (lastPlacedJengaColumn == 3 && lastPlacedJengaRow == jengaRowsCount)
            isJengaSpawning = false;
    }
	
}
