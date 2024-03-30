using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Diagnostics;

public class JengaBrick : NetworkBehaviour
{
    
    private LayerMask jengaLayermask;
    private Material defaultJengaMaterial;
    [SerializeField] private Material selectedJengaMaterial;
    [SerializeField] private AudioClip jengaBrickFallSound;

	
    private void Awake()
    {
        JengaTable.Instance.OnSelectedJengaChanged += JengaTable_OnSelectedJengaChanged;
        JengaTable.Instance.OnSelectedJengaPlacing += JengaTable_OnSelectedJengaPlacing;
        JengaTable.Instance.OnSelectedJengaPlaced += JengaTable_OnSelectedJengaPlaced;
        jengaLayermask = JengaGameManager.Instance.GetJengaLayerMask();
        defaultJengaMaterial = GetComponent<Renderer>().material;
    }

    private void JengaTable_OnSelectedJengaPlaced(object sender, EventArgs e)
    {
        if (JengaTable.Instance.GetSelectedJengaGameObject() == gameObject)
        {
            EnableJengaBrickPhysics();
            AddJengaLayermask();         
        }        
    }

    private void JengaTable_OnSelectedJengaPlacing(object sender, EventArgs e)
    {
        if (JengaTable.Instance.GetSelectedJengaGameObject() == gameObject)
        {
            DisableJengaBrickPhysics(); // Çarpışmasın ya da düşmesin diye fiziklerini kapatıyor
            RemoveJengaLayermask();
            RemoveMaterialsFromJengaBrick();
            AddDefaultMaterialToJengaBrick();
        }
    }

    private void JengaTable_OnSelectedJengaChanged(object sender, EventArgs e)
    {
        if (JengaTable.Instance.GetSelectedJengaGameObject() == gameObject)
        {
            AddMaterialToJengaBrick(selectedJengaMaterial);
        }
        else
        {
            if (gameObject.GetComponent<Renderer>().materials.Length > 1)
            {
                RemoveMaterialsFromJengaBrick();
                AddDefaultMaterialToJengaBrick();
            }
        }
    }

    private void RemoveMaterialsFromJengaBrick()
    {
        Material[] newMaterials = { };
        gameObject.GetComponent<Renderer>().materials = newMaterials;
    }

    private void AddDefaultMaterialToJengaBrick()
    {
        Material[] defaultMaterial = { defaultJengaMaterial };
        gameObject.GetComponent<Renderer>().materials = defaultMaterial;
    }

    private void AddMaterialToJengaBrick(Material newMaterial)
    {
        Material[] oldMaterials = gameObject.GetComponent<Renderer>().materials;
        Material[] newMaterials = new Material[oldMaterials.Length + 1];

        for (int i = 0; i < oldMaterials.Length; i++)
        {
            newMaterials[i] = oldMaterials[i];
        }

        newMaterials[newMaterials.Length - 1] = newMaterial;
        gameObject.GetComponent<Renderer>().materials = newMaterials;
    }
    
    public void MoveJengaBrick(Vector3 forceDir)
    {
        MoveJengaBrickServerRpc(forceDir);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveJengaBrickServerRpc(Vector3 forceDir)
    {
        transform.Translate(forceDir * Time.deltaTime);
    }

    public void TeleportJengaBrick(Vector3 position)
    {
        TeleportJengaBrickServerRpc(position);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TeleportJengaBrickServerRpc(Vector3 position)
    {
        transform.position = position;
    }    

    public void RotateJengaBrick(Vector3 eulerAngles)
    {
        RotateJengaBrickServerRpc(eulerAngles);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RotateJengaBrickServerRpc(Vector3 eulerAngles)
    {
        transform.eulerAngles = eulerAngles;
    }

    public void DisableJengaBrickPhysics()
    {
        DisableJengaBrickPhysicsServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisableJengaBrickPhysicsServerRpc()
    {
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        gameObject.GetComponent<Collider>().enabled = false;
    }

    public void EnableJengaBrickPhysics()
    {
        EnableJengaBrickPhysicsServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableJengaBrickPhysicsServerRpc()
    {
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
        gameObject.GetComponent<Collider>().enabled = true;
    }

    private void AddJengaLayermask()
    {
        gameObject.layer = (int)Math.Log(jengaLayermask, 2);
    }

    private void RemoveJengaLayermask()
    {
        gameObject.layer = 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsServer && collision.relativeVelocity.magnitude > 0.7f)
        {
            PlayJengaSoundClientRpc(collision.transform.position);
        }
    }

    [ClientRpc]
    private void PlayJengaSoundClientRpc(Vector3 position)
    {
        SoundManager.Instance.PlaySound(jengaBrickFallSound, position);
    }

}
