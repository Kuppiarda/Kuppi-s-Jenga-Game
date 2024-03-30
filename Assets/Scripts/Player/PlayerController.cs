using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{

    private CharacterController controller;
    private Transform playerCameraHead;
    private float currentHeadRotation;
    private Vector3 playerVelocity;
    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravityValue = -9.81f;
    private bool isPlayerRunning;
    [SerializeField] private AudioClip footstepSFX;
    private float footstepTimer;
        
    // Ground Check
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayerMask;
    private float groundDistance;

    // Player State (Idle, OnAir, Jump, Land)
    public enum PlayerState {
        Idle,
        OnAir,
        Jumping,
        Landing
    }

    private PlayerState playerState;

    public event EventHandler<OnPlayerStateChangedEventArgs> OnPlayerStateChanged;

    public class OnPlayerStateChangedEventArgs : EventArgs {
        public PlayerState newPlayerState;
    }

    private float playerOnAirTimer;
    private float playerOnAirTimerOffset = 0.5f;
    

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCameraHead = gameObject.GetComponent<Player>().GetPlayerCameraHead();

        // GroundCheck ayarlamaları
        groundDistance = controller.radius * 0.9f; // Kapsül karakterden biraz küçük olması için
    }

    private void Update()
    {

        if (!IsOwner) return;
        if (!Player.LocalInstance.GetPlayerVisual()) return;
        
        PlayerStateHandler();
        
        if (Player.LocalInstance.IsPlayerFrozen() && !Player.LocalInstance.IsPlayerSitting() || JengaGameManager.Instance.IsGamePaused()) // Oturmuyor ve donuk ise yine de yerçekimini uygula
        {
            GroundCheckForVelocity();            
            ApplyGravity();
            return;
        }
        
        if (Player.LocalInstance.IsPlayerFrozen() || JengaGameManager.Instance.IsGamePaused()) return; // oyuncu hareket edemiyorsa ya da oyunu duraklatılıysa aşağıdakileri işleme(gravity ve groundcheck üstte çalışıyor)

        PlayerUndergroundCheck();
        PlayerRotation();
        PlayerHeadRotation();
        GroundCheckForVelocity();
        PlayerMovement();
        PlayFootstepSFX();
        PlayerJump();
        ApplyGravity();
    }


    private void PlayerStateHandler()
    {

        if (!IsPlayerGrounded())
        {
            playerOnAirTimer += Time.deltaTime;

            if (playerOnAirTimer >= playerOnAirTimerOffset)
            {
                ChangePlayerState(PlayerState.OnAir);
            }
        }
        else
        {            
            if (playerOnAirTimer >= playerOnAirTimerOffset)
            {
                ChangePlayerState(PlayerState.Landing);
            }
            else if (playerState == PlayerState.Jumping && playerVelocity.y <= 0f)
            {
                ChangePlayerState(PlayerState.Idle);
            }

            playerOnAirTimer = 0f;
        }

    }

    private void ChangePlayerState(PlayerState playerState)
    {
        if (playerState == this.playerState) return;
        this.playerState = playerState;
        OnPlayerStateChanged?.Invoke(this, new OnPlayerStateChangedEventArgs { newPlayerState = playerState });        
    }


    private void PlayerUndergroundCheck()
    {
        if (transform.position.y <= -0.01f)
        {
            Vector3 playerPosition = transform.position;
            playerPosition.y = 2f;
            TeleportPlayer(playerPosition);
        }
    }

    private void GroundCheckForVelocity()
    {
        if (IsPlayerGrounded() && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
    }

    private void PlayerHeadRotation()
    {
        Vector2 mouseDelta = GameInput.Instance.GetMouseDelta();

        if (mouseDelta != new Vector2(0, 0))
        {
            float headRotationDelta = mouseDelta.y * JengaGameManager.Instance.GetMouseSensitivity() * Time.deltaTime;
            currentHeadRotation -= headRotationDelta;
            currentHeadRotation = Mathf.Clamp(currentHeadRotation, -90f, 90f);            
            playerCameraHead.localRotation = Quaternion.Euler(currentHeadRotation, 0f, 0f); // Localde kamera değişimi
            JengaGameMultiplayerManager.Instance.SetPlayerVisualHeadRotationServerRpc(currentHeadRotation); // 
        }
    }



    private void PlayerRotation()
    {
        Vector2 mouseDelta = GameInput.Instance.GetMouseDelta();
        float playerRotationDelta = mouseDelta.x * JengaGameManager.Instance.GetMouseSensitivity() * Time.deltaTime;
        transform.Rotate(Vector3.up * playerRotationDelta);
    }

    private void PlayerMovement()
    {
        Vector2 movement = GameInput.Instance.GetMovementNormalized();
        Vector3 move = transform.right * movement.x + transform.forward * movement.y;

        isPlayerRunning = !(move == Vector3.zero);

        if (!isPlayerRunning)
        {
            Player.LocalInstance.GetPlayerVisual().ResetPlayerVisualRotationSmoothly(Quaternion.identity);
            return;
        }

        Player.LocalInstance.GetPlayerVisual().RotatePlayerVisualSmoothly(Quaternion.LookRotation(move)); // Vücut dönmesi
        controller.Move(move * Time.deltaTime * playerSpeed);
    }

    private void PlayerJump()
    {
        if (GameInput.Instance.IsPlayerJumped() && IsPlayerGrounded())
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
            ChangePlayerState(PlayerState.Jumping);
        }
    }

    private void ApplyGravity()
    {
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }


    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(groundCheck.position + Vector3.up * (groundDistance * 0.9f), groundDistance); // GroundCheck için gizmos
    }

    private bool IsPlayerGrounded()
    {
        return Physics.CheckSphere(groundCheck.position + Vector3.up * (groundDistance * 0.9f), groundDistance, groundLayerMask); // (P1) Mesafenin groundDistance'tan biraz küçük olması gerekiyor ki fazla yukarıda kalmasın
    }


    private void PlayFootstepSFX()
    {
        footstepTimer -= Time.deltaTime;
        if (IsPlayerRunning() && IsPlayerGrounded() && footstepTimer <= 0)
        {
            footstepTimer = 0.5f;
            SoundManager.Instance.PlaySound(footstepSFX, transform.position, 0.3f);
        }
    } 


    public bool IsPlayerRunning()
    {
        return isPlayerRunning;
    }


    public void TeleportPlayer(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }   


    public void SetPlayerHeadRotationSmoothly(float newHeadRotation)
    {
        float rotationSpeed = 5f;
        playerCameraHead.localRotation = Quaternion.Lerp(playerCameraHead.localRotation, Quaternion.Euler(newHeadRotation, 0f, 0f), Time.deltaTime * rotationSpeed); // Camera Head
        Player.LocalInstance.SetPlayerVisualHeadRotationSmoothly(newHeadRotation); // Visual Head
    }


}