using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    
	public static CrosshairUI Instance { get; private set; }


    private void Awake()
    {
        Instance = this;
        Show();
    }

    public void Hide()
    {
        Cursor.lockState = CursorLockMode.None;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        Cursor.lockState = CursorLockMode.Locked;
        gameObject.SetActive(true);
    }
	
}
