using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class JengaTable : NetworkBehaviour
{
    
    // Singleton
    public static JengaTable Instance { get; private set; }

    // Jenga spawn ayarları
    [SerializeField] private GameObject jengaPrefab;
    [Range(5, 20)] [SerializeField] private int jengaRowsCount;
    private float singleJengaSize = 0.15f;
    private List<GameObject> jengaList = new List<GameObject>();

    // Jenga yaratıcıları
    [SerializeField] private BaseTableJengaCreator[] tableJengaCreators;
    private NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false);

    // Jenga Game Endingleri
    [SerializeField] private BaseJengaGameEnding[] jengaGameEndings;

    // Jenga Movement
    [SerializeField] private float jengaPushPowerDefault;
    [SerializeField] private float jengaPushPowerMax = 10f;
    [SerializeField] private float jengaPushLerpSpeed = 3f;
    private LayerMask jengaLayerMask;
    private float jengaPushPower;
    private GameObject selectedJenga;
    private bool isJengaMoved;
    public event EventHandler OnSelectedJengaChanged;
    private Vector3 jengaPositionBeforeMove;

    // Jenga Placement
    [SerializeField] private VisualEffect jengaPlacementVisualEffect;
    public event EventHandler OnSelectedJengaPlacing;
    public event EventHandler OnSelectedJengaPlaced;
    private float flyingJengaPositionVerticalOffset = 0.3f;

    // Jenga Table State
    public enum TableState {
        Select,
        Move,
        Place,
        Fall
    }

    private NetworkVariable<TableState> tableState = new NetworkVariable<TableState>();

    // Ending
    public event EventHandler OnGameEnding;    

    // Camera
    private float highestPlacedJengaHeight;


    private void Awake()
    {
        Instance = this;

        if (IsServer)
            ChangeTableStateServerRpc(TableState.Select);
        
        highestPlacedJengaHeight = transform.position.y + flyingJengaPositionVerticalOffset + ((singleJengaSize * 3f/5f) * jengaRowsCount) ; // JengaTable'ın üstünden başlayıp singlejengasize'ın 3/5 katı (0.25 x 0.15 x 0.75) ile row count çarpılıyor ve koyma offseti ekleniyor)
        jengaPlacementVisualEffect.enabled = false;
    }

    private void Start()
    {
        jengaLayerMask = JengaGameManager.Instance.GetJengaLayerMask();
    }



    private void Update()
    {
        if (!isGameStarted.Value) return;

        if (tableState.Value == TableState.Fall && IsServer)
        {
            PrepareForEnding();            
        }

        if (!JengaGameMultiplayerManager.Instance.GetActivePlayer().IsPlayerSitting() || !JengaGameMultiplayerManager.Instance.IsActivePlayer()) return;

        switch (tableState.Value)
        {
            case TableState.Select:
                HandleJengaSelection();

                // Eğer seçildikten sonra fareye basılırsa move aşamasına geç
                if (GameInput.Instance.IsLeftMouseButtonPressed() && selectedJenga != null)
                    ChangeTableStateServerRpc(TableState.Move);

                break;
            case TableState.Move:
                HandleJengaMovement();
                break;
            case TableState.Place:
                HandleJengaPlacement();
                break;
        }                              
    }    

    // Jenga yaratıcısı çağırıcı
    public void SpawnTableJengaCreator()
    {
        if (tableJengaCreators.Length == 0) return;

        GameObject randomTableJengaCreator = tableJengaCreators[UnityEngine.Random.Range(0, tableJengaCreators.Length)].gameObject;
        GameObject tableJengaCreatorGameObject = Instantiate(randomTableJengaCreator);
        NetworkObject tableJengaCreatorNetworkObject = tableJengaCreatorGameObject.GetComponent<NetworkObject>();
        tableJengaCreatorNetworkObject.Spawn();
        tableJengaCreatorNetworkObject.TrySetParent(transform.GetComponent<NetworkObject>()); // TrySetParent ile Parent yapıyoruz(keşke skin değiştirmeyi yapmadan önce bilseydim)
        tableJengaCreatorNetworkObject.transform.localPosition = Vector3.zero;
        BaseTableJengaCreator tableJengaCreator = tableJengaCreatorGameObject.GetComponent<BaseTableJengaCreator>();
        tableJengaCreator.SetTableSettings(jengaRowsCount, singleJengaSize);
        tableJengaCreator.StartJengaSpawn();
    }

    public GameObject SpawnJengaGameObject(Vector3 position, Quaternion rotation, NetworkObject parent)
    {
        GameObject jenga = Instantiate(jengaPrefab);
        NetworkObject jengaNetworkObject = jenga.GetComponent<NetworkObject>();
        if (parent != null)
            jenga.transform.position = parent.transform.position + position;
        jengaNetworkObject.Spawn();
        jengaNetworkObject.DestroyWithScene = true;
        jengaNetworkObject.TrySetParent(parent); // TrySetParent ile Parent yapıyoruz(keşke skin değiştirmeyi yapmadan önce bilseydim)
        jenga.transform.localPosition = position;
        jenga.transform.localRotation = rotation;
        jengaList.Add(jenga);
        return jenga;
    }


    // Kameraya göre jenga seçme
    private void HandleJengaSelection()
    {
        if (JengaGameManager.Instance.IsGamePaused()) return;
        if (IsRaycastHitJengaBrick(out RaycastHit hit))
        {   
            float topLayerJengaVerticalPosition = highestPlacedJengaHeight - flyingJengaPositionVerticalOffset - (singleJengaSize * (3f / 5));
            if (selectedJenga != hit.transform.gameObject && hit.transform.position.y <= topLayerJengaVerticalPosition)
            {
                UnselectSelectedJenga();
                SetSelectedJenga(hit.transform.gameObject);
            }
            JengaGameMultiplayerManager.Instance.SetPlayerVisualHeadRotationWithPositionServerRpc(hit.point); // Jengaya baksın diye
        }
    }    

    // Fare sol tıklaması ile jenga hareket ettirme
    private void HandleJengaMovement()
    {

        if (GameInput.Instance.IsLeftMouseButtonPressed()) // Eğer sol tıka basılıyorsa
        {

            if (!isJengaMoved)
            {
                isJengaMoved = true;
                jengaPositionBeforeMove = selectedJenga.transform.position; // Hareket ettirilmeden önceki jenga konumu(kontrol için)
            }
            else // Jengayı slottan çıkarma kontrolü
            {
                float jengaCollectOffset = singleJengaSize * 3f; // Eğer jenga boyutunun 3 katı kadar ilerlemiş ise jenga yerleştirmeye geçilecek
                if (Vector3.Distance(jengaPositionBeforeMove, selectedJenga.transform.position) >= jengaCollectOffset) // Jenga dışarıda
                {                            
                    OnSelectedJengaPlacing?.Invoke(this, EventArgs.Empty);            
                    ChangeTableStateServerRpc(TableState.Place);                    
                    return;
                }
            }

            Vector3 forceDir = Vector3.zero;
            Vector3 selectedJengaPosition = selectedJenga.transform.position;
            Vector3 cameraPosition = Camera.main.transform.position;
            float dirMultiplier;

            // Kamera için: X fazla         
            if (selectedJengaPosition.x < cameraPosition.x)
            {
                // Z fazla ise -1, az ise açıya göre
                dirMultiplier = (selectedJengaPosition.z < cameraPosition.z) ? -1f : 
                                (selectedJenga.transform.eulerAngles.y > 45f && selectedJenga.transform.eulerAngles.y < 135f) ? -1f : 1f;
            }
            // Kamera için: X az
            else
            {
                // Z az ise 1, fazla ise açıya göre
                dirMultiplier = (selectedJengaPosition.z > cameraPosition.z) ? 1f : 
                                (selectedJenga.transform.eulerAngles.y > 45f && selectedJenga.transform.eulerAngles.y < 135f) ? 1f : -1f;
            }

            jengaPushPower = Mathf.Lerp(jengaPushPower, jengaPushPowerMax, jengaPushLerpSpeed * Time.deltaTime);
            forceDir.z = jengaPushPower * dirMultiplier;

            selectedJenga.GetComponent<JengaBrick>().MoveJengaBrick(forceDir);
            JengaGameMultiplayerManager.Instance.SetActivePlayerAsLastTouchedPlayerServerRpc();
            

        }
        else if (!GameInput.Instance.IsLeftMouseButtonPressed() && isJengaMoved) // Eğer jenga önceden hareket ettirilmiş ve sol tıka basılmıyorsa
        {
            jengaPushPower = jengaPushPowerDefault;
        }

    }

    // Jengayı yukarı yerleştirme
    private void HandleJengaPlacement()
    {

        if (selectedJenga == null) return;
        
        if (IsRaycastHitJengaBrick(out RaycastHit hit)) // Eğer yerleştirirken bakılan şey bir jengabrick ise
        {

            if (jengaPlacementVisualEffect.enabled == false)
            {
                jengaPlacementVisualEffect.enabled = true;
            }

            Vector3 flyingJengaPosition = hit.point;
            flyingJengaPosition.y += flyingJengaPositionVerticalOffset; // Koyma sırasında daha yukarıda durması için

            selectedJenga.GetComponent<JengaBrick>().TeleportJengaBrick(flyingJengaPosition);
            selectedJenga.GetComponent<JengaBrick>().RotateJengaBrick(new Vector3(0, 
                                                    (hit.transform.eulerAngles.y > 45 && hit.transform.eulerAngles.y < 135) ? 0 : 90,
                                                    0)); // Eğer seçilen objenin y değeri 45 ten büyük ise bir sonraki ters olması gerektiği için rotation y 0, değil ise 90
                                                    
            jengaPlacementVisualEffect.transform.position = hit.point;

            if (GameInput.Instance.IsLeftMouseButtonPressedOnce())
            {
                PlaceSelectedJenga();

                CheckForHighestPlacedJengaHeight(flyingJengaPosition); // Yeni yüksekliğe erişildi mi diye kontrol

                ChangeTableStateServerRpc(TableState.Select);     

                JengaGameMultiplayerManager.Instance.SelectNextActivePlayerServerRpc();           
            }

            JengaGameMultiplayerManager.Instance.SetPlayerVisualHeadRotationWithPositionServerRpc(hit.point); // Jengaya baksın diye

        }

    }
 
    public void PlaceSelectedJenga()
    {
        OnSelectedJengaPlaced?.Invoke(this, EventArgs.Empty);
        selectedJenga = null;                
        jengaPlacementVisualEffect.enabled = false;
    }




    // Jenga düştükten sonra tek seferlik hazırlanış
    private void PrepareForEnding()
    {
        PrepareForEndingClientRpc();
        isGameStarted.Value = false;
        SpawnJengaGameEnding();
    }

    [ClientRpc]
    private void PrepareForEndingClientRpc()
    {
        if (Player.LocalInstance.IsPlayerSitting())
        {
            Player.LocalInstance.GetUpChair();
        }

        UnselectSelectedJenga();
    }

    private void SpawnJengaGameEnding()
    {
        if (jengaGameEndings.Length == 0) return;
        GameObject randomJengaGameEnding = jengaGameEndings[UnityEngine.Random.Range(0, jengaGameEndings.Length)].gameObject;
        GameObject jengaGameEnding = Instantiate(randomJengaGameEnding);
        NetworkObject jengaGameEndingNetworkObject = jengaGameEnding.GetComponent<NetworkObject>();
        jengaGameEndingNetworkObject.GetComponent<NetworkObject>().Spawn();
        jengaGameEndingNetworkObject.DestroyWithScene = true;
    }    



    public void TriggerOnGameEnding()
    {
        OnGameEnding?.Invoke(this, EventArgs.Empty);
    }




    private bool IsRaycastHitJengaBrick(out RaycastHit hit)
    {
        // Camera'dan Jenga'ya raycast
        Ray cameraRay = Camera.main.ScreenPointToRay(GameInput.Instance.GetMousePosition());
        return Physics.Raycast(cameraRay, out hit, 10, jengaLayerMask);
    }

    public GameObject GetSelectedJengaGameObject()
    {
        return selectedJenga;
    }    

    public List<GameObject> GetJengaList()
    {
        return jengaList;
    }



    private void SetSelectedJenga(GameObject selectedJenga)
    {
        this.selectedJenga = selectedJenga;
        OnSelectedJengaChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UnselectSelectedJenga()
    {
        SetSelectedJenga(null);
        isJengaMoved = false;
    }



    private void CheckForHighestPlacedJengaHeight(Vector3 position)
    {
        if (highestPlacedJengaHeight < position.y)
        {
            highestPlacedJengaHeight = position.y;
        }
    }

    public Vector3 GetTableCoordinates()
    {
        return transform.position;
    }

    public float GetHighestPlacedJengaHeight()
    {
        return highestPlacedJengaHeight;
    }


    [ServerRpc(RequireOwnership = false)]
    public void ChangeTableStateServerRpc(TableState tableState)
    {
        this.tableState.Value = tableState;
    }

    public TableState GetTableState()
    {
        return tableState.Value;
    }



    public void StartGame()
    {
        if (!IsServer || !NetworkManager.Singleton) return;
        isGameStarted.Value = true;
    }

    public bool IsGameStarted()
    {
        return isGameStarted.Value;
    }


}
