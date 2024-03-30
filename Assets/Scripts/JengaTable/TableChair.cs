using Unity.Netcode;
using UnityEngine;

public class TableChair : NetworkBehaviour
{
    
	private bool isChairEmpty = true;


    public void SitThisChair()
    {
        isChairEmpty = false;
        CameraController.Instance.CheckForCameraNextFrame();
    }

    public void GetUpFromThisChair()
    {
        isChairEmpty = true;
        CameraController.Instance.CheckForCameraNextFrame();
    }

    public bool IsChairEmpty()
    {
        return isChairEmpty;
    }
	
}
