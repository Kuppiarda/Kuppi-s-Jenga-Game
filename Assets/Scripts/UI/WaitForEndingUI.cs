using System;
using UnityEngine;

public class WaitForEndingUI : MonoBehaviour
{
    

    private void Start()
    {
        JengaTable.Instance.OnGameEnding += JengaTable_OnGameEnding;
        Hide();
    }

    private void JengaTable_OnGameEnding(object sender, EventArgs e)
    {
        Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

}
