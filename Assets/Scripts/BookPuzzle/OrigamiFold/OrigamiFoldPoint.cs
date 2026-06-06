using UnityEngine;

public class OrigamiFoldPoint : MonoBehaviour
{
    public string pointId;
    public Renderer visualRenderer;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    private void Awake()
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponentInChildren<Renderer>();
        }

        if (visualRenderer == null)
        {
            visualRenderer = GetComponent<Renderer>();
        }

        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (visualRenderer == null)
        {
            return;
        }

        visualRenderer.material.color = highlighted ? highlightColor : normalColor;
    }
}
