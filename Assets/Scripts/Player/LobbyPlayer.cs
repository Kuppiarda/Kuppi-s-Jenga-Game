using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : MonoBehaviour
{
    
	[SerializeField] private int holdingPlayerIndex;
    [SerializeField] private Button kickButton;
    [SerializeField] private TextMeshPro playerNameText;
    [SerializeField] private Animator animator;
    private float animationTimer;

    private int currentSkinId = -1;


    private void Update()
    {
        RandomAnimation();
    }

    private void Awake()
    {
        kickButton.onClick.AddListener(() => {
            PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(holdingPlayerIndex);
            LobbyServices.Instance.KickPlayer(playerData.playerId.ToString());
            MultiplayerManager.Instance.KickPlayer(playerData.clientId);
        });
    }

    private void Start()
    {

        if (!NetworkManager.Singleton.IsServer || holdingPlayerIndex == 0) // Host değilse ya da kendisiyse (0 = Host)
        {
            kickButton.gameObject.SetActive(false);
        }

        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += MultiplayerManager_OnPlayerDataNetworkListChanged;
        UpdateLobbyPlayer();

    }

    private void MultiplayerManager_OnPlayerDataNetworkListChanged(object sender, EventArgs e)
    {
        UpdateLobbyPlayer();
    }

    private void UpdateLobbyPlayer()
    {
        if (MultiplayerManager.Instance.IsPlayerIndexConnected(holdingPlayerIndex))
        {
            PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(holdingPlayerIndex);
            SetPlayerSkin(playerData.skinId);
            playerNameText.text = playerData.playerName.ToString();
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetPlayerSkin(int skinId)
    {
        if (currentSkinId == skinId) return;

        if (skinId < 0 || skinId > MultiplayerManager.Instance.GetSkinCount() - 1)
        {
            skinId = 0;
        }

        DestoryPlayerSkin(); // Eğer varsa önceki skini/skinleri(bug durumunda) silmek için
        PlayerVisual playerVisual = Instantiate(MultiplayerManager.Instance.GetPlayerSkinGameObjectFromSkinId(skinId), transform).GetComponent<PlayerVisual>();
        SetAnimatorAvatarFromPlayerVisual(playerVisual); // Idle animasyon avatarı
        currentSkinId = skinId;
    }

    private void DestoryPlayerSkin()
    {
        if (transform.childCount > 1)
        {
            foreach (Transform transformChild in transform)
            {
                if (transformChild.name != "PlayerNameText" && transformChild.name != "Canvas")
                {
                    Destroy(transformChild.gameObject);
                    currentSkinId = -1;
                }
            }
        }
    }

    private void SetAnimatorAvatarFromPlayerVisual(PlayerVisual playerVisual)
    {
        GetComponent<Animator>().avatar = playerVisual.GetSkinAvatar();
    }
    
    private void OnDestroy()
    {
        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged -= MultiplayerManager_OnPlayerDataNetworkListChanged;
    }

    private void RandomAnimation()
    {

        if (animationTimer <= 1f)
            SetAnimation(3); // 1 Kere tetiklensin diye Idle'a geçiyor

        animationTimer -= Time.deltaTime;
        if (animationTimer > 0) return;
        animationTimer = 2f; // Default olarak 2 saniyede bir deneyecek

        int randomNumber = UnityEngine.Random.Range(0, 100);
        if (randomNumber == 0)
        {
            SetAnimation(0); // SecretDance
            animationTimer = 10f; // Dans için ekstra süre
        }
        else if (randomNumber > 0 && randomNumber <= 10)
        {
            SetAnimation(1); // LookAround1
        }
        else if (randomNumber > 10 && randomNumber <= 20)
        {
            SetAnimation(2); // LookAround2
        }
        else
        {
            SetAnimation(3); // Idle
        }
    }

    private void SetAnimation(int animationId)
    {
        animator.SetInteger("SelectedAnimation", animationId);
    }
}
