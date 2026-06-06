using System.Collections;
using UnityEngine;

[System.Serializable]
public class OrigamiSqueezeTarget
{
    public Transform target;
    public bool animatePosition = true;
    public bool animateScale = true;
    public Vector3 activeLocalPositionOffset;
    public Vector3 activeLocalScale = Vector3.one;
}

public class OrigamiFoldSqueezeAction : MonoBehaviour
{
    public bool isActive;
    public float animationDuration = 0.25f;
    public OrigamiSqueezeTarget[] targets;

    public GameObject[] enableBeforeActive;
    public GameObject[] disableBeforeActive;
    public GameObject[] enableAfterActive;
    public GameObject[] disableAfterActive;

    public GameObject[] enableBeforeInactive;
    public GameObject[] disableBeforeInactive;
    public GameObject[] enableAfterInactive;
    public GameObject[] disableAfterInactive;

    public bool IsAnimating { get; private set; }

    private Vector3[] initialLocalPositions;
    private Vector3[] initialLocalScales;

    private void Awake()
    {
        CaptureInitialTransforms();
    }

    public void SetActive(bool active)
    {
        if (IsAnimating || active == isActive)
        {
            return;
        }

        isActive = active;

        if (active)
        {
            SetObjectsActive(enableBeforeActive, true, nameof(enableBeforeActive));
            SetObjectsActive(disableBeforeActive, false, nameof(disableBeforeActive));
        }
        else
        {
            SetObjectsActive(enableBeforeInactive, true, nameof(enableBeforeInactive));
            SetObjectsActive(disableBeforeInactive, false, nameof(disableBeforeInactive));
        }

        StartCoroutine(AnimateRoutine(active));
    }

    public void Toggle()
    {
        SetActive(!isActive);
    }

    private IEnumerator AnimateRoutine(bool active)
    {
        IsAnimating = true;

        EnsureInitialTransforms();

        float safeDuration = Mathf.Max(0f, animationDuration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            float t = safeDuration <= 0f ? 1f : elapsed / safeDuration;
            ApplyInterpolatedState(active, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ApplyInterpolatedState(active, 1f);

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
    }

    private void ApplyInterpolatedState(bool active, float t)
    {
        if (targets == null)
        {
            Debug.LogWarning($"{name}: targets is not assigned.", this);
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            OrigamiSqueezeTarget squeezeTarget = targets[i];

            if (squeezeTarget == null || squeezeTarget.target == null)
            {
                Debug.LogWarning($"{name}: targets contains an empty slot.", this);
                continue;
            }

            Vector3 initialPosition = initialLocalPositions[i];
            Vector3 initialScale = initialLocalScales[i];
            Vector3 activePosition = initialPosition + squeezeTarget.activeLocalPositionOffset;
            Vector3 activeScale = squeezeTarget.activeLocalScale;
            Vector3 fromPosition = active ? initialPosition : activePosition;
            Vector3 toPosition = active ? activePosition : initialPosition;
            Vector3 fromScale = active ? initialScale : activeScale;
            Vector3 toScale = active ? activeScale : initialScale;

            if (squeezeTarget.animatePosition)
            {
                squeezeTarget.target.localPosition = Vector3.Lerp(fromPosition, toPosition, t);
            }

            if (squeezeTarget.animateScale)
            {
                squeezeTarget.target.localScale = Vector3.Lerp(fromScale, toScale, t);
            }
        }
    }

    private void CaptureInitialTransforms()
    {
        int count = targets == null ? 0 : targets.Length;
        initialLocalPositions = new Vector3[count];
        initialLocalScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            OrigamiSqueezeTarget squeezeTarget = targets[i];

            if (squeezeTarget == null || squeezeTarget.target == null)
            {
                Debug.LogWarning($"{name}: targets contains an empty slot.", this);
                continue;
            }

            initialLocalPositions[i] = squeezeTarget.target.localPosition;
            initialLocalScales[i] = squeezeTarget.target.localScale;
        }
    }

    private void EnsureInitialTransforms()
    {
        int count = targets == null ? 0 : targets.Length;

        if (initialLocalPositions == null
            || initialLocalScales == null
            || initialLocalPositions.Length != count
            || initialLocalScales.Length != count)
        {
            CaptureInitialTransforms();
        }
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
