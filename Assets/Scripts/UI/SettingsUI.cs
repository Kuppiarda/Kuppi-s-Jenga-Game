using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    
    public static SettingsUI Instance { get; private set; }

    [SerializeField] private Button settingsButton;
    [SerializeField] private Slider audioVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;	
    [SerializeField] private Transform settingsTransform;
    [SerializeField] private Animator animator;


    private void Awake()
    {
        Instance = this;
        HideSettings();

        settingsButton.onClick.AddListener(() => {
            ToggleSettings();
        });

        audioVolumeSlider.onValueChanged.AddListener((newValue) => {
            SoundManager.Instance.SetAudioVolume(newValue);
        });

        musicVolumeSlider.onValueChanged.AddListener((newValue) => {
            SoundManager.Instance.SetMusicVolume(newValue);
        });         
    }

    private void Start()
    {
        musicVolumeSlider.value = SoundManager.Instance.GetMusicVolume();
        audioVolumeSlider.value = SoundManager.Instance.GetAudioVolume();
    }

    private void ShowSettings()
    {
        settingsTransform.gameObject.SetActive(true);
        animator.SetTrigger("ShowAnimation");
    }

    public void HideSettings()
    {
        animator.ResetTrigger("ShowAnimation");
        settingsTransform.gameObject.SetActive(false);
    }

    public void ToggleSettings()
    {
        if (settingsTransform.gameObject.activeInHierarchy)
        {
            HideSettings();
        }
        else
        {
            ShowSettings();
        }
    }

}
