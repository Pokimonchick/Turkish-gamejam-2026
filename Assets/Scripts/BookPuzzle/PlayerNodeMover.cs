using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerNodeMover : MonoBehaviour
{
    public PathNode currentNode;
    public float moveSpeed = 5f;

    private PathEdge[] edges;
    private PathNode targetNode;

    private bool IsMoving => targetNode != null;

    private void Awake()
    {
        edges = FindObjectsByType<PathEdge>(FindObjectsSortMode.None);

        if (currentNode != null)
        {
            transform.position = currentNode.transform.position;
        }
    }

    private void Update()
    {
        if (IsReloadPressed())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (IsMoving)
        {
            MoveToTarget();
            return;
        }

        Vector2 inputDirection = ReadMoveDirection();

        if (inputDirection != Vector2.zero)
        {
            TryStartMove(inputDirection);
        }
    }

    private void MoveToTarget()
    {
        Vector3 targetPosition = targetNode.transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) <= 0.001f)
        {
            transform.position = targetPosition;
            currentNode = targetNode;
            targetNode = null;
        }
    }

    private void TryStartMove(Vector2 inputDirection)
    {
        if (currentNode == null || edges == null)
        {
            return;
        }

        PathNode bestNode = null;
        float bestDot = 0f;

        for (int i = 0; i < edges.Length; i++)
        {
            PathEdge edge = edges[i];

            if (edge == null || !edge.isOpen)
            {
                continue;
            }

            PathNode neighbor = edge.Other(currentNode);

            if (neighbor == null)
            {
                continue;
            }

            Vector2 directionToNeighbor = neighbor.transform.position - currentNode.transform.position;

            if (directionToNeighbor == Vector2.zero)
            {
                continue;
            }

            float dot = Vector2.Dot(inputDirection.normalized, directionToNeighbor.normalized);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestNode = neighbor;
            }
        }

        if (bestNode != null)
        {
            targetNode = bestNode;
        }
    }

    private Vector2 ReadMoveDirection()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            return Vector2.up;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            return Vector2.right;
        }

        if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            return Vector2.down;
        }

        if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            return Vector2.left;
        }
#else
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            return Vector2.up;
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            return Vector2.right;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            return Vector2.down;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            return Vector2.left;
        }
#endif

        return Vector2.zero;
    }

    private bool IsReloadPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}
