using UnityEngine;

public class FoldMarker : MonoBehaviour, IInteractable
{
    public FoldAction targetAction;

    public void Interact()
    {
        if (targetAction != null)
        {
            targetAction.Toggle();
        }
    }
}
