using Unity.Netcode;
using UnityEngine;

public class BaseTableJengaCreator : NetworkBehaviour
{
    
    private protected int jengaRowsCount;
    private protected float singleJengaSize;
    private protected bool isJengaSpawning;


    public void SetTableSettings(int jengaRowsCount, float singleJengaSize)
    {
        this.jengaRowsCount = jengaRowsCount;
        this.singleJengaSize = singleJengaSize;
    }

    public virtual void StartJengaSpawn()
    {
        Debug.Log("TableJengaCreator.StartJengaSpawn() Çağırılmamalıydı.");
    }
	
    public override void OnDestroy()
    {
        JengaTable.Instance.StartGame();
    }

}
