using UnityEngine;

public class OrigamiFoldTrapTarget : MonoBehaviour
{
    public GameObject activeRoot;
    public GameObject trappedRoot;
    public Collider2D[] hazardColliders;
    public OrigamiFoldPatrolMover patrolMover;
    public bool resetPatrolOnUntrap = true;
    public bool pausePatrolWhenTrapped = true;
    public bool isTrapped;

    private void Awake()
    {
        ResolveHazardCollidersIfNeeded();
        ApplyTrappedState(isTrapped, false);
    }

    private void OnEnable()
    {
        ApplyTrappedState(isTrapped, false);
    }

    public void SetTrapped(bool trapped)
    {
        isTrapped = trapped;
        ResolveHazardCollidersIfNeeded();
        ApplyTrappedState(trapped, true);
        Debug.Log($"{name}: trap state set to {trapped}.", this);
    }

    private void ApplyTrappedState(bool trapped, bool applyPatrolReset)
    {
        if (trapped)
        {
            if (pausePatrolWhenTrapped && patrolMover != null)
            {
                patrolMover.PausePatrol(true);
            }

            SetHazardCollidersEnabled(false);

            if (activeRoot != null)
            {
                activeRoot.SetActive(false);
            }

            if (trappedRoot != null)
            {
                trappedRoot.SetActive(true);
            }

            return;
        }

        if (activeRoot != null)
        {
            activeRoot.SetActive(true);
        }

        if (trappedRoot != null)
        {
            trappedRoot.SetActive(false);
        }

        SetHazardCollidersEnabled(true);

        if (patrolMover != null)
        {
            if (applyPatrolReset && resetPatrolOnUntrap)
            {
                patrolMover.ResetPatrol();
            }

            patrolMover.PausePatrol(false);
        }
    }

    private void ResolveHazardCollidersIfNeeded()
    {
        if (hazardColliders != null && hazardColliders.Length > 0)
        {
            return;
        }

        if (activeRoot != null)
        {
            hazardColliders = activeRoot.GetComponentsInChildren<Collider2D>(true);
            return;
        }

        hazardColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void SetHazardCollidersEnabled(bool enabled)
    {
        if (hazardColliders == null)
        {
            return;
        }

        for (int i = 0; i < hazardColliders.Length; i++)
        {
            Collider2D hazardCollider = hazardColliders[i];

            if (hazardCollider != null)
            {
                hazardCollider.enabled = enabled;
            }
        }
    }
}
