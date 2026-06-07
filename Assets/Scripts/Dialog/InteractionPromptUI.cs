using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    public GameObject root;
    public TextMeshProUGUI promptText;
    public string defaultMessage = "E \u2014 \u0432\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0435";
    public Font uiSourceFont;
    public TMP_FontAsset uiFontAsset;

    private Object currentOwner;
    private TMP_FontAsset runtimeFontAsset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate InteractionPromptUI found. Replacing current instance.", this);
        }

        Instance = this;
        ApplyConfiguredFont();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show(string message, Object owner)
    {
        currentOwner = owner;

        if (promptText != null)
        {
            promptText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;
        }

        if (root != null)
        {
            root.SetActive(true);
        }
    }

    public void Hide(Object owner)
    {
        if (owner != null && currentOwner != owner)
        {
            return;
        }

        HideImmediate();
    }

    public void HideImmediate()
    {
        currentOwner = null;

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void ApplyConfiguredFont()
    {
        TMP_FontAsset fontAsset = uiFontAsset;

        if (fontAsset == null && uiSourceFont != null)
        {
            runtimeFontAsset = TMP_FontAsset.CreateFontAsset(
                uiSourceFont,
                128,
                12,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic);

            if (runtimeFontAsset != null)
            {
                fontAsset = runtimeFontAsset;
            }
        }

        if (promptText != null && fontAsset != null)
        {
            promptText.font = fontAsset;
        }
    }
}
