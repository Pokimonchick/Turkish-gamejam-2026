using System.Collections;
using UnityEngine;

public class OrigamiFoldPassenger : MonoBehaviour
{
    public LayerMask walkableMask = ~0;
    public float probeRadius = 0.18f;
    public OrigamiFoldTransformStack currentStack;
    public Behaviour[] disableWhileCarried;
    public bool refreshStackBeforeCarry = true;
    public bool debugLogs = false;

    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
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
        SetCarriedBehavioursEnabled(true);
        RefreshCurrentStack();
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
