using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LanguageButtonUI : MonoBehaviour
{
    
	[SerializeField] private Locale localeCode;
    [SerializeField] private Image selectedImage;


    private void Start()
    {
        LocalizationSettings.SelectedLocaleChanged += LocalizationSettings_SelectedLocaleChanged;
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= LocalizationSettings_SelectedLocaleChanged;
    }

    private void LocalizationSettings_SelectedLocaleChanged(Locale locale)
    {
        CheckForLocale();
    }

    private void Awake()
    {
        CheckForLocale();

        GetComponent<Button>().onClick.AddListener(() => {
            SettingsUI.Instance.ChangeLanguage(localeCode);
        });
    }

    private void CheckForLocale()
    {
        if (LocalizationSettings.SelectedLocale.ToString() == localeCode.ToString()) // Locale'leri karşılaştırınca Build'da işe yaramıyor
        {
            selectedImage.gameObject.SetActive(true);
        }
        else
        {
            selectedImage.gameObject.SetActive(false);
        }
    }

	
}
