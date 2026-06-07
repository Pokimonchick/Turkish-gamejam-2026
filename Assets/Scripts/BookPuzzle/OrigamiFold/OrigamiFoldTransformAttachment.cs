using UnityEngine;

public class OrigamiFoldTransformAttachment : MonoBehaviour
{
    public Transform target;
    public Vector3 targetLocalPosition;
    public bool followRotation;

    private void Awake()
    {
        SnapToTarget();
    }

    private void OnEnable()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.TransformPoint(targetLocalPosition);

        if (followRotation)
        {
            transform.rotation = target.rotation;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.TransformPoint(targetLocalPosition));
        Gizmos.DrawWireSphere(target.TransformPoint(targetLocalPosition), 0.08f);
    }
}
