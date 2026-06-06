using UnityEngine;

public class OrigamiFoldFireShard : MonoBehaviour
{
    public OrigamiFoldPuzzleState puzzleState;
    public GameObject visualRoot;
    public bool collected;
    public Collider2D[] triggerColliders;
    public bool disableCollidersOnCollect = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !IsPlayer(other))
        {
            return;
        }

        if (puzzleState == null)
        {
            puzzleState = FindFirstObjectByType<OrigamiFoldPuzzleState>();
        }

        if (puzzleState == null)
        {
            Debug.LogWarning($"{name}: puzzleState is not assigned.", this);
            return;
        }

        puzzleState.CollectFireShard();
        collected = true;

        ResolveTriggerCollidersIfNeeded();

        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
        else
        {
            SetRenderersEnabled(false);
        }

        if (disableCollidersOnCollect)
        {
            SetTriggerCollidersEnabled(false);
        }
    }

    public void ResetShard()
    {
        gameObject.SetActive(true);
        collected = false;
        ResolveTriggerCollidersIfNeeded();

        if (visualRoot != null)
        {
            visualRoot.SetActive(true);
        }
        else
        {
            SetRenderersEnabled(true);
        }

        SetTriggerCollidersEnabled(true);
        Debug.Log($"{name}: fire shard reset.", this);
    }

    private void ResolveTriggerCollidersIfNeeded()
    {
        if (triggerColliders == null || triggerColliders.Length == 0)
        {
            triggerColliders = GetComponentsInChildren<Collider2D>(true);
        }
    }

    private void SetTriggerCollidersEnabled(bool enabled)
    {
        if (triggerColliders == null)
        {
            return;
        }

        for (int i = 0; i < triggerColliders.Length; i++)
        {
            Collider2D collider = triggerColliders[i];

            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = enabled;
            }
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        try
        {
            if (other.CompareTag("Player"))
            {
                return true;
            }
        }
        catch (UnityException)
        {
        }

        return other.GetComponentInParent<PlayerFreeRoadMover>() != null
            || other.GetComponentInParent<OrigamiFoldPlayerMover>() != null
            || other.GetComponentInParent<OrigamiFoldPassenger>() != null;
    }
}
