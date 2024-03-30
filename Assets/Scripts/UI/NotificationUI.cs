using TMPro;
using UnityEngine;

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

    public void ErrorNotification()
    {
        Notification("Bir hata meydana geldi.");
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
