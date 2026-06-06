using UnityEngine;

public class OrigamiFoldWalkableArea : MonoBehaviour
{
    public OrigamiFoldTransformStack ownerStack;
    public bool isWalkable = true;

    private void Awake()
    {
        if (ownerStack == null)
        {
            ownerStack = GetComponentInParent<OrigamiFoldTransformStack>();
        }
    }
}
