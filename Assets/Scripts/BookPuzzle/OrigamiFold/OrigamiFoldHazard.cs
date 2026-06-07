using UnityEngine;

public class OrigamiFoldHazard : MonoBehaviour
{
    public OrigamiFoldPuzzleState puzzleState;
    public bool respawnOnTouch = true;
    public bool disableAfterTouch;
    public GameObject visualRoot;
    public string debugName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (puzzleState == null)
        {
            puzzleState = FindFirstObjectByType<OrigamiFoldPuzzleState>();
        }

        string displayName = string.IsNullOrEmpty(debugName) ? name : debugName;
        Debug.Log($"{displayName}: player touched hazard.", this);

        if (respawnOnTouch)
        {
            if (puzzleState != null)
            {
                puzzleState.RespawnPlayer();
            }
            else
            {
                Debug.LogWarning($"{displayName}: puzzleState is not assigned.", this);
            }
        }

        if (disableAfterTouch)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
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

        return other.GetComponent<OrigamiFoldPlayerMover>() != null
            || other.GetComponentInParent<OrigamiFoldPlayerMover>() != null
            || other.GetComponent<OrigamiFoldPassenger>() != null
            || other.GetComponentInParent<OrigamiFoldPassenger>() != null;
    }
}
