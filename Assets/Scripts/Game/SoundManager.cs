using System;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    
    // Audio = Genel sesler, Music = Tavern müziği
    [SerializeField] private AudioClip[] tavernMusics;

    [SerializeField] private AudioSource tavernMusicAudioSource;
    [SerializeField] private AudioSource buttonClickAudioSource;

    private float playerAudioVolume;
    private float playerMusicVolume;

    private float buttonClickAudioVolumeMultiplier = 0.25f;

	public static SoundManager Instance { get; private set; }


    private void Awake()
    {

        if (Instance != null) // Kendisi değilse kendisini destroylasın
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadSoundSettings();
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySound(AudioClip audioClip, Vector3 position, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(audioClip, position, volume * playerAudioVolume);
    }
	
    private void LateUpdate()
    {
        if (!tavernMusicAudioSource.isPlaying)
            PlayTavernMusic();
    }

    private void PlayTavernMusic()
    {
        AudioClip selectedAudioClip = tavernMusics[UnityEngine.Random.Range(0, tavernMusics.Length)];
        tavernMusicAudioSource.clip = selectedAudioClip;
        tavernMusicAudioSource.Play();
    }

    private void ChangeCustomVolumes()
    {
        ChangeTavernMusicVolume();
        ChangeButtonClickSoundVolume();
    }

    private void ChangeTavernMusicVolume()
    {
        tavernMusicAudioSource.volume = playerMusicVolume;
    }

    private void ChangeButtonClickSoundVolume()
    {
        buttonClickAudioSource.volume = playerAudioVolume * buttonClickAudioVolumeMultiplier;
    }

    public void SetAudioVolume(float volume)
    {
        playerAudioVolume = volume;
        SaveSoundSettings();
    }

    public void SetMusicVolume(float volume)
    {
        playerMusicVolume = volume;
        SaveSoundSettings();
    }

    public float GetAudioVolume()
    {
        return playerAudioVolume;
    }

    public float GetMusicVolume()
    {
        return playerMusicVolume;
    }

    private void LoadSoundSettings()
    {
        playerAudioVolume = PlayerPrefs.GetFloat("AudioVolume", 0.8f);
        playerMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.2f);
        ChangeCustomVolumes();
    }

    private void SaveSoundSettings()
    {
        PlayerPrefs.SetFloat("AudioVolume", playerAudioVolume);
        PlayerPrefs.SetFloat("MusicVolume", playerMusicVolume);
        PlayerPrefs.Save();
        ChangeCustomVolumes();
    }

    private void PlayButtonClickAudio()
    {
        buttonClickAudioSource.Play();
    }

    public void AddButtonSoundsToChildren(Transform parentTransform)
    {
        Button[] buttons = parentTransform.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            button.onClick.AddListener(() => {
                PlayButtonClickAudio();
            });
        }
    }   

}
