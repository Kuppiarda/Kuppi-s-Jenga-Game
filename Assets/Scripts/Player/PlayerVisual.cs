using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{

    [SerializeField] private Transform playerVisualHead;
    [SerializeField] private Avatar skinAvatar;

    
    public Avatar GetSkinAvatar()
    {
        return skinAvatar;
    }
    
    public void ResetPlayerVisualRotation()
    {
        transform.localRotation = Quaternion.identity;
    }

	public void ResetPlayerVisualRotationSmoothly(Quaternion rotation, float rotateSpeed = 5f)
    {
        transform.localRotation = Quaternion.Lerp(transform.localRotation, rotation, rotateSpeed * Time.deltaTime);
    }

    public void RotatePlayerVisualSmoothly(Quaternion rotation, float rotateSpeed = 5f)
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
    }

    public void SetPlayerVisualHeadVerticalRotation(float verticalRotation)
    {
        verticalRotation = Mathf.Clamp(verticalRotation, -45, 35);
        Vector3 newRotation = playerVisualHead.localEulerAngles;
        newRotation.x = verticalRotation;
        playerVisualHead.localEulerAngles = newRotation;
    }

    public void SetPlayerVisualHeadVerticalRotationSmoothly(float verticalRotation)
    {
        float rotationSpeed = 5f;
        verticalRotation = Mathf.Clamp(verticalRotation, -45, 35);
        Vector3 newRotation = playerVisualHead.localEulerAngles;
        newRotation.x = verticalRotation;
        playerVisualHead.localEulerAngles = Vector3.Lerp(playerVisualHead.localEulerAngles, newRotation, Time.deltaTime * rotationSpeed);
    }    

    public void SetPlayerVisualHeadRotationWithPosition(Vector3 hitPosition)
    {
        playerVisualHead.LookAt(hitPosition);
    }

}
