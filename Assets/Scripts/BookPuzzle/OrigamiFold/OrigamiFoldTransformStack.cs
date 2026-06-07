using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct OrigamiTransformContribution
{
    public Object owner;
    public Vector3 localPositionOffset;
    public Vector3 localScaleMultiplier;
}

public class OrigamiFoldTransformStack : MonoBehaviour
{
    public Vector3 baseLocalPosition;
    public Vector3 baseLocalScale;

    private readonly List<OrigamiTransformContribution> contributions =
        new List<OrigamiTransformContribution>();

    private void Awake()
    {
        CaptureBaseTransform();
    }

    public void CaptureBaseTransform()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;
    }

    public void SetContribution(
        Object owner,
        Vector3 localPositionOffset,
        Vector3 localScaleMultiplier)
    {
        if (owner == null)
        {
            Debug.LogWarning($"{name}: contribution owner is not assigned.", this);
            return;
        }

        OrigamiTransformContribution contribution = new OrigamiTransformContribution
        {
            owner = owner,
            localPositionOffset = localPositionOffset,
            localScaleMultiplier = localScaleMultiplier
        };

        for (int i = 0; i < contributions.Count; i++)
        {
            if (contributions[i].owner == owner)
            {
                contributions[i] = contribution;
                return;
            }
        }

        contributions.Add(contribution);
    }

    public void ClearContribution(Object owner)
    {
        if (owner == null)
        {
            Debug.LogWarning($"{name}: contribution owner is not assigned.", this);
            return;
        }

        for (int i = contributions.Count - 1; i >= 0; i--)
        {
            if (contributions[i].owner == owner)
            {
                contributions.RemoveAt(i);
            }
        }
    }

    public Vector3 GetResolvedLocalPosition()
    {
        Vector3 resolved = baseLocalPosition;

        for (int i = 0; i < contributions.Count; i++)
        {
            resolved += contributions[i].localPositionOffset;
        }

        return resolved;
    }

    public Vector3 GetResolvedLocalScale()
    {
        Vector3 resolved = baseLocalScale;

        for (int i = 0; i < contributions.Count; i++)
        {
            Vector3 multiplier = contributions[i].localScaleMultiplier;
            resolved = new Vector3(
                resolved.x * multiplier.x,
                resolved.y * multiplier.y,
                resolved.z * multiplier.z);
        }

        return resolved;
    }

    public IEnumerator AnimateToResolved(float duration)
    {
        if (duration <= 0f)
        {
            SnapToResolved();
            yield break;
        }

        Vector3 startPosition = transform.localPosition;
        Vector3 startScale = transform.localScale;
        Vector3 targetPosition = GetResolvedLocalPosition();
        Vector3 targetScale = GetResolvedLocalScale();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SnapToResolved();
    }

    public void SnapToResolved()
    {
        transform.localPosition = GetResolvedLocalPosition();
        transform.localScale = GetResolvedLocalScale();
    }
}
