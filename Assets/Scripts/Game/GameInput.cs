using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{

    public static GameInput Instance { get; private set; }

	private InputActions inputActions;

    public event EventHandler OnInteractAction;
    public event EventHandler OnPauseAction;


    private void Awake()
    {
        Instance = this;
        inputActions = new InputActions();
        inputActions.PlayerController.Interact.performed += Interact_performed;
        inputActions.PlayerController.Pause.performed += Pause_performed;
    }

    private void Pause_performed(InputAction.CallbackContext context)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);   
    }

    private void OnDestroy()
    {
        inputActions.PlayerController.Pause.performed -= Pause_performed;  
        inputActions.PlayerController.Interact.performed -= Interact_performed;        
    }

    private void Interact_performed(InputAction.CallbackContext context)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    private void OnEnable()
    {
        inputActions.JengaController.Enable();
        inputActions.PlayerController.Enable();
    }

    private void OnDisable()
    {
        inputActions.JengaController.Disable();
        inputActions.PlayerController.Disable();
    }

    public bool IsLeftMouseButtonPressed()
    {
        if (JengaGameManager.Instance.IsGamePaused())
            return false;
        return inputActions.JengaController.PushJenga.IsPressed();
    }

    public bool IsLeftMouseButtonPressedOnce()
    {
        if (JengaGameManager.Instance.IsGamePaused())
            return false;        
        return inputActions.JengaController.PushJenga.WasReleasedThisFrame();
    }

    public Vector2 GetMovementNormalized()
    {
        return inputActions.PlayerController.Movement.ReadValue<Vector2>();
    }

    public bool IsPlayerJumped()
    {
        return inputActions.PlayerController.Jump.WasPressedThisFrame();
    }

    public Vector2 GetMouseDelta()
    {        
        return inputActions.PlayerController.Look.ReadValue<Vector2>();
    }

    public Vector3 GetMousePosition()
    {
        return Mouse.current.position.ReadValue();
    }    

}
