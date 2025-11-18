using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    public string checkpointName = "CP1";
    public Transform spawnPoint;  // boþ býrakýrsan kendi transform’unu kullanýr

    private void Reset()
    {
        // collider'ý trigger yapmayý unutma
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    Vector3 GetSpawnPos()
    {
        return spawnPoint != null ? spawnPoint.position : transform.position;
    }

    Quaternion GetSpawnRot()
    {
        return spawnPoint != null ? spawnPoint.rotation : transform.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (PlayerRespawnManager.Instance != null)
        {
            PlayerRespawnManager.Instance.SetCheckpoint(GetSpawnPos(), GetSpawnRot());
        }
    }
}
