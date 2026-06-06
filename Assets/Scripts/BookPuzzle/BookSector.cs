using UnityEngine;

public class BookSector : MonoBehaviour
{
    public bool isFolded;
    public GameObject unfoldedVisual;
    public GameObject foldedVisual;

    private void Awake()
    {
        SetFolded(isFolded);
    }

    public void SetFolded(bool folded)
    {
        isFolded = folded;

        if (unfoldedVisual != null)
        {
            unfoldedVisual.SetActive(!folded);
        }

        if (foldedVisual != null)
        {
            foldedVisual.SetActive(folded);
        }
    }

    public void ToggleFold()
    {
        SetFolded(!isFolded);
    }
}
