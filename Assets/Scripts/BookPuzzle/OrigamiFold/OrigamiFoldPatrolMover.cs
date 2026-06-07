using System.Collections;
using UnityEngine;

public class OrigamiFoldPatrolMover : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 1.2f;
    public float waitAtPointSeconds = 0.15f;
    public bool pingPong = true;
    public bool useLocalSpace = true;
    public bool playOnStart = true;

    public bool IsMoving { get; private set; }

    private Vector3 initialLocalPosition;
    private Vector3 initialWorldPosition;
    private int currentWaypointIndex;
    private int direction;
    private bool isPaused;
    private Coroutine patrolRoutine;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialWorldPosition = transform.position;
        currentWaypointIndex = 0;
        direction = 1;
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartPatrol();
        }
    }

    public void ResetPatrol()
    {
        StopPatrolRoutine();
        currentWaypointIndex = 0;
        direction = 1;
        isPaused = false;
        IsMoving = false;

        if (useLocalSpace)
        {
            transform.localPosition = initialLocalPosition;
        }
        else
        {
            transform.position = initialWorldPosition;
        }

        if (playOnStart && isActiveAndEnabled)
        {
            StartPatrol();
        }
    }

    public void PausePatrol(bool paused)
    {
        isPaused = paused;
        IsMoving = !paused && patrolRoutine != null;
    }

    private void StartPatrol()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"{name}: waypoints are not assigned.", this);
            return;
        }

        StopPatrolRoutine();
        patrolRoutine = StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        IsMoving = true;

        while (true)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                IsMoving = false;
                yield break;
            }

            if (isPaused)
            {
                IsMoving = false;
                yield return null;
                continue;
            }

            IsMoving = true;
            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
            Transform waypoint = waypoints[currentWaypointIndex];

            if (waypoint == null)
            {
                Debug.LogWarning($"{name}: waypoints contains an empty slot.", this);
                SelectNextWaypoint();
                yield return null;
                continue;
            }

            Vector3 targetPosition = useLocalSpace
                ? waypoint.localPosition
                : waypoint.position;

            while (!HasReached(targetPosition))
            {
                if (isPaused)
                {
                    IsMoving = false;
                    yield return null;
                    continue;
                }

                MoveToward(targetPosition);
                yield return null;
            }

            SnapTo(targetPosition);

            if (waitAtPointSeconds > 0f)
            {
                float elapsed = 0f;

                while (elapsed < waitAtPointSeconds)
                {
                    if (!isPaused)
                    {
                        elapsed += Time.deltaTime;
                    }

                    IsMoving = !isPaused;
                    yield return null;
                }
            }

            SelectNextWaypoint();
        }
    }

    private bool HasReached(Vector3 targetPosition)
    {
        Vector3 currentPosition = useLocalSpace
            ? transform.localPosition
            : transform.position;

        return Vector3.Distance(currentPosition, targetPosition) <= 0.01f;
    }

    private void MoveToward(Vector3 targetPosition)
    {
        Vector3 currentPosition = useLocalSpace
            ? transform.localPosition
            : transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (useLocalSpace)
        {
            transform.localPosition = nextPosition;
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    private void SnapTo(Vector3 targetPosition)
    {
        if (useLocalSpace)
        {
            transform.localPosition = targetPosition;
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private void SelectNextWaypoint()
    {
        if (waypoints == null || waypoints.Length <= 1)
        {
            return;
        }

        if (pingPong)
        {
            currentWaypointIndex += direction;

            if (currentWaypointIndex >= waypoints.Length)
            {
                direction = -1;
                currentWaypointIndex = waypoints.Length - 2;
            }
            else if (currentWaypointIndex < 0)
            {
                direction = 1;
                currentWaypointIndex = 1;
            }

            return;
        }

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void StopPatrolRoutine()
    {
        if (patrolRoutine != null)
        {
            StopCoroutine(patrolRoutine);
            patrolRoutine = null;
        }
    }
}
