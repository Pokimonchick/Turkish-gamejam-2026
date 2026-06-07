using System.Collections;
using UnityEngine;

public enum OrigamiFoldTriadState
{
    Unfolded,
    HorizontalFolded,
    VerticalFolded,
    BothFolded
}

public enum OrigamiFoldTriadCommand
{
    FoldHorizontal,
    FoldVertical,
    ResetHorizontal,
    ResetVertical,
    ResetAll
}

public class OrigamiFoldTriadGroup : MonoBehaviour
{
    public OrigamiFoldTriadState state;
    public OrigamiFoldStripSqueezeAction horizontalAction;
    public OrigamiFoldStripSqueezeAction verticalAction;
    public OrigamiFoldActionCoordinator coordinator;
    public bool isBusy;
    public bool allowSecondFold = true;

    public GameObject[] visibleWhenUnfolded;
    public GameObject[] visibleWhenHorizontalFolded;
    public GameObject[] visibleWhenVerticalFolded;
    public GameObject[] visibleWhenBothFolded;

    private void Awake()
    {
        if (coordinator == null)
        {
            coordinator = FindFirstObjectByType<OrigamiFoldActionCoordinator>();
        }

        ApplyVisibility();
    }

    public void Execute(OrigamiFoldTriadCommand command)
    {
        if (isBusy || !CanExecute(command) || AnyActionAnimating())
        {
            return;
        }

        StartCoroutine(ExecuteRoutine(command));
    }

    public void ApplyVisibility()
    {
        SetObjectsActive(visibleWhenUnfolded, state == OrigamiFoldTriadState.Unfolded);
        SetObjectsActive(
            visibleWhenHorizontalFolded,
            state == OrigamiFoldTriadState.HorizontalFolded);
        SetObjectsActive(
            visibleWhenVerticalFolded,
            state == OrigamiFoldTriadState.VerticalFolded);
        SetObjectsActive(visibleWhenBothFolded, state == OrigamiFoldTriadState.BothFolded);
    }

    public bool CanExecute(OrigamiFoldTriadCommand command)
    {
        switch (command)
        {
            case OrigamiFoldTriadCommand.FoldHorizontal:
                return state == OrigamiFoldTriadState.Unfolded
                    || (allowSecondFold && state == OrigamiFoldTriadState.VerticalFolded);

            case OrigamiFoldTriadCommand.FoldVertical:
                return state == OrigamiFoldTriadState.Unfolded
                    || (allowSecondFold && state == OrigamiFoldTriadState.HorizontalFolded);

            case OrigamiFoldTriadCommand.ResetHorizontal:
                return state == OrigamiFoldTriadState.HorizontalFolded
                    || state == OrigamiFoldTriadState.BothFolded;

            case OrigamiFoldTriadCommand.ResetVertical:
                return state == OrigamiFoldTriadState.VerticalFolded
                    || state == OrigamiFoldTriadState.BothFolded;

            case OrigamiFoldTriadCommand.ResetAll:
                return state != OrigamiFoldTriadState.Unfolded
                    || IsActionActive(horizontalAction)
                    || IsActionActive(verticalAction);

            default:
                return false;
        }
    }

    private IEnumerator ExecuteRoutine(OrigamiFoldTriadCommand command)
    {
        isBusy = true;

        if (coordinator == null)
        {
            coordinator = FindFirstObjectByType<OrigamiFoldActionCoordinator>();
        }

        yield return WaitForCoordinator();

        switch (command)
        {
            case OrigamiFoldTriadCommand.FoldHorizontal:
                yield return SetActionActive(horizontalAction, true, "horizontalAction");
                state = state == OrigamiFoldTriadState.VerticalFolded
                    ? OrigamiFoldTriadState.BothFolded
                    : OrigamiFoldTriadState.HorizontalFolded;
                break;

            case OrigamiFoldTriadCommand.FoldVertical:
                yield return SetActionActive(verticalAction, true, "verticalAction");
                state = state == OrigamiFoldTriadState.HorizontalFolded
                    ? OrigamiFoldTriadState.BothFolded
                    : OrigamiFoldTriadState.VerticalFolded;
                break;

            case OrigamiFoldTriadCommand.ResetHorizontal:
                yield return SetActionActive(horizontalAction, false, "horizontalAction");
                state = state == OrigamiFoldTriadState.BothFolded
                    ? OrigamiFoldTriadState.VerticalFolded
                    : OrigamiFoldTriadState.Unfolded;
                break;

            case OrigamiFoldTriadCommand.ResetVertical:
                yield return SetActionActive(verticalAction, false, "verticalAction");
                state = state == OrigamiFoldTriadState.BothFolded
                    ? OrigamiFoldTriadState.HorizontalFolded
                    : OrigamiFoldTriadState.Unfolded;
                break;

            case OrigamiFoldTriadCommand.ResetAll:
                if (IsActionActive(horizontalAction))
                {
                    yield return SetActionActive(horizontalAction, false, "horizontalAction");
                }

                if (IsActionActive(verticalAction))
                {
                    yield return SetActionActive(verticalAction, false, "verticalAction");
                }

                state = OrigamiFoldTriadState.Unfolded;
                break;
        }

        ApplyVisibility();
        isBusy = false;
    }

    private IEnumerator SetActionActive(
        OrigamiFoldStripSqueezeAction action,
        bool active,
        string fieldName)
    {
        if (action == null)
        {
            Debug.LogWarning($"{name}: {fieldName} is not assigned.", this);
            yield break;
        }

        if (action.isActive == active)
        {
            yield break;
        }

        yield return WaitForCoordinator();

        action.SetActive(active);

        while (action != null && action.IsAnimating)
        {
            yield return null;
        }

        yield return WaitForCoordinator();
    }

    private IEnumerator WaitForCoordinator()
    {
        while (coordinator != null && coordinator.IsBusy)
        {
            yield return null;
        }
    }

    private bool AnyActionAnimating()
    {
        return IsActionAnimating(horizontalAction) || IsActionAnimating(verticalAction);
    }

    private bool IsActionAnimating(OrigamiFoldStripSqueezeAction action)
    {
        return action != null && action.IsAnimating;
    }

    private bool IsActionActive(OrigamiFoldStripSqueezeAction action)
    {
        return action != null && action.isActive;
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject item = objects[i];

            if (item != null)
            {
                item.SetActive(active);
            }
        }
    }
}
