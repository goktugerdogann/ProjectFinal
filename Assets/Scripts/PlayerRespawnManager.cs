using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance;

    [Header("Refs")]
    public Transform player;
    public Transform defaultSpawnPoint;   // ilk doðacaðý yer (spawn)
    public float spawnYOffset = 0.15f;
    Vector3 lastCheckpointPos;
    Quaternion lastCheckpointRot;
    bool hasCheckpoint = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (!hasCheckpoint && defaultSpawnPoint != null)
        {
            lastCheckpointPos = defaultSpawnPoint.position;
            lastCheckpointRot = defaultSpawnPoint.rotation;
            hasCheckpoint = true;
        }
    }

    public void SetCheckpoint(Vector3 pos, Quaternion rot)
    {
        lastCheckpointPos = pos;
        lastCheckpointRot = rot;
        hasCheckpoint = true;
        // istersem burada SaveGame de diyebilirim
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public void RespawnPlayer()
    {
        if (player == null) return;

        // 1) Temel pozisyon & rotasyon kaynaðýný seç
        Vector3 basePos;
        Quaternion baseRot;

        if (hasCheckpoint)
        {
            basePos = lastCheckpointPos;
            baseRot = lastCheckpointRot;
        }
        else if (defaultSpawnPoint != null)
        {
            basePos = defaultSpawnPoint.position;
            baseRot = defaultSpawnPoint.rotation;
        }
        else
        {
            basePos = player.position;
            baseRot = player.rotation;
        }

        // 2) Biraz yukarý kaydýr (zemine gömülmesin)
        Vector3 targetPos = basePos + Vector3.up * spawnYOffset;
        Quaternion targetRot = baseRot;

        // 3) CharacterController varsa, pozisyon deðiþtirirken kapatýp aç
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null)
        {
            cc.enabled = false;
            player.SetPositionAndRotation(targetPos, targetRot);
            cc.enabled = true;
        }
        else
        {
            player.SetPositionAndRotation(targetPos, targetRot);

            // Rigidbody’li bir karakterse hýzlarý sýfýrla
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

}
