using System.Collections;
using UnityEngine;

public class OrigamiFoldPassenger : MonoBehaviour
{
    public LayerMask walkableMask = ~0;
    public float probeRadius = 0.18f;
    public OrigamiFoldTransformStack currentStack;
    public Behaviour[] disableWhileCarried;
    public bool refreshStackBeforeCarry = true;
    public bool resolveToWalkableAfterCarry = true;
    public float resolveSearchRadius = 1.25f;
    public float resolveSearchStep = 0.08f;
    public int resolveDirectionCount = 16;
    public float resolveMoveDuration = 0.1f;
    public bool debugLogs = false;

    private Rigidbody2D body;
    private OrigamiFoldPlayerMover playerMover;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerMover = GetComponent<OrigamiFoldPlayerMover>();
    }

    public void RefreshCurrentStack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            probeRadius,
            walkableMask);

        currentStack = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            OrigamiFoldWalkableArea area = hit.GetComponent<OrigamiFoldWalkableArea>();

            if (area == null)
            {
                area = hit.GetComponentInParent<OrigamiFoldWalkableArea>();
            }

            if (area == null || !area.isWalkable || area.ownerStack == null)
            {
                continue;
            }

            currentStack = area.ownerStack;

            if (debugLogs)
            {
                Debug.Log($"{name}: current stack is {currentStack.name}.", this);
            }

            return;
        }

        if (debugLogs)
        {
            Debug.Log($"{name}: no current walkable stack found.", this);
        }
    }

    public bool TryGetCurrentStack(out OrigamiFoldTransformStack stack)
    {
        stack = currentStack;
        return stack != null;
    }

    public Coroutine CarryBy(Vector3 worldOffset, float duration)
    {
        if (duration <= 0f)
        {
            MoveTo(transform.position + worldOffset);
            RefreshCurrentStack();
            ResolveToNearestWalkable(0f);
            return null;
        }

        return StartCoroutine(CarryByRoutine(worldOffset, duration));
    }

    public IEnumerator CarryByRoutine(Vector3 worldOffset, float duration)
    {
        SetCarriedBehavioursEnabled(false);

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + worldOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            MoveTo(Vector3.Lerp(startPosition, targetPosition, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        MoveTo(targetPosition);
        RefreshCurrentStack();
        yield return ResolveToNearestWalkableRoutine(resolveMoveDuration);
        SetCarriedBehavioursEnabled(true);
    }

    public Coroutine ResolveToNearestWalkable(float duration)
    {
        if (!resolveToWalkableAfterCarry)
        {
            RefreshCurrentStack();
            return null;
        }

        if (!TryFindNearestWalkablePosition(out Vector3 targetPosition))
        {
            RefreshCurrentStack();
            return null;
        }

        if (duration <= 0f)
        {
            MoveTo(targetPosition);
            RefreshCurrentStack();
            return null;
        }

        return StartCoroutine(ResolveToNearestWalkableRoutine(duration));
    }

    public IEnumerator ResolveToNearestWalkableRoutine(float duration)
    {
        if (!resolveToWalkableAfterCarry)
        {
            RefreshCurrentStack();
            yield break;
        }

        if (!TryFindNearestWalkablePosition(out Vector3 targetPosition))
        {
            RefreshCurrentStack();
            yield break;
        }

        SetCarriedBehavioursEnabled(false);

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = duration <= 0f ? 1f : elapsed / duration;
            MoveTo(Vector3.Lerp(startPosition, targetPosition, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        MoveTo(targetPosition);
        RefreshCurrentStack();
        SetCarriedBehavioursEnabled(true);
    }

    private bool TryFindNearestWalkablePosition(out Vector3 position)
    {
        position = transform.position;

        if (CanOccupy(position))
        {
            return false;
        }

        if (TryFindWalkableColliderCenter(out position))
        {
            return true;
        }

        float step = Mathf.Max(0.02f, resolveSearchStep);
        int directionCount = Mathf.Max(8, resolveDirectionCount);

        float searchRadius = GetResolveSearchRadius();

        for (float radius = step; radius <= searchRadius; radius += step)
        {
            for (int i = 0; i < directionCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / directionCount;
                Vector3 candidate = transform.position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);

                if (CanOccupy(candidate))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        if (debugLogs)
        {
            Debug.Log($"{name}: no nearby walkable rescue position found.", this);
        }

        return false;
    }

    private bool TryFindWalkableColliderCenter(out Vector3 position)
    {
        position = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            GetResolveSearchRadius(),
            walkableMask);

        float bestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (!IsWalkableHit(hit))
            {
                continue;
            }

            Vector3 candidate = hit.bounds.center;

            if (!CanOccupy(candidate))
            {
                continue;
            }

            float distance = (candidate - transform.position).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                position = candidate;
                found = true;
            }
        }

        return found;
    }

    private float GetResolveSearchRadius()
    {
        return Mathf.Max(resolveSearchRadius, 1.25f);
    }

    private bool CanOccupy(Vector3 position)
    {
        if (playerMover != null)
        {
            return playerMover.CanOccupy(position);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            position,
            probeRadius,
            walkableMask);

        for (int i = 0; i < hits.Length; i++)
        {
            if (IsWalkableHit(hits[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWalkableHit(Collider2D hit)
    {
        if (hit == null)
        {
            return false;
        }

        OrigamiFoldWalkableArea area = hit.GetComponent<OrigamiFoldWalkableArea>();

        if (area == null)
        {
            area = hit.GetComponentInParent<OrigamiFoldWalkableArea>();
        }

        return area != null && area.isWalkable && area.ownerStack != null;
    }

    private void MoveTo(Vector3 position)
    {
        if (body != null)
        {
            body.MovePosition((Vector2)position);
            return;
        }

        transform.position = position;
    }

    private void SetCarriedBehavioursEnabled(bool enabled)
    {
        if (disableWhileCarried == null)
        {
            return;
        }

        for (int i = 0; i < disableWhileCarried.Length; i++)
        {
            Behaviour item = disableWhileCarried[i];

            if (item != null)
            {
                item.enabled = enabled;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, probeRadius);
    }
}
