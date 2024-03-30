using UnityEngine;

public class AddButtonClickAudioToChildren : MonoBehaviour
{
    
    private void Awake()
    {
	    SoundManager.Instance.AddButtonSoundsToChildren(transform);
    }
	
}
