using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFreeRoadMover : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float probeRadius = 0.14f;
    public LayerMask walkableMask;

    private Rigidbody2D body;
    private Vector2 moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    private void Update()
    {
        if (OrigamiFoldDialogueGuard.IsDialogueActive())
        {
            moveInput = Vector2.zero;
            return;
        }

        if (IsReloadPressed())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        moveInput = ReadMoveInput();

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    private void FixedUpdate()
    {
        if (OrigamiFoldDialogueGuard.IsDialogueActive())
        {
            moveInput = Vector2.zero;
            return;
        }

        if (moveInput == Vector2.zero)
        {
            return;
        }

        Vector2 currentPosition = body.position;
        Vector2 moveDelta = moveInput * moveSpeed * Time.fixedDeltaTime;
        Vector2 targetPosition = currentPosition + moveDelta;

        if (CanStandAt(targetPosition))
        {
            body.MovePosition(targetPosition);
            return;
        }

        Vector2 slidePosition = currentPosition;
        bool canSlide = false;

        Vector2 xTarget = currentPosition + new Vector2(moveDelta.x, 0f);

        if (!Mathf.Approximately(moveDelta.x, 0f) && CanStandAt(xTarget))
        {
            slidePosition = xTarget;
            canSlide = true;
        }

        Vector2 yTarget = slidePosition + new Vector2(0f, moveDelta.y);

        if (!Mathf.Approximately(moveDelta.y, 0f) && CanStandAt(yTarget))
        {
            slidePosition = yTarget;
            canSlide = true;
        }

        if (canSlide)
        {
            body.MovePosition(slidePosition);
        }
    }

    private bool CanStandAt(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, probeRadius, walkableMask) != null;
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return input;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            input.y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            input.y -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            input.x += 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            input.x -= 1f;
        }
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            input.y += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            input.y -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            input.x += 1f;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            input.x -= 1f;
        }
#endif

        return input;
    }

    private bool IsReloadPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, probeRadius);
    }
}
