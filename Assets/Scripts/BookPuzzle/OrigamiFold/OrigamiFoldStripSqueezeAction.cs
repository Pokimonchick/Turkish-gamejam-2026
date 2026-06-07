using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OrigamiStripContributionTarget
{
    public OrigamiFoldTransformStack stack;
    public Vector3 activeLocalPositionOffset;
    public Vector3 activeLocalScaleMultiplier = Vector3.one;
    public bool overridePassengerCarryOffset;
    public Vector3 passengerActiveLocalPositionOffset;
}

public class OrigamiFoldStripSqueezeAction : MonoBehaviour
{
    public bool isActive;
    public float animationDuration = 0.3f;
    public AudioClip foldSound;
    [Range(0f, 1f)] public float foldSoundVolume = 1f;
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
    public OrigamiFoldTrapTarget[] trapTargetsWhenActive;

    public bool IsAnimating { get; private set; }

    private struct PassengerCarry
    {
        public OrigamiFoldPassenger passenger;
        public Vector3 worldOffset;
    }

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

        PassengerCarry[] passengerCarries = CollectPassengerCarries(active);
        isActive = active;
        PlayFoldSound();

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

        StartCoroutine(AnimateRoutine(active, activeCoordinator, passengerCarries));
    }

    public void Toggle()
    {
        SetActive(!isActive);
    }

    private IEnumerator AnimateRoutine(
        bool active,
        OrigamiFoldActionCoordinator activeCoordinator,
        PassengerCarry[] passengerCarries)
    {
        IsAnimating = true;

        List<OrigamiFoldTransformStack> stacks = CollectUniqueStacks();
        int carryCount = passengerCarries == null ? 0 : passengerCarries.Length;
        Coroutine[] animations = new Coroutine[stacks.Count + carryCount];
        int animationIndex = 0;

        for (int i = 0; i < stacks.Count; i++)
        {
            animations[animationIndex] = StartCoroutine(stacks[i].AnimateToResolved(animationDuration));
            animationIndex++;
        }

        for (int i = 0; i < carryCount; i++)
        {
            PassengerCarry carry = passengerCarries[i];

            if (carry.passenger != null)
            {
                animations[animationIndex] = carry.passenger.CarryBy(
                    carry.worldOffset,
                    animationDuration);
                animationIndex++;
            }
        }

        for (int i = 0; i < animations.Length; i++)
        {
            if (animations[i] != null)
            {
                yield return animations[i];
            }
        }

        yield return ResolvePassengerPlacements();

        if (active)
        {
            SetObjectsActive(enableAfterActive, true, nameof(enableAfterActive));
            SetObjectsActive(disableAfterActive, false, nameof(disableAfterActive));
            SetTrapTargets(true);
        }
        else
        {
            SetObjectsActive(enableAfterInactive, true, nameof(enableAfterInactive));
            SetObjectsActive(disableAfterInactive, false, nameof(disableAfterInactive));
            SetTrapTargets(false);
        }

        IsAnimating = false;

        if (activeCoordinator != null)
        {
            activeCoordinator.End(this);
        }
    }

    private PassengerCarry[] CollectPassengerCarries(bool active)
    {
        OrigamiFoldPassenger[] passengers = FindObjectsByType<OrigamiFoldPassenger>(
            FindObjectsSortMode.None);

        if (passengers == null || passengers.Length == 0 || targets == null)
        {
            return new PassengerCarry[0];
        }

        List<PassengerCarry> carries = new List<PassengerCarry>();

        for (int i = 0; i < passengers.Length; i++)
        {
            OrigamiFoldPassenger passenger = passengers[i];

            if (passenger == null)
            {
                continue;
            }

            if (passenger.refreshStackBeforeCarry)
            {
                passenger.RefreshCurrentStack();
            }

            if (!passenger.TryGetCurrentStack(out OrigamiFoldTransformStack passengerStack))
            {
                continue;
            }

            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                OrigamiStripContributionTarget target = targets[targetIndex];

                if (target == null || target.stack != passengerStack)
                {
                    continue;
                }

                Vector3 localOffset = active
                    ? GetPassengerCarryOffset(target)
                    : -GetPassengerCarryOffset(target);
                Vector3 worldOffset = LocalOffsetToWorldOffset(target.stack, localOffset);

                carries.Add(new PassengerCarry
                {
                    passenger = passenger,
                    worldOffset = worldOffset
                });
                break;
            }
        }

        return carries.ToArray();
    }

    private IEnumerator ResolvePassengerPlacements()
    {
        OrigamiFoldPassenger[] passengers = FindObjectsByType<OrigamiFoldPassenger>(
            FindObjectsSortMode.None);

        if (passengers == null || passengers.Length == 0)
        {
            yield break;
        }

        Coroutine[] resolves = new Coroutine[passengers.Length];
        int resolveCount = 0;

        for (int i = 0; i < passengers.Length; i++)
        {
            OrigamiFoldPassenger passenger = passengers[i];

            if (passenger == null)
            {
                continue;
            }

            Coroutine resolve = passenger.ResolveToNearestWalkable(
                passenger.resolveMoveDuration);

            if (resolve != null)
            {
                resolves[resolveCount] = resolve;
                resolveCount++;
            }
        }

        for (int i = 0; i < resolveCount; i++)
        {
            if (resolves[i] != null)
            {
                yield return resolves[i];
            }
        }
    }

    private static Vector3 GetPassengerCarryOffset(OrigamiStripContributionTarget target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        return target.overridePassengerCarryOffset
            ? target.passengerActiveLocalPositionOffset
            : target.activeLocalPositionOffset;
    }

    private Vector3 LocalOffsetToWorldOffset(
        OrigamiFoldTransformStack stack,
        Vector3 localOffset)
    {
        if (stack == null || stack.transform.parent == null)
        {
            return localOffset;
        }

        return stack.transform.parent.TransformVector(localOffset);
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

    private void SetTrapTargets(bool trapped)
    {
        if (trapTargetsWhenActive == null)
        {
            return;
        }

        for (int i = 0; i < trapTargetsWhenActive.Length; i++)
        {
            OrigamiFoldTrapTarget trapTarget = trapTargetsWhenActive[i];

            if (trapTarget == null)
            {
                Debug.LogWarning($"{name}: trapTargetsWhenActive contains an empty slot.", this);
                continue;
            }

            trapTarget.SetTrapped(trapped);
        }
    }

    private void PlayFoldSound()
    {
        if (foldSound == null)
        {
            return;
        }

        GameAudioManager.Instance.PlaySfx(foldSound, foldSoundVolume);
    }
}
