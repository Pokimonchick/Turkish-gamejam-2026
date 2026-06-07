using UnityEngine;

public class OrigamiFoldLink : MonoBehaviour
{
    public OrigamiFoldPoint pointA;
    public OrigamiFoldPoint pointB;
    public bool bidirectional = true;
    public GameObject[] enableOnExecute;
    public GameObject[] disableOnExecute;
    public bool executed;
    public OrigamiFoldMoveAction targetMoveAction;
    public OrigamiFoldSqueezeAction targetSqueezeAction;
    public OrigamiFoldStripSqueezeAction targetStripSqueezeAction;
    public OrigamiFoldTriadGroup targetTriadGroup;
    public OrigamiFoldTriadCommand triadCommand;
    public bool activeStateOnExecute = true;

    private bool warnedMissingPoints;

    public bool IsValidPair(OrigamiFoldPoint start, OrigamiFoldPoint end)
    {
        if (pointA == null || pointB == null)
        {
            WarnMissingPoints();
            return false;
        }

        if (start == null || end == null)
        {
            Debug.LogWarning($"{name}: start or end point is not assigned.", this);
            return false;
        }

        if (start == pointA && end == pointB)
        {
            return true;
        }

        return bidirectional && start == pointB && end == pointA;
    }

    public OrigamiFoldPoint GetOther(OrigamiFoldPoint point)
    {
        if (pointA == null || pointB == null)
        {
            WarnMissingPoints();
            return null;
        }

        if (point == pointA)
        {
            return pointB;
        }

        if (bidirectional && point == pointB)
        {
            return pointA;
        }

        return null;
    }

    public void Execute(OrigamiFoldPoint start, OrigamiFoldPoint end)
    {
        if (!IsValidPair(start, end))
        {
            Debug.LogWarning($"{name}: cannot execute invalid origami fold pair.", this);
            return;
        }

        executed = true;
        SetObjectsActive(enableOnExecute, true, nameof(enableOnExecute));
        SetObjectsActive(disableOnExecute, false, nameof(disableOnExecute));

        if (targetMoveAction != null)
        {
            targetMoveAction.SetActive(activeStateOnExecute);
        }

        if (targetSqueezeAction != null)
        {
            targetSqueezeAction.SetActive(activeStateOnExecute);
        }

        if (targetStripSqueezeAction != null)
        {
            targetStripSqueezeAction.SetActive(activeStateOnExecute);
        }

        if (targetTriadGroup != null)
        {
            targetTriadGroup.Execute(triadCommand);
        }

        Debug.Log($"{name}: executed origami fold link {GetPointName(start)} -> {GetPointName(end)}.", this);
    }

    private void SetObjectsActive(GameObject[] objects, bool active, string fieldName)
    {
        if (objects == null)
        {
            Debug.LogWarning($"{name}: {fieldName} is not assigned.", this);
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject item = objects[i];

            if (item == null)
            {
                Debug.LogWarning($"{name}: {fieldName} contains an empty slot.", this);
                continue;
            }

            item.SetActive(active);
        }
    }

    private void WarnMissingPoints()
    {
        if (warnedMissingPoints)
        {
            return;
        }

        warnedMissingPoints = true;
        Debug.LogWarning($"{name}: pointA or pointB is not assigned.", this);
    }

    private string GetPointName(OrigamiFoldPoint point)
    {
        if (point == null)
        {
            return "<missing>";
        }

        if (!string.IsNullOrEmpty(point.pointId))
        {
            return point.pointId;
        }

        return point.name;
    }
}
