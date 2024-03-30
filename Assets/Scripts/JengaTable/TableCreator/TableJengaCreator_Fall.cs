using Unity.Netcode;
using UnityEngine;

public class TableJengaCreator_Fall : BaseTableJengaCreator
{

    private int lastPlacedJengaColumn;
    private int lastPlacedJengaRow;
    private float jengaSpawnTimer = 1f;
    private float jengaSpawnTimerMax = 0.1f;
    private float jengaUnfreezeTimer = 1f;


    private void Update()
    {

        if (!IsServer) return;

        if (!isJengaSpawning)
        {
            jengaUnfreezeTimer -= Time.deltaTime;
            if (jengaUnfreezeTimer <= 0f)
            {
                foreach (GameObject jenga in JengaTable.Instance.GetJengaList())
                {
                    // Jengalar yerleştikten sonra rotasyon kilitlerini açıyor
                    jenga.GetComponent<Rigidbody>().freezeRotation = false;
                }
                Destroy(gameObject);
            }
        }
        else
        {
            jengaSpawnTimer -= Time.deltaTime;
            if (jengaSpawnTimer <= 0f)
            {
                SpawnNextJenga();
                jengaSpawnTimer = jengaSpawnTimerMax;
            }
        }

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
        // Jengaların yukarıdan düşmesi için y değeri
        nextJengaPosition.y = singleJengaSize * (jengaRowsCount + 2); 

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

        GameObject spawnedJenga = JengaTable.Instance.SpawnJengaGameObject(nextJengaPosition, nextJengaRotation, transform.parent.GetComponent<NetworkObject>());
        spawnedJenga.GetComponent<Rigidbody>().freezeRotation = true;

        lastPlacedJengaColumn = nextJengaColumn;
        lastPlacedJengaRow = nextJengaRow;

        if (lastPlacedJengaColumn == 3 && lastPlacedJengaRow == jengaRowsCount)
            isJengaSpawning = false;

    }

}
