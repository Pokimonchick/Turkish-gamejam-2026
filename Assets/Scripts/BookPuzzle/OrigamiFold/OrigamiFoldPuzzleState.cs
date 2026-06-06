using UnityEngine;

public class OrigamiFoldPuzzleState : MonoBehaviour
{
    public bool HasFireShard { get; private set; }
    public bool IsComplete { get; private set; }
    public Transform player;
    public Transform respawnPoint;
    public GameObject fireCollectedIndicator;
    public GameObject completeIndicator;

    private void Awake()
    {
        HasFireShard = false;
        IsComplete = false;

        if (fireCollectedIndicator != null)
        {
            fireCollectedIndicator.SetActive(false);
        }

        if (completeIndicator != null)
        {
            completeIndicator.SetActive(false);
        }
    }

    public void CollectFireShard()
    {
        HasFireShard = true;

        if (fireCollectedIndicator != null)
        {
            fireCollectedIndicator.SetActive(true);
        }

        Debug.Log("Fire shard collected", this);
    }

    public void CompleteLevel()
    {
        if (!HasFireShard)
        {
            Debug.Log("Need fire shard", this);
            return;
        }

        IsComplete = true;

        if (completeIndicator != null)
        {
            completeIndicator.SetActive(true);
        }

        Debug.Log("Origami puzzle complete", this);
    }

    public void RespawnPlayer()
    {
        if (player == null || respawnPoint == null)
        {
            Debug.LogWarning($"{name}: player or respawnPoint is not assigned.", this);
            return;
        }

        player.position = respawnPoint.position;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.position = (Vector2)respawnPoint.position;
        }

        OrigamiFoldPassenger passenger = player.GetComponent<OrigamiFoldPassenger>();

        if (passenger != null)
        {
            passenger.RefreshCurrentStack();
        }
    }
}
