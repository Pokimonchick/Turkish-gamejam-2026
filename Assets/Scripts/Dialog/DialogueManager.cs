using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject continueHintObject;
    [SerializeField] private Font uiSourceFont;
    [SerializeField] private TMP_FontAsset uiFontAsset;
    public Image dialogueFrameImage;
    public Image topPatternImage;
    public Image namePlateImage;

    [SerializeField] private AudioClip dialogOpenSound;
    [SerializeField] private AudioClip dialogNextSound;
    [SerializeField] private AudioClip dialogCloseSound;

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private int startedFrame = -1;
    private TMP_FontAsset runtimeFontAsset;

    public static DialogueManager Instance { get; private set; }

    public bool IsActive { get; private set; }
    public static bool IsDialogueActive => Instance != null && Instance.IsActive;
    public static int LastEndedFrame { get; private set; } = -1;

    public event Action DialogueStarted;
    public event Action DialogueEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate DialogueManager found. The extra instance will be destroyed.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RepairUiReferences();
        ApplyConfiguredFont();

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(false);
        }

        SetDialogueVisualsActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        if (Time.frameCount == startedFrame)
        {
            return;
        }

        if (IsClosePressed())
        {
            EndDialogue();
            return;
        }

        if (IsAdvancePressed())
        {
            AdvanceDialogue();
        }
    }

    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Count == 0)
        {
            return;
        }

        currentDialogue = data;
        currentLineIndex = 0;
        IsActive = true;
        startedFrame = Time.frameCount;

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(true);
        }

        SetDialogueVisualsActive(true);

        if (continueHintObject != null)
        {
            continueHintObject.SetActive(true);
        }

        ShowCurrentLine();
        PlaySfx(dialogOpenSound);
        DialogueStarted?.Invoke();
    }

    public void AdvanceDialogue()
    {
        if (!IsActive || currentDialogue == null || currentDialogue.lines == null)
        {
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
        PlaySfx(dialogNextSound);
    }

    public void EndDialogue()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        LastEndedFrame = Time.frameCount;
        currentDialogue = null;
        currentLineIndex = 0;

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(false);
        }

        SetDialogueVisualsActive(false);

        if (continueHintObject != null)
        {
            continueHintObject.SetActive(false);
        }

        PlaySfx(dialogCloseSound);
        DialogueEnded?.Invoke();
    }

    private void RepairUiReferences()
    {
        NormalizeDialogueCanvases();

        GameObject modernDialogRoot = FindDeepChild("DialogRoot")?.gameObject
            ?? FindDeepChild("Dialog Root")?.gameObject;

        if (modernDialogRoot != null)
        {
            dialogRoot = modernDialogRoot;
        }

        TextMeshProUGUI modernSpeakerNameText =
            FindDeepComponent<TextMeshProUGUI>("SpeakerNameText")
            ?? FindDeepComponent<TextMeshProUGUI>("Speaker Name Text");

        if (modernSpeakerNameText != null)
        {
            speakerNameText = modernSpeakerNameText;
        }

        TextMeshProUGUI modernBodyText =
            FindDeepComponent<TextMeshProUGUI>("BodyText")
            ?? FindDeepComponent<TextMeshProUGUI>("Body Text");

        if (modernBodyText != null)
        {
            bodyText = modernBodyText;
        }

        Image modernPortraitImage =
            FindDeepComponent<Image>("PortraitImage")
            ?? FindDeepComponent<Image>("Portrait Image");

        if (modernPortraitImage != null)
        {
            portraitImage = modernPortraitImage;
        }

        GameObject modernContinueHint = FindDeepChild("ContinueHint")?.gameObject
            ?? FindDeepChild("Continue Hint Text")?.gameObject;

        if (modernContinueHint != null)
        {
            continueHintObject = modernContinueHint;
        }

        Image modernDialogueFrameImage = FindDeepComponent<Image>("DialogueFrameImage");

        if (modernDialogueFrameImage != null)
        {
            dialogueFrameImage = modernDialogueFrameImage;
        }

        Image modernTopPatternImage = FindDeepComponent<Image>("TopPatternImage");

        if (modernTopPatternImage != null)
        {
            topPatternImage = modernTopPatternImage;
        }

        Image modernNamePlateImage = FindDeepComponent<Image>("NamePlateImage");

        if (modernNamePlateImage != null)
        {
            namePlateImage = modernNamePlateImage;
        }

        DisableLegacyDialogPanel();

#if UNITY_EDITOR
        RepairEditorSpriteFallbacks();
#endif
    }

    private void NormalizeDialogueCanvases()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null)
            {
                continue;
            }

            canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
            canvases[i].worldCamera = null;
            canvases[i].sortingOrder = 100;

            RectTransform rectTransform = canvases[i].GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                continue;
            }

            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }
    }

    private Transform FindDeepChild(string childName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == childName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private T FindDeepComponent<T>(string objectName)
        where T : Component
    {
        Transform child = FindDeepChild(objectName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private void DisableLegacyDialogPanel()
    {
        Transform legacyPanel = FindDeepChild("Dialog Panel");

        if (legacyPanel == null)
        {
            return;
        }

        Image image = legacyPanel.GetComponent<Image>();

        if (image != null)
        {
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
        }

        legacyPanel.gameObject.SetActive(false);
    }

    private void ShowCurrentLine()
    {
        if (currentDialogue == null
            || currentDialogue.lines == null
            || currentLineIndex < 0
            || currentLineIndex >= currentDialogue.lines.Count)
        {
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];
        string speakerName = line.speakerName;
        Sprite portrait = line.portrait;

        if (line.speakerProfile != null)
        {
            speakerName = line.speakerProfile.displayName;
            portrait = line.speakerProfile.portrait;

#if UNITY_EDITOR
            if (portrait == null)
            {
                portrait = ResolveEditorPortraitFallback(line.speakerProfile);
            }
#endif
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
        }

        if (bodyText != null)
        {
            bodyText.text = line.text;
        }

        if (portraitImage == null)
        {
            return;
        }

        if (portrait == null)
        {
            portraitImage.gameObject.SetActive(false);
            portraitImage.sprite = null;
            return;
        }

        portraitImage.sprite = portrait;
        portraitImage.gameObject.SetActive(true);
    }

#if UNITY_EDITOR
    private void RepairEditorSpriteFallbacks()
    {
        AssignEditorSpriteIfMissing(dialogueFrameImage, "9586", null);
        AssignEditorSpriteIfMissing(topPatternImage, "9590", null);
        AssignEditorSpriteIfMissing(namePlateImage, "6356", "6356 4");
    }

    private static void AssignEditorSpriteIfMissing(
        Image image,
        string token,
        string preferredFileNamePart)
    {
        if (image == null || image.sprite != null)
        {
            return;
        }

        Sprite sprite = LoadEditorSpriteByToken(token, preferredFileNamePart);

        if (sprite != null)
        {
            image.sprite = sprite;
        }
    }

    private static Sprite ResolveEditorPortraitFallback(DialogueSpeakerProfile profile)
    {
        if (profile == null)
        {
            return null;
        }

        string speakerId = profile.speakerId ?? string.Empty;

        if (speakerId.Equals("aisulu", StringComparison.OrdinalIgnoreCase))
        {
            return LoadEditorSpriteByToken("9592", null);
        }

        if (speakerId.Equals("umay", StringComparison.OrdinalIgnoreCase))
        {
            return LoadEditorSpriteByToken("9593", null);
        }

        if (speakerId.Equals("elder_village_01", StringComparison.OrdinalIgnoreCase))
        {
            return LoadEditorSpriteByToken("9601", "9601 1");
        }

        return null;
    }

    private static Sprite LoadEditorSpriteByToken(string token, string preferredFileNamePart)
    {
        string[] guids = AssetDatabase.FindAssets(token, new[] { "Assets" });
        List<string> candidates = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
            string extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension != ".png"
                && extension != ".jpg"
                && extension != ".jpeg"
                && extension != ".psd")
            {
                continue;
            }

            string fileName = Path.GetFileName(path);

            if (fileName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                candidates.Add(path);
            }
        }

        candidates.Sort(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(preferredFileNamePart))
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(candidates[i]);

                if (fileName.IndexOf(preferredFileNamePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return LoadEditorSprite(candidates[i]);
                }
            }
        }

        return candidates.Count == 1 ? LoadEditorSprite(candidates[0]) : null;
    }

    private static Sprite LoadEditorSprite(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite != null)
        {
            return sprite;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite nestedSprite)
            {
                return nestedSprite;
            }
        }

        return null;
    }
#endif

    private void SetDialogueVisualsActive(bool active)
    {
        if (topPatternImage != null)
        {
            topPatternImage.gameObject.SetActive(active);
        }

        if (dialogueFrameImage != null)
        {
            dialogueFrameImage.gameObject.SetActive(active);
        }

        if (namePlateImage != null)
        {
            namePlateImage.gameObject.SetActive(active);
        }
    }

    private static void PlaySfx(AudioClip clip)
    {
        if (clip == null || !GameAudioManager.HasInstance)
        {
            return;
        }

        GameAudioManager.Instance.PlaySfx(clip);
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

        if (fontAsset == null)
        {
            return;
        }

        ApplyFont(speakerNameText, fontAsset);
        ApplyFont(bodyText, fontAsset);

        if (continueHintObject == null)
        {
            return;
        }

        TextMeshProUGUI[] continueTexts =
            continueHintObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < continueTexts.Length; i++)
        {
            ApplyFont(continueTexts[i], fontAsset);
        }
    }

    private static void ApplyFont(TextMeshProUGUI text, TMP_FontAsset fontAsset)
    {
        if (text != null && fontAsset != null)
        {
            text.font = fontAsset;
        }
    }

    private static bool IsClosePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static bool IsAdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool keyboardPressed = keyboard != null
            && (keyboard.eKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame);
        bool mousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        return keyboardPressed || mousePressed;
#else
        return Input.GetKeyDown(KeyCode.E)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetMouseButtonDown(0);
#endif
    }
}
