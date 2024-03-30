using Unity.Netcode;
using UnityEngine;

public class JengaGameEnding_Crash : BaseJengaGameEnding
{
    
    [SerializeField] private CrashCarController crashCarController;
    private NetworkVariable<Vector3> carSpawnPosition = new NetworkVariable<Vector3>();
    private float carDistance = 100;
    private float hornSoundDistancePercentage = 0.25f;
    private bool isWaitingForNextRound;
    private Vector3 targetPosition;


    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        GenerateRandomPosition();
    }

	private void Update()
    {

        if (!IsServer) return;

        if (isWaitingForNextRound)
        {
            WaitForNextRound();
            return;
        }

        if (!isEndingStarted)
        {
            CheckForDelay();

            if (isEndingStarted)
            {
                crashCarController.PlayEngineSound(); // Motor sesi başlıyor
            }

            return;
        }

        crashCarController.MoveCarToLastTouchedPlayer();
        CheckForCarSounds();

    }

    // Her el araba farklı yerden gelmesi için rastgele pozisyon oluşturucu
    private void GenerateRandomPosition()
    {
        targetPosition = JengaGameMultiplayerManager.Instance.GetLastTouchedPlayer().transform.position;
        carSpawnPosition.Value = new Vector3(targetPosition.x + Random.Range(-1f, 1f) * carDistance, targetPosition.y, targetPosition.z + Random.Range(-1f, 1f) * carDistance);        
        crashCarController.SetPosition(carSpawnPosition.Value);
    }

    private void CheckForCarSounds()
    {
        float carCurrentDistance = Vector3.Distance(crashCarController.transform.position, targetPosition);
        float carSpawnDistance = Vector3.Distance(carSpawnPosition.Value, targetPosition);
        
            if (carCurrentDistance / carSpawnDistance <= 1 - hornSoundDistancePercentage)
            {
                crashCarController.PlayHornSound();
            }

            if (carCurrentDistance / carSpawnDistance <= 0.1f) // Yeterince yaklaştı, oyun bitti
            {
                ShowWaitForEndingClientRpc();
                crashCarController.StopNonCrashSounds();
                crashCarController.PlayCrashSound();
                isWaitingForNextRound = true;
            }

    }

    [ClientRpc]
    private void ShowWaitForEndingClientRpc()
    {
        ShowWaitForEndingUI();
    }
	
}
