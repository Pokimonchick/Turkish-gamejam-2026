using UnityEngine;

public class FoldAction : MonoBehaviour
{
    public bool isActive;
    public BookSector[] sectorsToFold;
    public PathEdge[] openWhenActive;
    public PathEdge[] closeWhenActive;
    public GameObject[] enableWhenActive;
    public GameObject[] disableWhenActive;

    private void Awake()
    {
        ApplyState();
    }

    public void Toggle()
    {
        isActive = !isActive;
        ApplyState();
    }

    public void ApplyState()
    {
        SetSectorsFolded(isActive);
        SetEdgesOpen(openWhenActive, isActive);
        SetEdgesOpen(closeWhenActive, !isActive);
        SetObjectsActive(enableWhenActive, isActive);
        SetObjectsActive(disableWhenActive, !isActive);
    }

    private void SetSectorsFolded(bool folded)
    {
        if (sectorsToFold == null)
        {
            return;
        }

        for (int i = 0; i < sectorsToFold.Length; i++)
        {
            BookSector sector = sectorsToFold[i];

            if (sector != null)
            {
                sector.SetFolded(folded);
            }
        }
    }

    private void SetEdgesOpen(PathEdge[] edges, bool open)
    {
        if (edges == null)
        {
            return;
        }

        for (int i = 0; i < edges.Length; i++)
        {
            PathEdge edge = edges[i];

            if (edge != null)
            {
                edge.SetOpen(open);
            }
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject item = objects[i];

            if (item != null)
            {
                item.SetActive(active);
            }
        }
    }
}
