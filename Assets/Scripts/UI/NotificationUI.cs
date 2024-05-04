using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class NotificationUI : MonoBehaviour
{
    
    public static NotificationUI Instance { get; private set; }

	[SerializeField] private Animator notificationAnimator;
    [SerializeField] private TMP_Text notificationText;
    private float notificationTimer = -1;


    private void Awake()
    {
        Instance = this;
    }
    
    private void Update()
    {
        if (notificationTimer >= 0)
        {
            notificationTimer -= Time.deltaTime;

            if (notificationTimer < 0)
            {
                HideAnimation();
            }

        }
        else
        {
            return;
        }
    }

    public void Notification(string message)
    {
        ShowAnimation();
        notificationTimer = 2f;
        notificationText.text = message;
    }

    public void LocalizedNotification(string localeKey, List<string> parameters = null, string tableName = "NotificationLocalization")
    {
        string localizedText = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, localeKey);

        if (parameters != null)
            localizedText = string.Format(localizedText, parameters.ToArray());

        Notification(localizedText);
    }

    public void ErrorNotification()
    {
        LocalizedNotification("errorNotification");
    }

    private void ShowAnimation()
    {
        notificationAnimator.ResetTrigger("HideAnimation");
        if (notificationTimer >= 0f)
        {
            notificationTimer = 4f;
        }
        else
        {
            notificationAnimator.SetTrigger("ShowAnimation");
        }
    }

    private void HideAnimation()
    {
        notificationAnimator.ResetTrigger("ShowAnimation");
        notificationAnimator.SetTrigger("HideAnimation");
    }
	
}
