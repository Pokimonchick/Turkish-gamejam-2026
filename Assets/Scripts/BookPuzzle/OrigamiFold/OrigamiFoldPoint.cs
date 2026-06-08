using UnityEngine;

public class OrigamiFoldPoint : MonoBehaviour
{
    public string pointId;
    public Renderer visualRenderer;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    private OrigamiFoldPointVisual visualEffect;

    private void Awake()
    {
        visualEffect = GetComponent<OrigamiFoldPointVisual>();
        if (visualEffect != null)
        {
            visualEffect.EnsureVisuals();
            visualRenderer = visualEffect.MainRenderer;
        }

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
        if (visualEffect != null)
        {
            visualEffect.SetHighlighted(highlighted);
        }

        if (visualRenderer == null || visualEffect != null)
        {
            return;
        }

        visualRenderer.material.color = highlighted ? highlightColor : normalColor;
    }
}
