using UnityEngine;

public class PathEdge : MonoBehaviour
{
    public PathNode a;
    public PathNode b;
    public bool isOpen = true;
    public GameObject visual;

    public PathNode Other(PathNode node)
    {
        if (node == a)
        {
            return b;
        }

        if (node == b)
        {
            return a;
        }

        return null;
    }

    public void SetOpen(bool open)
    {
        isOpen = open;

        if (visual != null)
        {
            visual.SetActive(open);
        }
    }
}
