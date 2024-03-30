using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    
    public static CameraController Instance { get; private set; }

    private Player activeCameraPlayer; // Kamerayı bazen başkasına vermek için
    private Transform activePlayerCameraHead;
    private float cameraDefaultFOV = 90f;
    private float cameraFocusFOV = 60f;
    private bool checkForCameraNextFrame;


    private void Awake()
    {
        Instance = this;
        checkForCameraNextFrame = true;
    }

    private void Update()
    {

        if (!Player.LocalInstance) return;
        if (!Player.LocalInstance.GetPlayerVisual()) return;

        if (activeCameraPlayer != null)
        {
            ChangeFOV(cameraDefaultFOV);
            MoveCamera(activePlayerCameraHead.position, activePlayerCameraHead.rotation);
        }

        if (Player.LocalInstance.IsPlayerSitting() && JengaGameMultiplayerManager.Instance.GetActivePlayer().IsPlayerSitting()) // Eğer local oyuncu ve oyun hakkı sahibi oturuyorsa(masayı gör)
        {

            if (JengaTable.Instance.GetTableState() == JengaTable.TableState.Place) // Koyma aşaması
            {
                ChangeFOV(cameraFocusFOV);
                LookAtJengaTableFromTop(); // Koyma aşamasında aktif oyuncunun gözünden yukarıdan gör(kamera)
            }
            else // Seçme/ittirme aşaması
            {
                ChangeFOV(cameraDefaultFOV);
                LookAtJengaTable(); // Seçme ve ittirme aşamasında aktif oyuncunun gözünden gör(kamera)
            }

            ChangeActiveCameraPlayer(null);

        }
        else if (Player.LocalInstance.IsPlayerSitting() && !JengaGameMultiplayerManager.Instance.GetActivePlayer().IsPlayerSitting())
        {
            ChangeActiveCameraPlayer(JengaGameMultiplayerManager.Instance.GetActivePlayer()); // Local oturuyor ama aktif oturmuyorsa aktifi gör
        }
        else
        {
            if (JengaGameMultiplayerManager.Instance.IsActivePlayer())
            {
                JengaGameMultiplayerManager.Instance.SetActivePlayerCameraHeadLocalRotationServerRpc(Player.LocalInstance.GetPlayerCameraHead().localRotation); // Ayaktaysam ve aktif oyuncuysam kamera açımı servera yolla
            }
            ChangeActiveCameraPlayer(Player.LocalInstance); // Sadece normal şekilde local oyuncuyu gör
        }

    }  

    private void ChangeActiveCameraPlayer(Player player)
    {
        if (!checkForCameraNextFrame) return;
        checkForCameraNextFrame = false;

        if (activeCameraPlayer)
            ChangeChildrenLayer(0); // Visual görünsün diye

        activeCameraPlayer = player;

        if (player == null) return;

        ChangeChildrenLayer((int)Math.Log(JengaGameManager.Instance.GetHideCameraLayerMask(), 2)); // Visual gizleniyor
        activePlayerCameraHead = activeCameraPlayer.GetPlayerCameraHead();        
    }

    private void ChangeChildrenLayer(int layer)
    {
        foreach (Transform child in activeCameraPlayer.GetPlayerVisual().transform)
        {
            child.gameObject.layer = layer;
        }
    }

    private void LookAtJengaTableFromTop()
    {
        TableChair activePlayerTableChair = JengaGameMultiplayerManager.Instance.GetActivePlayer().GetSittingChair(); // Kameranın pozisyonu için
        Vector3 activePlayerCameraPosition = activePlayerTableChair.transform.position + activePlayerTableChair.transform.right * 1.5f; // Chairın sağından bakması için
        float topPositionCameraBonusOffset = 1f;
        activePlayerCameraPosition.y = JengaTable.Instance.GetHighestPlacedJengaHeight() + topPositionCameraBonusOffset;

        Vector3 jengaTableCameraPosition = JengaTable.Instance.GetTableCoordinates();
        jengaTableCameraPosition.y = JengaTable.Instance.GetHighestPlacedJengaHeight(); // Jenganın tam ortasına bakması için boyutu yarıya bölündü

        MoveCameraSmoothly(activePlayerCameraPosition, Quaternion.identity);
        LookAtSmoothly(jengaTableCameraPosition);     
    }

    private void LookAtJengaTable()
    {
        TableChair activePlayerTableChair = JengaGameMultiplayerManager.Instance.GetActivePlayer().GetSittingChair(); // Kameranın pozisyonu için
        Vector3 activePlayerCameraPosition = activePlayerTableChair.transform.position + activePlayerTableChair.transform.right * 1.5f; // Chairın sağından bakması için
        activePlayerCameraPosition.y = JengaTable.Instance.GetHighestPlacedJengaHeight();

        Vector3 jengaTableCameraPosition = JengaTable.Instance.GetTableCoordinates();
        jengaTableCameraPosition.y += JengaTable.Instance.GetHighestPlacedJengaHeight() / 2f; // Jenganın tam ortasına bakması için boyutu yarıya bölündü

        Instance.MoveCameraSmoothly(activePlayerCameraPosition, Quaternion.identity);
        Instance.LookAt(jengaTableCameraPosition);
    }    



    public void CheckForCameraNextFrame()
    {
        checkForCameraNextFrame = true;
    }

    public void LookAt(Vector3 position)
    {
        transform.LookAt(position);
    }

    public void LookAtSmoothly(Vector3 position, float lookSpeed = 5f)
    {
        Vector3 smoothPosition = Vector3.Lerp(transform.position, position, lookSpeed * Time.deltaTime);
        transform.LookAt(smoothPosition);
    }

	public void MoveCamera(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    public void MoveCameraSmoothly(Vector3 position, Quaternion rotation, float moveSpeed = 5f)
    {
        transform.position = Vector3.Lerp(transform.position, position, moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, moveSpeed * Time.deltaTime);
    }

    public void ChangeFOV(float fov, float fovSpeed = 5f)
    {
        Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, fov, fovSpeed * Time.deltaTime);
    }


	
}
