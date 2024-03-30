using UnityEngine;
using UnityEngine.UI;

public class AboutMeUI : MonoBehaviour
{
    
	[SerializeField] private Button aboutMeToggleButton;

    [SerializeField] private Transform aboutMePopup;
    [SerializeField] private Button aboutMeCloseButton;
    [SerializeField] private Button githubButton;
    [SerializeField] private Button assetsButton;
	

    private void Awake()
    {
        aboutMeToggleButton.onClick.AddListener(() => {
            ToggleAboutMe();
        });
        aboutMeCloseButton.onClick.AddListener(() => {
            HideAboutMe();
        });
        githubButton.onClick.AddListener(() => {
            Application.OpenURL("https://github.com/Kuppiarda/Kuppi-s-Jenga-Game");
        });
        assetsButton.onClick.AddListener(() => {
            Application.OpenURL("https://github.com/Kuppiarda/Kuppi-s-Jenga-Game/blob/main/ASSETS.md");
        });
    }

    private void Start()
    {
        HideAboutMe(); // Hide'da gizlenirs buttonClickSound çalışmıyor(Hiyerarşiden dolayı)
    }

    private void ShowAboutMe()
    {
        aboutMePopup.gameObject.SetActive(true);
    }

    private void HideAboutMe()
    {
        aboutMePopup.gameObject.SetActive(false);
    }

    private void ToggleAboutMe()
    {
        if (aboutMePopup.gameObject.activeInHierarchy)
        {
            HideAboutMe();
        }
        else
        {
            ShowAboutMe();
        }
    }

}
