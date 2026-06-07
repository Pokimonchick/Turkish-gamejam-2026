using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

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
        ApplyConfiguredFont();

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(false);
        }
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

        if (continueHintObject != null)
        {
            continueHintObject.SetActive(false);
        }

        PlaySfx(dialogCloseSound);
        DialogueEnded?.Invoke();
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

        var line = currentDialogue.lines[currentLineIndex];

        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        if (bodyText != null)
        {
            bodyText.text = line.text;
        }

        if (portraitImage == null)
        {
            return;
        }

        if (line.portrait == null)
        {
            portraitImage.gameObject.SetActive(false);
            portraitImage.sprite = null;
            return;
        }

        portraitImage.sprite = line.portrait;
        portraitImage.gameObject.SetActive(true);
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
