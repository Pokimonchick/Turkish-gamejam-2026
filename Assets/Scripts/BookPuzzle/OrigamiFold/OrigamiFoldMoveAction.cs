using UnityEngine;

public class OrigamiFoldMoveAction : MonoBehaviour
{
    public OrigamiFoldBoard board;
    public bool isActive;
    public OrigamiCellMove[] movesWhenActive;
    public OrigamiCellMove[] movesWhenInactive;
    public GameObject[] enableWhenActive;
    public GameObject[] disableWhenActive;
    public GameObject[] enableWhenInactive;
    public GameObject[] disableWhenInactive;

    public void SetActive(bool active)
    {
        if (active == isActive)
        {
            return;
        }

        if (board == null)
        {
            board = FindFirstObjectByType<OrigamiFoldBoard>();
        }

        if (board == null)
        {
            Debug.LogWarning($"{name}: cannot run origami fold move action because board is not assigned.", this);
            return;
        }

        if (board.IsAnimating)
        {
            return;
        }

        isActive = active;

        if (isActive)
        {
            SetObjectsActive(enableWhenActive, true);
            SetObjectsActive(disableWhenActive, false);
            board.AnimateMoves(movesWhenActive);
        }
        else
        {
            SetObjectsActive(enableWhenInactive, true);
            SetObjectsActive(disableWhenInactive, false);
            board.AnimateMoves(movesWhenInactive);
        }
    }

    public void Toggle()
    {
        SetActive(!isActive);
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }
}
