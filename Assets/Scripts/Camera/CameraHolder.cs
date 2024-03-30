using UnityEngine;

public class CameraHolder : MonoBehaviour
{
    
    [SerializeField] private float turnSpeed = 5f;

	private void Update()
    {
       Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.y += turnSpeed * Time.deltaTime;
        transform.eulerAngles = eulerAngles;
        Camera.main.transform.LookAt(gameObject.transform);
    }
	
}
