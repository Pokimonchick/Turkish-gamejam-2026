using System.Collections;
using UnityEngine;

public class OrigamiFoldMapResetter : MonoBehaviour
{
    public OrigamiFoldStripSqueezeAction[] stripActions;
    public bool autoFindActions = true;
    public OrigamiFoldTriadGroup[] triadGroups;
    public bool autoFindTriadGroups = true;
    public OrigamiFoldActionCoordinator coordinator;
    public float resetTimeoutSeconds = 5f;

    public bool IsResetting { get; private set; }

    private void Awake()
    {
        ResolveActionsIfNeeded();
        ResolveTriadGroupsIfNeeded();

        if (coordinator == null)
        {
            coordinator = FindFirstObjectByType<OrigamiFoldActionCoordinator>();
        }
    }

    public Coroutine ResetAllFolds()
    {
        return StartCoroutine(ResetAllFoldsRoutine());
    }

    public IEnumerator ResetAllFoldsRoutine()
    {
        IsResetting = true;
        ResolveActionsIfNeeded();
        ResolveTriadGroupsIfNeeded();

        if (coordinator == null)
        {
            coordinator = FindFirstObjectByType<OrigamiFoldActionCoordinator>();
        }

        if (stripActions != null)
        {
            for (int i = 0; i < stripActions.Length; i++)
            {
                OrigamiFoldStripSqueezeAction action = stripActions[i];

                if (action == null || !action.isActive)
                {
                    continue;
                }

                yield return WaitUntilReady(action, "before reset");

                if (!action.isActive)
                {
                    continue;
                }

                action.SetActive(false);
                yield return WaitUntilReady(action, "after reset");
            }
        }

        if (triadGroups != null)
        {
            for (int i = 0; i < triadGroups.Length; i++)
            {
                OrigamiFoldTriadGroup triadGroup = triadGroups[i];

                if (triadGroup == null
                    || !triadGroup.CanExecute(OrigamiFoldTriadCommand.ResetAll))
                {
                    continue;
                }

                yield return WaitUntilReady(null, "before triad reset");
                triadGroup.Execute(OrigamiFoldTriadCommand.ResetAll);
                yield return WaitUntilTriadReady(triadGroup, "after triad reset");
            }
        }

        IsResetting = false;
    }

    public bool HasActiveFolds()
    {
        ResolveActionsIfNeeded();

        if (stripActions == null)
        {
            return false;
        }

        for (int i = 0; i < stripActions.Length; i++)
        {
            OrigamiFoldStripSqueezeAction action = stripActions[i];

            if (action != null && action.isActive)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveTriadGroupsIfNeeded()
    {
        if (!autoFindTriadGroups && triadGroups != null && triadGroups.Length > 0)
        {
            return;
        }

        if (triadGroups != null && triadGroups.Length > 0)
        {
            return;
        }

        triadGroups = FindObjectsByType<OrigamiFoldTriadGroup>(
            FindObjectsSortMode.None);
    }

    private void ResolveActionsIfNeeded()
    {
        if (!autoFindActions && stripActions != null && stripActions.Length > 0)
        {
            return;
        }

        if (stripActions != null && stripActions.Length > 0)
        {
            return;
        }

        stripActions = FindObjectsByType<OrigamiFoldStripSqueezeAction>(
            FindObjectsSortMode.None);
    }

    private IEnumerator WaitUntilReady(
        OrigamiFoldStripSqueezeAction action,
        string phase)
    {
        float elapsed = 0f;

        while ((action != null && action.IsAnimating)
            || (coordinator != null && coordinator.IsBusy))
        {
            elapsed += Time.deltaTime;

            if (resetTimeoutSeconds > 0f && elapsed >= resetTimeoutSeconds)
            {
                Debug.LogWarning($"{name}: reset timeout {phase}.", this);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator WaitUntilTriadReady(
        OrigamiFoldTriadGroup triadGroup,
        string phase)
    {
        float elapsed = 0f;

        while ((triadGroup != null && triadGroup.isBusy)
            || (coordinator != null && coordinator.IsBusy))
        {
            elapsed += Time.deltaTime;

            if (resetTimeoutSeconds > 0f && elapsed >= resetTimeoutSeconds)
            {
                Debug.LogWarning($"{name}: triad reset timeout {phase}.", this);
                yield break;
            }

            yield return null;
        }
    }
}
