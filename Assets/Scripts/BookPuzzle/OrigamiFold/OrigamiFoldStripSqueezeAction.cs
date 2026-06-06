using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OrigamiStripContributionTarget
{
    public OrigamiFoldTransformStack stack;
    public Vector3 activeLocalPositionOffset;
    public Vector3 activeLocalScaleMultiplier = Vector3.one;
}

public class OrigamiFoldStripSqueezeAction : MonoBehaviour
{
    public bool isActive;
    public float animationDuration = 0.3f;
    public OrigamiFoldActionCoordinator coordinator;
    public bool useCoordinator = true;
    public OrigamiStripContributionTarget[] targets;

    public GameObject[] enableBeforeActive;
    public GameObject[] disableBeforeActive;
    public GameObject[] enableAfterActive;
    public GameObject[] disableAfterActive;

    public GameObject[] enableBeforeInactive;
    public GameObject[] disableBeforeInactive;
    public GameObject[] enableAfterInactive;
    public GameObject[] disableAfterInactive;

    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        if (useCoordinator && coordinator == null)
        {
            coordinator = FindFirstObjectByType<OrigamiFoldActionCoordinator>();
        }
    }

    public void SetActive(bool active)
    {
        if (IsAnimating || active == isActive)
        {
            return;
        }

        OrigamiFoldActionCoordinator activeCoordinator = GetCoordinator();

        if (activeCoordinator != null && !activeCoordinator.TryBegin(this))
        {
            return;
        }

        isActive = active;

        if (active)
        {
            SetObjectsActive(enableBeforeActive, true, nameof(enableBeforeActive));
            SetObjectsActive(disableBeforeActive, false, nameof(disableBeforeActive));
            ApplyActiveContributions();
        }
        else
        {
            SetObjectsActive(enableBeforeInactive, true, nameof(enableBeforeInactive));
            SetObjectsActive(disableBeforeInactive, false, nameof(disableBeforeInactive));
            ClearContributions();
        }

        StartCoroutine(AnimateRoutine(active, activeCoordinator));
    }

    public void Toggle()
    {
        SetActive(!isActive);
    }

    private IEnumerator AnimateRoutine(bool active, OrigamiFoldActionCoordinator activeCoordinator)
    {
        IsAnimating = true;

        List<OrigamiFoldTransformStack> stacks = CollectUniqueStacks();
        Coroutine[] animations = new Coroutine[stacks.Count];

        for (int i = 0; i < stacks.Count; i++)
        {
            animations[i] = StartCoroutine(stacks[i].AnimateToResolved(animationDuration));
        }

        for (int i = 0; i < animations.Length; i++)
        {
            yield return animations[i];
        }

        if (active)
        {
            SetObjectsActive(enableAfterActive, true, nameof(enableAfterActive));
            SetObjectsActive(disableAfterActive, false, nameof(disableAfterActive));
        }
        else
        {
            SetObjectsActive(enableAfterInactive, true, nameof(enableAfterInactive));
            SetObjectsActive(disableAfterInactive, false, nameof(disableAfterInactive));
        }

        IsAnimating = false;

        if (activeCoordinator != null)
        {
            activeCoordinator.End(this);
        }
    }

    private void ApplyActiveContributions()
    {
        if (targets == null)
        {
            Debug.LogWarning($"{name}: targets is not assigned.", this);
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            OrigamiStripContributionTarget target = targets[i];

            if (target == null || target.stack == null)
            {
                Debug.LogWarning($"{name}: targets contains an empty stack.", this);
                continue;
            }

            target.stack.SetContribution(
                this,
                target.activeLocalPositionOffset,
                target.activeLocalScaleMultiplier);
        }
    }

    private void ClearContributions()
    {
        if (targets == null)
        {
            Debug.LogWarning($"{name}: targets is not assigned.", this);
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            OrigamiStripContributionTarget target = targets[i];

            if (target == null || target.stack == null)
            {
                Debug.LogWarning($"{name}: targets contains an empty stack.", this);
                continue;
            }

            target.stack.ClearContribution(this);
        }
    }

    private List<OrigamiFoldTransformStack> CollectUniqueStacks()
    {
        List<OrigamiFoldTransformStack> stacks = new List<OrigamiFoldTransformStack>();

        if (targets == null)
        {
            return stacks;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            OrigamiStripContributionTarget target = targets[i];

            if (target == null || target.stack == null)
            {
                continue;
            }

            if (!stacks.Contains(target.stack))
            {
                stacks.Add(target.stack);
            }
        }

        return stacks;
    }

    private OrigamiFoldActionCoordinator GetCoordinator()
    {
        if (!useCoordinator)
        {
            return null;
        }

        if (coordinator == null)
        {
            coordinator = FindFirstObjectByType<OrigamiFoldActionCoordinator>();
        }

        return coordinator;
    }

    private void SetObjectsActive(GameObject[] objects, bool active, string fieldName)
    {
        if (objects == null)
        {
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
}
