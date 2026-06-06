using UnityEngine;

public class OrigamiFoldFireShard : MonoBehaviour
{
    public OrigamiFoldPuzzleState puzzleState;
    public GameObject visualRoot;
    public bool collected;

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

        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
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
            || other.GetComponentInParent<OrigamiFoldPassenger>() != null;
    }
}
