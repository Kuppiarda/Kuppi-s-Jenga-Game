using Unity.Netcode;
using UnityEngine;

public class CrashCarController : NetworkBehaviour
{
    
    [SerializeField] private AudioSource engineSound;
    [SerializeField] private AudioSource hornSound;
    [SerializeField] private AudioSource crashSound;
    private Vector3 spawnPosition;
    private float carSpeed = 10f;

    
    private void Awake()
    {
        // Nadir anlarda oluşacağı için anlık güncellenmiyor onun yerine oluşurken güncelleniyor(güncel kalmadan yaklaşık 5-10 saniye duruyor)
        engineSound.volume = SoundManager.Instance.GetAudioVolume();
        hornSound.volume = SoundManager.Instance.GetAudioVolume();
        crashSound.volume = SoundManager.Instance.GetAudioVolume();
        spawnPosition = transform.position;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void MoveCarToLastTouchedPlayer()
    {
        transform.LookAt(spawnPosition);
        transform.position += transform.forward * Time.deltaTime * carSpeed;
    }

	public void PlayEngineSound()
    {
        PlayEngineSoundClientRpc();
    }

    public void PlayHornSound()
    {
        PlayHornSoundClientRpc();
    }

    public void PlayCrashSound()
    {
        PlayCrashSoundClientRpc();
    }

    [ClientRpc]
    private void PlayEngineSoundClientRpc()
    {
        engineSound.Play();        
    }

    [ClientRpc]
    private void PlayHornSoundClientRpc()
    {
        if (!hornSound.isPlaying)
            hornSound.Play();
    }

    [ClientRpc]
    private void PlayCrashSoundClientRpc()
    {
        crashSound.Play();
    }

    public void StopNonCrashSounds()
    {
        StopNonCrashSoundsClientRpc();
    }

    [ClientRpc]
    private void StopNonCrashSoundsClientRpc()
    {
        hornSound.Stop();
        engineSound.Stop();
    }
	
}
