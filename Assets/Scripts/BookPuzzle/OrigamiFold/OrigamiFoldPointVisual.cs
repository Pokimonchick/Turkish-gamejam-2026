using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class OrigamiFoldPointVisual : MonoBehaviour
{
    [SerializeField] private Sprite nodeSprite;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(1f, 0.92f, 0.25f, 1f);
    [SerializeField] private Color glowColor = new Color(0.12f, 0.85f, 1f, 0.32f);
    [SerializeField] private Color highlightedGlowColor = new Color(1f, 0.72f, 0.18f, 0.62f);

    [Header("Shape")]
    [SerializeField, Min(0.01f)] private float visualSize = 0.55f;
    [SerializeField, Min(0.01f)] private float glowSize = 0.9f;
    [SerializeField] private int sortingOrder = 95;
    [SerializeField] private bool hideLegacyRenderer = true;

    [Header("Glow Pulse")]
    [SerializeField, Min(0f)] private float pulseSpeed = 2.2f;
    [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.18f;

    private const string MainVisualName = "Node Sprite";
    private const string GlowVisualName = "Node Glow";

    private SpriteRenderer nodeRenderer;
    private SpriteRenderer glowRenderer;
    private Renderer legacyRenderer;
    private bool highlighted;

    public Renderer MainRenderer
    {
        get
        {
            EnsureVisuals();
            return nodeRenderer;
        }
    }

    private void Awake()
    {
        EnsureVisuals();
        ApplyState();
    }

    private void OnEnable()
    {
        EnsureVisuals();
        ApplyState();
    }

    private void OnValidate()
    {
        EnsureVisuals();
        ApplyState();
    }

    private void Update()
    {
        if (glowRenderer == null)
        {
            return;
        }

        float pulse = 1f;
        if (pulseSpeed > 0f && pulseAmount > 0f)
        {
            pulse += Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        }

        glowRenderer.transform.localScale = Vector3.one * glowSize * pulse;
    }

    public void EnsureVisuals()
    {
        legacyRenderer = FindLegacyRenderer();
        nodeRenderer = FindOrCreateSpriteRenderer(MainVisualName, sortingOrder + 1);
        glowRenderer = FindOrCreateSpriteRenderer(GlowVisualName, sortingOrder);

        if (legacyRenderer != null)
        {
            legacyRenderer.enabled = !hideLegacyRenderer;
        }

        ConfigureRenderer(nodeRenderer, visualSize, sortingOrder + 1);
        ConfigureRenderer(glowRenderer, glowSize, sortingOrder);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        highlighted = isHighlighted;
        ApplyState();
    }

    private Renderer FindLegacyRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rendererComponent in renderers)
        {
            if (rendererComponent == null || rendererComponent is SpriteRenderer)
            {
                continue;
            }

            return rendererComponent;
        }

        return null;
    }

    private SpriteRenderer FindOrCreateSpriteRenderer(string objectName, int order)
    {
        Transform child = transform.Find(objectName);
        if (child == null)
        {
            GameObject visualObject = new GameObject(objectName);
            child = visualObject.transform;
            child.SetParent(transform, false);
        }

        SpriteRenderer rendererComponent = child.GetComponent<SpriteRenderer>();
        if (rendererComponent == null)
        {
            rendererComponent = child.gameObject.AddComponent<SpriteRenderer>();
        }

        rendererComponent.sortingOrder = order;
        return rendererComponent;
    }

    private void ConfigureRenderer(SpriteRenderer rendererComponent, float size, int order)
    {
        if (rendererComponent == null)
        {
            return;
        }

        rendererComponent.sprite = nodeSprite;
        rendererComponent.sortingOrder = order;
        rendererComponent.transform.localPosition = Vector3.zero;
        rendererComponent.transform.localRotation = Quaternion.identity;
        rendererComponent.transform.localScale = Vector3.one * size;
    }

    private void ApplyState()
    {
        if (nodeRenderer != null)
        {
            nodeRenderer.color = highlighted ? highlightedColor : normalColor;
        }

        if (glowRenderer != null)
        {
            glowRenderer.color = highlighted ? highlightedGlowColor : glowColor;
        }
    }
}
