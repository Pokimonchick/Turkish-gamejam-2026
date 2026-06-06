using UnityEngine;

public class OrigamiFoldExit : MonoBehaviour
{
    public OrigamiFoldPuzzleState puzzleState;
    public GameObject lockedVisual;
    public GameObject openVisual;

    private void Awake()
    {
        ResolvePuzzleState();
        RefreshVisual();
    }

    private void Update()
    {
        ResolvePuzzleState();
        RefreshVisual();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        ResolvePuzzleState();

        if (puzzleState == null)
        {
            Debug.LogWarning($"{name}: puzzleState is not assigned.", this);
            return;
        }

        if (!puzzleState.HasFireShard)
        {
            Debug.Log("Need fire shard", this);
            RefreshVisual();
            return;
        }

        puzzleState.CompleteLevel();
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        bool isOpen = puzzleState != null && puzzleState.HasFireShard;

        if (lockedVisual != null)
        {
            lockedVisual.SetActive(!isOpen);
        }

        if (openVisual != null)
        {
            openVisual.SetActive(isOpen);
        }
    }

    private void ResolvePuzzleState()
    {
        if (puzzleState == null)
        {
            puzzleState = FindFirstObjectByType<OrigamiFoldPuzzleState>();
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
