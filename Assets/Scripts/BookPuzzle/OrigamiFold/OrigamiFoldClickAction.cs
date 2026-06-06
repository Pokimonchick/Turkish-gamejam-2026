using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider2D))]
public class OrigamiFoldClickAction : MonoBehaviour
{
    public Camera targetCamera;
    public OrigamiFoldMoveAction targetMoveAction;
    public OrigamiFoldSqueezeAction targetSqueezeAction;
    public bool activeStateOnClick = false;
    public bool ignoreWhileActionAnimating = true;
    public string debugName;

    private Collider2D clickCollider;
    private bool mouseDownInside;

    private void Awake()
    {
        clickCollider = GetComponent<Collider2D>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || clickCollider == null)
        {
            return;
        }

        Vector2 mouseWorldPosition = GetMouseWorldPosition();

        if (IsMouseDown())
        {
            mouseDownInside = clickCollider.OverlapPoint(mouseWorldPosition);
        }

        if (IsMouseUp())
        {
            bool mouseUpInside = clickCollider.OverlapPoint(mouseWorldPosition);

            if (mouseDownInside && mouseUpInside)
            {
                ExecuteClick();
            }

            mouseDownInside = false;
        }
    }

    private void ExecuteClick()
    {
        if (targetMoveAction == null && targetSqueezeAction == null)
        {
            Debug.LogWarning($"{GetDebugName()}: no fold action is assigned.", this);
            return;
        }

        if (ignoreWhileActionAnimating
            && targetMoveAction != null
            && targetMoveAction.board != null
            && targetMoveAction.board.IsAnimating)
        {
            return;
        }

        if (ignoreWhileActionAnimating
            && targetSqueezeAction != null
            && targetSqueezeAction.IsAnimating)
        {
            return;
        }

        if (targetMoveAction != null)
        {
            targetMoveAction.SetActive(activeStateOnClick);
        }

        if (targetSqueezeAction != null)
        {
            targetSqueezeAction.SetActive(activeStateOnClick);
        }

        Debug.Log($"{GetDebugName()}: clicked, setting fold action active={activeStateOnClick}.", this);
    }

    private string GetDebugName()
    {
        if (!string.IsNullOrEmpty(debugName))
        {
            return debugName;
        }

        return name;
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
        Collider2D selectedCollider = clickCollider;

        if (selectedCollider == null)
        {
            selectedCollider = GetComponent<Collider2D>();
        }

        if (selectedCollider == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(selectedCollider.bounds.center, selectedCollider.bounds.size);
    }
}
