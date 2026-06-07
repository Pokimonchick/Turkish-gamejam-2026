using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OrigamiFoldDragController : MonoBehaviour
{
    public Camera targetCamera;
    public LayerMask foldPointMask = ~0;
    public float snapDistance = 0.45f;
    public LineRenderer shimmerLine;
    public OrigamiFoldLink[] links;
    public bool autoFindLinks = true;
    public float lineWidth = 0.05f;

    private OrigamiFoldPoint startPoint;
    private OrigamiFoldPoint highlightedTarget;
    private OrigamiFoldLink highlightedLink;

    private bool IsDragging => startPoint != null;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (autoFindLinks || links == null || links.Length == 0)
        {
            links = FindObjectsByType<OrigamiFoldLink>(FindObjectsSortMode.None);
        }

        EnsureShimmerLine();
        HideLine();
    }

    private void Update()
    {
        if (OrigamiFoldDialogueGuard.IsDialogueActive())
        {
            CancelDrag();
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector2 mouseWorld = GetMouseWorldPosition();

        if (IsMouseDown())
        {
            TryBeginDrag(mouseWorld);
        }

        if (IsDragging)
        {
            UpdateDrag(mouseWorld);
        }

        if (IsMouseUp())
        {
            EndDrag();
        }
    }

    private void OnDisable()
    {
        CancelDrag();
    }

    public void CancelDrag()
    {
        SetHighlightedTarget(null, null);
        startPoint = null;
        HideLine();
    }

    private void TryBeginDrag(Vector2 mouseWorld)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld, foldPointMask);
        OrigamiFoldPoint point = FindFoldPoint(hits);

        if (point == null)
        {
            return;
        }

        startPoint = point;
        ShowLine();
        SetLinePositions(startPoint.transform.position, mouseWorld);
    }

    private OrigamiFoldPoint FindFoldPoint(Collider2D[] hits)
    {
        if (hits == null)
        {
            return null;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            OrigamiFoldPoint point = hit.GetComponent<OrigamiFoldPoint>();

            if (point == null)
            {
                point = hit.GetComponentInParent<OrigamiFoldPoint>();
            }

            if (point != null)
            {
                return point;
            }
        }

        return null;
    }

    private void UpdateDrag(Vector2 mouseWorld)
    {
        SetLinePositions(startPoint.transform.position, mouseWorld);

        OrigamiFoldPoint nextTarget = null;
        OrigamiFoldLink nextLink = null;
        float bestDistance = snapDistance;

        if (links != null)
        {
            for (int i = 0; i < links.Length; i++)
            {
                OrigamiFoldLink link = links[i];

                if (link == null)
                {
                    continue;
                }

                OrigamiFoldPoint candidate = link.GetOther(startPoint);

                if (candidate == null || !link.IsValidPair(startPoint, candidate))
                {
                    continue;
                }

                float distance = Vector2.Distance(mouseWorld, candidate.transform.position);

                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    nextTarget = candidate;
                    nextLink = link;
                }
            }
        }

        SetHighlightedTarget(nextTarget, nextLink);
    }

    private void EndDrag()
    {
        if (!IsDragging)
        {
            return;
        }

        if (highlightedTarget != null && highlightedLink != null)
        {
            highlightedLink.Execute(startPoint, highlightedTarget);
        }

        SetHighlightedTarget(null, null);
        startPoint = null;
        HideLine();
    }

    private void SetHighlightedTarget(OrigamiFoldPoint target, OrigamiFoldLink link)
    {
        if (highlightedTarget == target)
        {
            highlightedLink = link;
            return;
        }

        if (highlightedTarget != null)
        {
            highlightedTarget.SetHighlighted(false);
        }

        highlightedTarget = target;
        highlightedLink = link;

        if (highlightedTarget != null)
        {
            highlightedTarget.SetHighlighted(true);
        }
    }

    private void EnsureShimmerLine()
    {
        if (shimmerLine == null)
        {
            GameObject lineObject = new GameObject("Generated_Origami_ShimmerLine");
            lineObject.transform.SetParent(transform);

            shimmerLine = lineObject.AddComponent<LineRenderer>();
            shimmerLine.useWorldSpace = true;
            shimmerLine.positionCount = 2;
            shimmerLine.sortingOrder = 100;
            shimmerLine.startColor = Color.cyan;
            shimmerLine.endColor = Color.white;

            Shader shader = FindLineShader();

            if (shader != null)
            {
                shimmerLine.material = new Material(shader);
            }
        }

        shimmerLine.useWorldSpace = true;
        shimmerLine.positionCount = 2;
        shimmerLine.startWidth = lineWidth;
        shimmerLine.endWidth = lineWidth;
    }

    private Shader FindLineShader()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Unlit/Color");
    }

    private void ShowLine()
    {
        if (shimmerLine != null)
        {
            shimmerLine.enabled = true;
        }
    }

    private void HideLine()
    {
        if (shimmerLine != null)
        {
            shimmerLine.enabled = false;
        }
    }

    private void SetLinePositions(Vector2 start, Vector2 end)
    {
        if (shimmerLine == null)
        {
            return;
        }

        shimmerLine.SetPosition(0, start);
        shimmerLine.SetPosition(1, end);
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector2 screenPosition = GetMouseScreenPosition();
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));

        return worldPosition;
    }

    private Vector2 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return Vector2.zero;
        }

        return Mouse.current.position.ReadValue();
#else
        return Input.mousePosition;
#endif
    }

    private bool IsMouseDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool IsMouseUp()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapDistance);
    }
}
