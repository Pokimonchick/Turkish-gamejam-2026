using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class FinalCutsceneController : MonoBehaviour
{
    private const int PanelCount = 3;
    private const float PanelInset = 8f;

    private static readonly Vector2[] PanelAnchorMin =
    {
        new Vector2(0.05f, 0.68f),
        new Vector2(0.05f, 0.35f),
        new Vector2(0.05f, 0.02f)
    };

    private static readonly Vector2[] PanelAnchorMax =
    {
        new Vector2(0.95f, 0.98f),
        new Vector2(0.95f, 0.65f),
        new Vector2(0.95f, 0.32f)
    };

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField, Min(0f)] private float startDelaySeconds = 0.25f;
    [SerializeField] private bool playOnStart = true;

    [Header("Comic Panels")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Sprite[] comicPanelSprites = System.Array.Empty<Sprite>();
    [SerializeField, Min(0.01f)] private float panelFadeSeconds = 0.85f;
    [SerializeField, Min(0f)] private float panelIntervalSeconds = 0.35f;
    [SerializeField, Min(0f)] private float postFinalPanelInputLockSeconds = 1f;
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private Color panelFrameColor = new Color(0.95f, 0.88f, 0.72f, 1f);
    [SerializeField] private Color panelMatteColor = Color.black;

    private CanvasGroup[] panelGroups;
    private Coroutine playRoutine;
    private bool dialogueEnded;

    private void Start()
    {
        if (playOnStart)
        {
            playRoutine = StartCoroutine(PlayFinalRoutine());
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
        }
    }

    public void PlayFinalCutscene()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayFinalRoutine());
    }

    private IEnumerator PlayFinalRoutine()
    {
        PrepareCanvas();
        CreateComicPanels();

        if (startDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(startDelaySeconds);
        }

        yield return FadePanelIn(0);
        yield return WaitBetweenPanels();
        yield return FadePanelIn(1);
        yield return WaitBetweenPanels();
        yield return RunDialogue();
        yield return WaitBetweenPanels();
        yield return FadePanelIn(2);

        if (postFinalPanelInputLockSeconds > 0f)
        {
            yield return new WaitForSeconds(postFinalPanelInputLockSeconds);
        }

        yield return WaitForContinueRelease();
        yield return WaitForContinuePress();

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneTransition.Load(nextSceneName);
        }

        playRoutine = null;
    }

    private void PrepareCanvas()
    {
        if (targetCanvas == null)
        {
            GameObject canvasObject = GameObject.Find("Final Canvas");

            if (canvasObject != null)
            {
                targetCanvas = canvasObject.GetComponent<Canvas>();
            }
        }

        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("Final Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = canvasObject.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = -20;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        targetCanvas.transform.localPosition = Vector3.zero;
        targetCanvas.transform.localRotation = Quaternion.identity;
        targetCanvas.transform.localScale = Vector3.one;
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.overrideSorting = true;

        Image background = FindCanvasImage("Final Background");

        if (background == null)
        {
            background = CreateImage(targetCanvas.transform, "Final Background", backgroundColor);
        }

        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.sprite = null;
        background.color = backgroundColor;
        background.preserveAspect = false;
        background.raycastTarget = false;
        background.transform.SetAsFirstSibling();
    }

    private void CreateComicPanels()
    {
        panelGroups = new CanvasGroup[PanelCount];

        for (int i = 0; i < PanelCount; i++)
        {
            Transform existing = targetCanvas.transform.Find($"ComicPanel_{i + 1}");

            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject frameObject = new GameObject($"ComicPanel_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            frameObject.transform.SetParent(targetCanvas.transform, false);

            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            frameRect.anchorMin = PanelAnchorMin[i];
            frameRect.anchorMax = PanelAnchorMax[i];
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;

            Image frameImage = frameObject.GetComponent<Image>();
            frameImage.color = panelFrameColor;
            frameImage.raycastTarget = false;

            CanvasGroup group = frameObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            panelGroups[i] = group;

            Image panelImage = CreateImage(frameObject.transform, "Image", panelMatteColor);
            RectTransform panelRect = panelImage.rectTransform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.one * PanelInset;
            panelRect.offsetMax = Vector2.one * -PanelInset;

            panelImage.sprite = GetPanelSprite(i);
            panelImage.color = panelImage.sprite == null ? panelMatteColor : Color.white;
            panelImage.preserveAspect = true;
            panelImage.raycastTarget = false;
        }
    }

    private IEnumerator RunDialogue()
    {
        if (dialogueData == null)
        {
            yield break;
        }

        while (DialogueManager.Instance == null)
        {
            yield return null;
        }

        dialogueEnded = false;
        DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
        DialogueManager.Instance.DialogueEnded += HandleDialogueEnded;
        DialogueManager.Instance.StartDialogue(dialogueData);

        while (!dialogueEnded)
        {
            yield return null;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DialogueEnded -= HandleDialogueEnded;
        }
    }

    private void HandleDialogueEnded()
    {
        dialogueEnded = true;
    }

    private IEnumerator FadePanelIn(int index)
    {
        if (panelGroups == null || index < 0 || index >= panelGroups.Length || panelGroups[index] == null)
        {
            yield break;
        }

        CanvasGroup group = panelGroups[index];
        float elapsed = 0f;

        while (elapsed < panelFadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / panelFadeSeconds);
            group.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        group.alpha = 1f;
    }

    private IEnumerator WaitBetweenPanels()
    {
        if (panelIntervalSeconds > 0f)
        {
            yield return new WaitForSeconds(panelIntervalSeconds);
        }
    }

    private IEnumerator WaitForContinueRelease()
    {
        while (IsContinueHeld())
        {
            yield return null;
        }
    }

    private IEnumerator WaitForContinuePress()
    {
        while (!IsContinuePressed())
        {
            yield return null;
        }
    }

    private Sprite GetPanelSprite(int index)
    {
        return comicPanelSprites != null
            && index >= 0
            && index < comicPanelSprites.Length
            ? comicPanelSprites[index]
            : null;
    }

    private Image FindCanvasImage(string objectName)
    {
        if (targetCanvas == null)
        {
            return null;
        }

        Transform[] transforms = targetCanvas.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
            {
                return transforms[i].GetComponent<Image>();
            }
        }

        return null;
    }

    private static Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static bool IsContinuePressed()
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

    private static bool IsContinueHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool keyboardHeld = keyboard != null
            && (keyboard.eKey.isPressed
                || keyboard.enterKey.isPressed
                || keyboard.numpadEnterKey.isPressed);
        bool mouseHeld = mouse != null && mouse.leftButton.isPressed;
        return keyboardHeld || mouseHeld;
#else
        return Input.GetKey(KeyCode.E)
            || Input.GetKey(KeyCode.Return)
            || Input.GetMouseButton(0);
#endif
    }
}
