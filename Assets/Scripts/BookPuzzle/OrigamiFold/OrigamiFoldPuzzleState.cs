using System.Collections;
using UnityEngine;

public class OrigamiFoldPuzzleState : MonoBehaviour
{
    public bool HasFireShard { get; private set; }
    public bool IsComplete { get; private set; }
    public Transform player;
    public Transform respawnPoint;
    public GameObject fireCollectedIndicator;
    public GameObject completeIndicator;
    public OrigamiFoldMapResetter mapResetter;
    public bool resetFoldsOnRespawn = true;
    public Behaviour[] disableWhileRespawning;
    public bool resetProgressOnRespawn = true;
    public OrigamiFoldFireShard[] fireShards;
    public OrigamiFoldExit[] exits;
    public bool autoFindResetObjects = true;

    public bool IsRespawning { get; private set; }

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
        if (IsRespawning)
        {
            return;
        }

        if (!resetFoldsOnRespawn && !resetProgressOnRespawn)
        {
            TeleportPlayerToRespawn();
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    public IEnumerator RespawnRoutine()
    {
        IsRespawning = true;
        SetRespawnBehavioursEnabled(false);
        OrigamiFoldPassenger respawnPassenger = player == null
            ? null
            : player.GetComponent<OrigamiFoldPassenger>();
        Behaviour[] previousCarryBehaviours = null;

        if (respawnPassenger != null)
        {
            previousCarryBehaviours = respawnPassenger.disableWhileCarried;
            respawnPassenger.disableWhileCarried = new Behaviour[0];
        }

        if (resetFoldsOnRespawn)
        {
            if (mapResetter == null)
            {
                mapResetter = FindFirstObjectByType<OrigamiFoldMapResetter>();
            }

            if (mapResetter != null)
            {
                yield return mapResetter.ResetAllFoldsRoutine();
            }
            else
            {
                Debug.LogWarning($"{name}: mapResetter is not assigned.", this);
            }
        }

        if (resetProgressOnRespawn)
        {
            ResetRunProgress();
        }

        TeleportPlayerToRespawn();

        if (respawnPassenger != null)
        {
            respawnPassenger.disableWhileCarried = previousCarryBehaviours;
        }

        SetRespawnBehavioursEnabled(true);
        IsRespawning = false;
    }

    public void ResetRunProgress()
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

        FindResetObjectsIfNeeded();

        if (fireShards != null)
        {
            for (int i = 0; i < fireShards.Length; i++)
            {
                OrigamiFoldFireShard shard = fireShards[i];

                if (shard != null)
                {
                    shard.ResetShard();
                }
            }
        }

        if (exits != null)
        {
            for (int i = 0; i < exits.Length; i++)
            {
                OrigamiFoldExit exit = exits[i];

                if (exit != null)
                {
                    exit.RefreshVisual();
                }
            }
        }

        Debug.Log("Origami puzzle run progress reset", this);
    }

    private void FindResetObjectsIfNeeded()
    {
        if (!autoFindResetObjects)
        {
            return;
        }

        if (fireShards == null || fireShards.Length == 0)
        {
            fireShards = FindObjectsByType<OrigamiFoldFireShard>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        if (exits == null || exits.Length == 0)
        {
            exits = FindObjectsByType<OrigamiFoldExit>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }
    }

    private void TeleportPlayerToRespawn()
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

    private void SetRespawnBehavioursEnabled(bool enabled)
    {
        if (disableWhileRespawning == null)
        {
            return;
        }

        for (int i = 0; i < disableWhileRespawning.Length; i++)
        {
            Behaviour item = disableWhileRespawning[i];

            if (item != null)
            {
                item.enabled = enabled;
            }
        }
    }
}
