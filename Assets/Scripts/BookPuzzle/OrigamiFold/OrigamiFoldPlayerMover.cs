using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class OrigamiFoldPlayerMover : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float bodyRadius = 0.18f;
    public float sampleProbeRadius = 0.025f;
    public LayerMask walkableMask;
    public bool requireAllSamplesInsideWalkable = true;
    public bool debugDrawSamples = true;

    private Rigidbody2D body;
    private OrigamiFoldPassenger passenger;
    private Vector2 moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        passenger = GetComponent<OrigamiFoldPassenger>();
    }

    private void Update()
    {
        if (OrigamiFoldDialogueGuard.IsDialogueActive())
        {
            moveInput = Vector2.zero;
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

        if (!CanOccupy(currentPosition))
        {
            if (passenger != null)
            {
                passenger.ResolveToNearestWalkable(passenger.resolveMoveDuration);
            }

            moveInput = Vector2.zero;
            return;
        }

        Vector2 moveDelta = moveInput * moveSpeed * Time.fixedDeltaTime;
        Vector2 targetPosition = currentPosition + moveDelta;

        if (CanOccupy(targetPosition))
        {
            body.MovePosition(targetPosition);
            return;
        }

        Vector2 slidePosition = currentPosition;
        bool canSlide = false;

        Vector2 xTarget = currentPosition + new Vector2(moveDelta.x, 0f);

        if (!Mathf.Approximately(moveDelta.x, 0f) && CanOccupy(xTarget))
        {
            slidePosition = xTarget;
            canSlide = true;
        }

        Vector2 yTarget = slidePosition + new Vector2(0f, moveDelta.y);

        if (!Mathf.Approximately(moveDelta.y, 0f) && CanOccupy(yTarget))
        {
            slidePosition = yTarget;
            canSlide = true;
        }

        if (canSlide)
        {
            body.MovePosition(slidePosition);
        }
    }

    public bool CanOccupy(Vector2 targetPosition)
    {
        Vector2[] samples = GetSamplePositions(targetPosition);
        bool hasValidSample = false;

        for (int i = 0; i < samples.Length; i++)
        {
            bool isValid = IsWalkableSample(samples[i]);

            if (isValid)
            {
                hasValidSample = true;
            }
            else if (requireAllSamplesInsideWalkable)
            {
                return false;
            }
        }

        return requireAllSamplesInsideWalkable || hasValidSample;
    }

    private bool IsWalkableSample(Vector2 sample)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            sample,
            sampleProbeRadius,
            walkableMask);

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

            if (area != null && area.isWalkable && hit.OverlapPoint(sample))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2[] GetSamplePositions(Vector2 center)
    {
        float diagonalOffset = bodyRadius * 0.70710678f;

        return new[]
        {
            center,
            center + Vector2.right * bodyRadius,
            center + Vector2.left * bodyRadius,
            center + Vector2.up * bodyRadius,
            center + Vector2.down * bodyRadius,
            center + new Vector2(diagonalOffset, diagonalOffset),
            center + new Vector2(-diagonalOffset, diagonalOffset),
            center + new Vector2(diagonalOffset, -diagonalOffset),
            center + new Vector2(-diagonalOffset, -diagonalOffset)
        };
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bodyRadius);

        if (!debugDrawSamples)
        {
            return;
        }

        Vector2[] samples = GetSamplePositions(transform.position);
        Gizmos.color = Color.cyan;

        for (int i = 0; i < samples.Length; i++)
        {
            Gizmos.DrawWireSphere(samples[i], sampleProbeRadius);
        }
    }
}
