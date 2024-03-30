using System;
using System.Collections.Generic;
using UnityEngine;

public class JengaGameManager : MonoBehaviour
{
    
    public static JengaGameManager Instance { get; private set; }

    private float mouseSensitivity = 20.0f;

    // Pause
    private bool isGamePaused;
    public event EventHandler OnPauseGameTriggered;

    // Layers
    [SerializeField] private LayerMask chairLayer;
    [SerializeField] private LayerMask jengaLayer;
    [SerializeField] private LayerMask hideCameraLayer;


    private void Awake()
    {
        Instance = this; // Singleton ayarlaması
    }

    private void Start()
    {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        TriggerPauseGame();
    }

    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }

    public LayerMask GetChairLayerMask()
    {
        return chairLayer;
    }

    public LayerMask GetJengaLayerMask()
    {
        return jengaLayer;
    }

    public LayerMask GetHideCameraLayerMask()
    {
        return hideCameraLayer;
    }

    public bool IsGamePaused()
    {
        return isGamePaused;
    }

    public void TriggerPauseGame()
    {
        isGamePaused = !isGamePaused;
        OnPauseGameTriggered?.Invoke(this, EventArgs.Empty);
    }

}
