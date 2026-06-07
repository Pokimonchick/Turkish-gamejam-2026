using UnityEngine;

public class OrigamiFoldActionCoordinator : MonoBehaviour
{
    public bool IsBusy { get; private set; }
    public Object currentOwner;

    public bool TryBegin(Object owner)
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        currentOwner = owner;
        return true;
    }

    public void End(Object owner)
    {
        if (owner != currentOwner)
        {
            Debug.LogWarning($"{name}: tried to end with a non-current owner.", this);
            return;
        }

        IsBusy = false;
        currentOwner = null;
    }
}
