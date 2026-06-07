using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class OrigamiFoldVillageLevelBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string LoadingScenePath = "Assets/Scenes/Loading.unity";
    private const string VillageScenePath = "Assets/Scenes/Village_Level_01_Greybox.unity";
    private const string StubScenePath = "Assets/Scenes/Village_Level_02_Stub.unity";
    private const string StubSceneName = "Village_Level_02_Stub";
    private const string DialogueSystemPrefabPath = "Assets/Prefabs/Dialog/DialogueSystem.prefab";
    private const string PreferredFontSourcePath = "Assets/artist_nouveau.ttf";
    private const string PreferredFontAssetPath = "Assets/Fonts/artist_nouveau SDF.asset";
    private const string IntroDialoguePath =
        "Assets/ScriptableObjects/Dialogues/Village_NPC_Intro.asset";
    private const string WallHintDialoguePath =
        "Assets/ScriptableObjects/Dialogues/Village_NPC_WallHint.asset";
    private const string PrologueDialoguePath =
        "Assets/ScriptableObjects/Dialogues/Prologue_Fire_Remembers.asset";
    private const string MotherSpeakerProfilePath =
        "Assets/ScriptableObjects/Dialogues/Speakers/Speaker_Mother.asset";
    private const string ElderSpeakerProfilePath =
        "Assets/ScriptableObjects/Dialogues/Speakers/Speaker_Elder_Village01.asset";
    private const string InteractionPromptMessage =
        "E \u2014 \u0432\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0435";
    private const string DialogueFontCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        "\u0410\u0411\u0412\u0413\u0414\u0415\u0401\u0416\u0417\u0418\u0419\u041a\u041b\u041c\u041d\u041e\u041f\u0420\u0421\u0422\u0423\u0424\u0425\u0426\u0427\u0428\u0429\u042a\u042b\u042c\u042d\u042e\u042f" +
        "\u0430\u0431\u0432\u0433\u0434\u0435\u0451\u0436\u0437\u0438\u0439\u043a\u043b\u043c\u043d\u043e\u043f\u0440\u0441\u0442\u0443\u0444\u0445\u0446\u0447\u0448\u0449\u044a\u044b\u044c\u044d\u044e\u044f" +
        " .,!?;:-/()[]\"'\u00ab\u00bb\u2014";
    private const int MapWidth = 12;
    private const int VisibleVillageWidth = 12;
    private const int MapHeight = 9;
    private const int WallColumnX = 10;
    private const int ExitBufferX = 11;
    private const float CellSize = 1f;

    private enum VillageCellKind
    {
        Walkable,
        Wall,
        ExitBuffer,
        House,
        Door,
        Fire,
        Blocked
    }

    private class CellData
    {
        public GameObject gameObject;
        public OrigamiFoldTransformStack stack;
        public VillageCellKind kind;
        public bool isWalkable;
    }

    [MenuItem("Tools/PANINI/Origami Fold/Create Village Level 01 Greybox")]
    public static void CreateVillageLevel01Greybox()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot rebuild Village Level 01 while Unity is in Play Mode.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        DialogueData introDialogue = EnsureDialogueData(
            IntroDialoguePath,
            "Village_NPC_Mother",
            MotherSpeakerProfilePath,
            "\u041c\u0430\u043c\u0430",
            "\u0411\u0443\u0434\u044c \u043e\u0441\u0442\u043e\u0440\u043e\u0436\u043d\u0430, \u0410\u0439\u0441\u0443\u043b\u0443. \u0417\u0430 \u0441\u0442\u0435\u043d\u043e\u0439 \u043d\u0430\u0447\u0438\u043d\u0430\u0435\u0442\u0441\u044f \u043f\u0443\u0442\u044c \u0441\u043a\u0430\u0437\u0430\u043d\u0438\u044f.");
        DialogueData wallHintDialogue = EnsureDialogueData(
            WallHintDialoguePath,
            "Village_NPC_Elder_Wall",
            ElderSpeakerProfilePath,
            "\u0421\u0442\u0430\u0440\u0435\u0439\u0448\u0438\u043d\u0430",
            "\u041e\u0433\u043e\u043d\u044c \u0438\u0441\u0447\u0435\u0437. \u0410\u0439\u0441\u0443\u043b\u0443, \u043d\u0430\u0439\u0434\u0438 \u043f\u0443\u0442\u044c \u0437\u0430 \u0441\u0442\u0435\u043d\u043e\u0439.");

        CreateVillageScene(introDialogue, wallHintDialogue);
        CreateStubScene();
        AddScenesToBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created village greybox scenes: {VillageScenePath}, {StubScenePath}");
    }

    private static void CreateVillageScene(
        DialogueData introDialogue,
        DialogueData wallHintDialogue)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Village_Level_01_Greybox";

        Camera camera = CreateCamera("Main Camera", new Vector3(0f, 0f, -10f), 5f);

        GameObject levelRoot = CreateEmpty("LEVEL_ROOT", null);
        GameObject foldSystemRoot = CreateEmpty("ORIGAMI_FOLD_SYSTEM", levelRoot.transform);
        GameObject mapRoot = CreateEmpty("VILLAGE_MAP", levelRoot.transform);
        GameObject cellsRoot = CreateEmpty("Cells", mapRoot.transform);
        GameObject pointsRoot = CreateEmpty("ORIGAMI_FOLD_POINTS", levelRoot.transform);
        GameObject linksRoot = CreateEmpty("ORIGAMI_FOLD_LINKS", levelRoot.transform);
        GameObject actionsRoot = CreateEmpty("ORIGAMI_ACTIONS", levelRoot.transform);
        GameObject playerRoot = CreateEmpty("VILLAGE_PLAYER", levelRoot.transform);
        GameObject npcsRoot = CreateEmpty("VILLAGE_NPCS", levelRoot.transform);
        GameObject exitRoot = CreateEmpty("VILLAGE_EXIT", levelRoot.transform);
        GameObject debugRoot = CreateEmpty("VILLAGE_DEBUG", levelRoot.transform);

        DialogueManager dialogueManager = CreateDialogueSystem(levelRoot.transform);
        TMP_FontAsset uiFont = ResolveProjectTmpFontAsset();
        Font uiSourceFont = ResolveProjectSourceFont();
        ApplyFontToDialogueManager(dialogueManager, uiFont, uiSourceFont);
        Canvas dialogueCanvas = FindOrCreateDialogueCanvas(dialogueManager, levelRoot.transform);
        CreateInteractionPromptUI(dialogueCanvas, uiFont, uiSourceFont);
        CreateEventSystemIfMissing();
        CreatePrologueAutoStart(levelRoot.transform);

        GameObject coordinatorObject = CreateEmpty("OrigamiFoldActionCoordinator", foldSystemRoot.transform);
        OrigamiFoldActionCoordinator coordinator =
            coordinatorObject.AddComponent<OrigamiFoldActionCoordinator>();

        CellData[,] cells = CreateCells(cellsRoot.transform);
        Debug.Log($"Village map grid created: MapCells={MapWidth * MapHeight}, expected=108.");
        int walkableLayer = ResolveWalkableLayer();
        LayerMask walkableMask = new LayerMask
        {
            value = 1 << walkableLayer
        };
        CreateWalkableAreas(cells, walkableLayer);

        OrigamiFoldTransformStack[] exitStacks = CreateVillageExit(exitRoot.transform);

        OrigamiFoldPoint leftPoint;
        OrigamiFoldPoint rightPoint;
        OrigamiFoldPoint mergedPoint;
        CreateWallFoldPoints(
            pointsRoot.transform,
            camera,
            out leftPoint,
            out rightPoint,
            out mergedPoint);

        OrigamiFoldStripSqueezeAction wallAction = CreateWallColumnAction(
            actionsRoot.transform,
            cells,
            exitStacks,
            coordinator,
            leftPoint,
            rightPoint,
            mergedPoint);

        ConfigureMergedWallPoint(mergedPoint, wallAction, camera);

        OrigamiFoldLink leftToRight = CreateWallLink(
            "Wall_Link_LeftToRight",
            linksRoot.transform,
            leftPoint,
            rightPoint,
            wallAction);
        OrigamiFoldLink rightToLeft = CreateWallLink(
            "Wall_Link_RightToLeft",
            linksRoot.transform,
            rightPoint,
            leftPoint,
            wallAction);

        GameObject controllerObject = CreateEmpty("OrigamiFoldDragController", foldSystemRoot.transform);
        OrigamiFoldDragController dragController =
            controllerObject.AddComponent<OrigamiFoldDragController>();
        dragController.targetCamera = camera;
        dragController.links = new[] { leftToRight, rightToLeft };
        dragController.autoFindLinks = true;
        dragController.snapDistance = 0.5f;
        dragController.lineWidth = 0.05f;

        GameObject player = CreatePlayer(playerRoot.transform, cells[1, 0], walkableMask);
        GameObject introNpc = CreateNpcPlaceholder("NPC_3_5", npcsRoot.transform, CellToWorld(3, 5));
        GameObject wallHintNpc = CreateNpcPlaceholder("NPC_8_2", npcsRoot.transform, CellToWorld(8, 2));
        int npcCount = 0;
        npcCount += ConfigureNpcInteractable(
            introNpc,
            introDialogue,
            player.transform,
            "VillageIntroNpc") ? 1 : 0;
        npcCount += ConfigureNpcInteractable(
            wallHintNpc,
            wallHintDialogue,
            player.transform,
            "VillageWallHintNpc") ? 1 : 0;

        CreateInstructionText(debugRoot.transform);

        Selection.activeGameObject = levelRoot;
        EditorSceneManager.SaveScene(scene, VillageScenePath);
        Debug.Log(
            $"Village dialogue setup: NPCInteractable created={npcCount}, DialogueManager found={dialogueManager != null}, Player found={player != null}.");
    }

    private static void CreatePrologueAutoStart(Transform parent)
    {
        DialogueData prologue = AssetDatabase.LoadAssetAtPath<DialogueData>(PrologueDialoguePath);

        if (prologue == null)
        {
            Debug.LogWarning(
                $"Village prologue dialogue was not found: {PrologueDialoguePath}. Run Tools/PANINI/Dialog/Rebuild Dialogue Art And Prologue before rebuilding the village scene.");
            return;
        }

        GameObject autoStartObject = CreateEmpty("PrologueAutoStart", parent);
        DialogueAutoStart autoStart = autoStartObject.AddComponent<DialogueAutoStart>();
        autoStart.dialogueData = prologue;
        autoStart.delaySeconds = 0.3f;
        autoStart.playOnStart = true;
        autoStart.onlyIfNoDialogueActive = true;
    }

    private static void CreateStubScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Village_Level_02_Stub";

        Camera camera = CreateCamera("Main Camera", new Vector3(0f, 0f, -10f), 3.5f);
        camera.backgroundColor = new Color(0.04f, 0.045f, 0.06f);

        GameObject textObject = new GameObject("StubText");
        textObject.transform.position = Vector3.zero;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = "Village Level 02 Stub";
        text.characterSize = 0.22f;
        text.fontSize = 40;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.86f, 0.92f, 1f);

        Renderer renderer = textObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 20;
        }

        CreateSpriteVisual(
            "PlayerPlaceholder",
            null,
            new Vector3(0f, -0.75f, 0f),
            new Vector2(0.36f, 0.36f),
            new Color(1f, 0.68f, 0.18f),
            10,
            false);

        EditorSceneManager.SaveScene(scene, StubScenePath);
    }

    private static DialogueData EnsureDialogueData(
        string path,
        string dialogueId,
        string speakerProfilePath,
        string speakerName,
        string text)
    {
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/Dialogues");

        DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.dialogueId = dialogueId;
        DialogueSpeakerProfile speakerProfile =
            AssetDatabase.LoadAssetAtPath<DialogueSpeakerProfile>(speakerProfilePath);
        data.lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerProfile = speakerProfile,
                speakerName = speakerProfile != null ? speakerProfile.displayName : speakerName,
                text = text,
                portrait = speakerProfile != null ? speakerProfile.portrait : null
            }
        };

        EditorUtility.SetDirty(data);
        return data;
    }

    private static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');

        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path);
        string folderName = Path.GetFileName(path);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
        {
            return;
        }

        parent = parent.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static DialogueManager CreateDialogueSystem(Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueSystemPrefabPath);

        if (prefab != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (instance != null)
            {
                instance.name = "DialogueSystem";
                instance.transform.SetParent(parent, false);
                NormalizeDialogueCanvases(instance.transform);

                DialogueManager prefabManager =
                    instance.GetComponentInChildren<DialogueManager>(true);
                SetDialogueRootActive(prefabManager, false);
                return prefabManager;
            }
        }

        Debug.LogWarning("DialogueSystem prefab was not found. Creating fallback DialogueManager UI.");
        return CreateFallbackDialogueSystem(parent);
    }

    private static DialogueManager CreateFallbackDialogueSystem(Transform parent)
    {
        GameObject root = CreateEmpty("DialogueSystem", parent);
        DialogueManager manager = root.AddComponent<DialogueManager>();

        GameObject canvasObject = CreateEmpty("Canvas", root.transform);
        int uiLayer = LayerMask.NameToLayer("UI");

        if (uiLayer >= 0)
        {
            canvasObject.layer = uiLayer;
        }

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject dialogRoot = CreateUiObject("Dialog Root", canvasObject.transform);
        Stretch(dialogRoot.GetComponent<RectTransform>());

        GameObject panel = CreateUiObject("Dialog Panel", dialogRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.04f);
        panelRect.anchorMax = new Vector2(0.95f, 0.34f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.035f, 0.045f, 0.88f);

        GameObject portraitObject = CreateUiObject("Portrait Image", panel.transform);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0f);
        portraitRect.anchorMax = new Vector2(0f, 1f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(32f, 0f);
        portraitRect.sizeDelta = new Vector2(220f, -48f);
        Image portraitImage = portraitObject.AddComponent<Image>();
        portraitImage.color = new Color(1f, 1f, 1f, 0.18f);
        portraitImage.preserveAspect = true;

        TextMeshProUGUI speakerNameText = CreateTmpText(
            "Speaker Name Text",
            panel.transform,
            new Vector2(284f, -84f),
            new Vector2(-36f, -24f),
            38f,
            FontStyles.Bold,
            new Color(1f, 0.78f, 0.35f));

        TextMeshProUGUI bodyText = CreateTmpText(
            "Body Text",
            panel.transform,
            new Vector2(284f, 78f),
            new Vector2(-36f, -104f),
            34f,
            FontStyles.Normal,
            new Color(0.94f, 0.92f, 0.86f));

        TextMeshProUGUI continueHintText = CreateTmpText(
            "Continue Hint Text",
            panel.transform,
            Vector2.zero,
            Vector2.zero,
            26f,
            FontStyles.Normal,
            new Color(1f, 0.78f, 0.35f));
        continueHintText.text = "E / Enter / Click\nPress Space to Skip";
        continueHintText.alignment = TextAlignmentOptions.Right;
        RectTransform hintRect = continueHintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-36f, 26f);
        hintRect.sizeDelta = new Vector2(420f, 68f);

        dialogRoot.SetActive(false);
        AssignDialogueManager(
            manager,
            dialogRoot,
            speakerNameText,
            bodyText,
            portraitImage,
            continueHintText.gameObject);

        return manager;
    }

    private static TextMeshProUGUI CreateTmpText(
        string name,
        Transform parent,
        Vector2 offsetMin,
        Vector2 offsetMax,
        float fontSize,
        FontStyles fontStyle,
        Color color)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Empty;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void AssignDialogueManager(
        DialogueManager manager,
        GameObject dialogRoot,
        TextMeshProUGUI speakerNameText,
        TextMeshProUGUI bodyText,
        Image portraitImage,
        GameObject continueHintObject)
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        SetSerializedObjectReference(serializedManager, "dialogRoot", dialogRoot);
        SetSerializedObjectReference(serializedManager, "speakerNameText", speakerNameText);
        SetSerializedObjectReference(serializedManager, "bodyText", bodyText);
        SetSerializedObjectReference(serializedManager, "portraitImage", portraitImage);
        SetSerializedObjectReference(serializedManager, "continueHintObject", continueHintObject);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetDialogueRootActive(DialogueManager manager, bool active)
    {
        if (manager == null)
        {
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty dialogRootProperty = serializedManager.FindProperty("dialogRoot");
        GameObject dialogRoot = dialogRootProperty != null
            ? dialogRootProperty.objectReferenceValue as GameObject
            : null;

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(active);
        }
    }

    private static void CreateEventSystemIfMissing()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Canvas FindOrCreateDialogueCanvas(DialogueManager dialogueManager, Transform parent)
    {
        Canvas canvas = dialogueManager != null
            ? dialogueManager.GetComponentInChildren<Canvas>(true)
            : null;

        if (canvas != null)
        {
            return canvas;
        }

        canvas = parent != null ? parent.GetComponentInChildren<Canvas>(true) : null;

        if (canvas != null)
        {
            NormalizeDialogueCanvas(canvas);
            return canvas;
        }

        GameObject canvasObject = CreateEmpty("Canvas", parent);
        int uiLayer = LayerMask.NameToLayer("UI");

        if (uiLayer >= 0)
        {
            canvasObject.layer = uiLayer;
        }

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        NormalizeDialogueCanvas(canvas);
        return canvas;
    }

    private static void NormalizeDialogueCanvases(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);

        for (int i = 0; i < canvases.Length; i++)
        {
            NormalizeDialogueCanvas(canvases[i]);
        }
    }

    private static void NormalizeDialogueCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = 100;

        RectTransform rectTransform = canvas.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            return;
        }

        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
    }

    private static void CreateInteractionPromptUI(
        Canvas canvas,
        TMP_FontAsset fontAsset,
        Font sourceFont)
    {
        if (canvas == null)
        {
            Debug.LogWarning("Could not create InteractionPromptUI: Canvas is missing.");
            return;
        }

        GameObject controller = CreateEmpty("InteractionPromptUI", canvas.transform);
        GameObject promptRoot = CreateUiObject("InteractionPrompt", canvas.transform);
        RectTransform rootRect = promptRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -70f);
        rootRect.sizeDelta = new Vector2(420f, 54f);

        Image background = promptRoot.AddComponent<Image>();
        background.color = new Color(0.02f, 0.025f, 0.032f, 0.72f);

        GameObject textObject = CreateUiObject("Prompt Text", promptRoot.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 6f);
        textRect.offsetMax = new Vector2(-18f, -6f);

        TextMeshProUGUI promptText = textObject.AddComponent<TextMeshProUGUI>();
        promptText.text = InteractionPromptMessage;
        promptText.fontSize = 30f;
        promptText.color = new Color(0.96f, 0.94f, 0.88f, 1f);
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.raycastTarget = false;
        promptText.textWrappingMode = TextWrappingModes.Normal;

        if (fontAsset != null)
        {
            promptText.font = fontAsset;
        }

        InteractionPromptUI promptUI = controller.AddComponent<InteractionPromptUI>();
        promptUI.root = promptRoot;
        promptUI.promptText = promptText;
        promptUI.defaultMessage = InteractionPromptMessage;
        promptUI.uiSourceFont = sourceFont;
        promptUI.uiFontAsset = fontAsset;

        promptRoot.SetActive(false);
    }

    private static TMP_FontAsset ResolveProjectTmpFontAsset()
    {
        TMP_FontAsset preferredFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);

        if (IsUsableFontAsset(preferredFont))
        {
            Debug.Log($"Using TMP font asset for village dialogue UI: {PreferredFontAssetPath}");
            return preferredFont;
        }

        TMP_FontAsset rebuiltPreferredFont = RebuildPreferredFontAssetIfPossible(preferredFont);

        if (IsUsableFontAsset(rebuiltPreferredFont))
        {
            Debug.Log($"Using rebuilt TMP font asset for village dialogue UI: {PreferredFontAssetPath}");
            return rebuiltPreferredFont;
        }

        List<string> customTmpFonts = FindCustomAssetPaths("t:TMP_FontAsset");
        customTmpFonts.Remove(PreferredFontAssetPath);

        if (customTmpFonts.Count == 1)
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(customTmpFonts[0]);

            if (IsUsableFontAsset(fontAsset))
            {
                Debug.Log($"Using TMP font asset for village dialogue UI: {customTmpFonts[0]}");
                return fontAsset;
            }
        }

        if (customTmpFonts.Count > 1)
        {
            Debug.LogWarning(
                $"Multiple custom TMP font candidates found. Font assignment skipped: {string.Join(", ", customTmpFonts)}");
            return null;
        }

        List<string> customFontFiles = FindCustomAssetPaths("t:Font");
        customFontFiles.Remove(PreferredFontSourcePath);

        if (customFontFiles.Count != 1)
        {
            if (customFontFiles.Count > 1)
            {
                Debug.LogWarning(
                    $"Multiple custom font files found. TMP font creation skipped: {string.Join(", ", customFontFiles)}");
            }

            return null;
        }

        Font font = AssetDatabase.LoadAssetAtPath<Font>(customFontFiles[0]);

        if (font == null)
        {
            return null;
        }

        string folder = Path.GetDirectoryName(customFontFiles[0]).Replace('\\', '/');
        string fontName = Path.GetFileNameWithoutExtension(customFontFiles[0]);
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fontName} SDF.asset");
        TMP_FontAsset createdFontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            128,
            12,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic);

        if (createdFontAsset == null)
        {
            return null;
        }

        AssetDatabase.CreateAsset(createdFontAsset, assetPath);
        AddFontSubAssets(createdFontAsset);
        EditorUtility.SetDirty(createdFontAsset);
        Debug.Log($"Created TMP font asset for village dialogue UI: {assetPath}");
        return createdFontAsset;
    }

    private static Font ResolveProjectSourceFont()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PreferredFontSourcePath);

        if (sourceFont == null)
        {
            Debug.LogWarning($"Village UI source font was not found: {PreferredFontSourcePath}");
        }

        return sourceFont;
    }

    private static TMP_FontAsset RebuildPreferredFontAssetIfPossible(TMP_FontAsset existing)
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PreferredFontSourcePath);

        if (sourceFont == null)
        {
            Debug.LogWarning(
                $"Preferred village UI font source was not found: {PreferredFontSourcePath}");
            return existing;
        }

        EnsureFolder("Assets/Fonts");

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(PreferredFontAssetPath);
            AssetDatabase.Refresh();
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            128,
            12,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.name = "artist_nouveau SDF";
        fontAsset.TryAddCharacters(DialogueFontCharacters, out _, false);

        AssetDatabase.CreateAsset(fontAsset, PreferredFontAssetPath);
        AddFontSubAssets(fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(PreferredFontAssetPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);
    }

    private static void AddFontSubAssets(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return;
        }

        if (fontAsset.material != null)
        {
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        if (fontAsset.atlasTextures == null)
        {
            return;
        }

        for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
        {
            Texture2D texture = fontAsset.atlasTextures[i];

            if (texture != null)
            {
                AssetDatabase.AddObjectToAsset(texture, fontAsset);
            }
        }
    }

    private static bool IsUsableFontAsset(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return false;
        }

        try
        {
            return fontAsset.material != null
                && fontAsset.atlasTextures != null
                && fontAsset.atlasTextures.Length > 0
                && fontAsset.atlasTextures[0] != null;
        }
        catch (UnassignedReferenceException)
        {
            return false;
        }
    }

    private static List<string> FindCustomAssetPaths(string filter)
    {
        string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });
        List<string> paths = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');

            if (path.StartsWith("Assets/TextMesh Pro/"))
            {
                continue;
            }

            paths.Add(path);
        }

        return paths;
    }

    private static void ApplyFontToDialogueManager(
        DialogueManager dialogueManager,
        TMP_FontAsset fontAsset,
        Font sourceFont)
    {
        if (dialogueManager == null)
        {
            return;
        }

        SerializedObject serializedManager = new SerializedObject(dialogueManager);
        SetSerializedObjectReference(serializedManager, "uiSourceFont", sourceFont);
        SetSerializedObjectReference(serializedManager, "uiFontAsset", fontAsset);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        if (fontAsset == null)
        {
            return;
        }

        ApplyFontToTextProperty(serializedManager, "speakerNameText", fontAsset);
        ApplyFontToTextProperty(serializedManager, "bodyText", fontAsset);

        SerializedProperty continueHintProperty =
            serializedManager.FindProperty("continueHintObject");
        GameObject continueHintObject = continueHintProperty != null
            ? continueHintProperty.objectReferenceValue as GameObject
            : null;

        if (continueHintObject != null)
        {
            ApplyFontToTmpChildren(continueHintObject, fontAsset);
        }
    }

    private static void ApplyFontToTextProperty(
        SerializedObject serializedObject,
        string propertyName,
        TMP_FontAsset fontAsset)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        TextMeshProUGUI text = property != null
            ? property.objectReferenceValue as TextMeshProUGUI
            : null;

        if (text != null)
        {
            text.font = fontAsset;
            EditorUtility.SetDirty(text);
        }
    }

    private static void ApplyFontToTmpChildren(GameObject root, TMP_FontAsset fontAsset)
    {
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].font = fontAsset;
            EditorUtility.SetDirty(texts[i]);
        }
    }

    private static Camera CreateCamera(string name, Vector3 position, float orthographicSize)
    {
        GameObject cameraObject = new GameObject(name);
        TrySetTag(cameraObject, "MainCamera");
        cameraObject.transform.position = position;

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);

        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static CellData[,] CreateCells(Transform parent)
    {
        CellData[,] cells = new CellData[MapWidth, MapHeight];

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                VillageCellKind kind = GetCellKind(x, y);
                bool walkable = IsWalkableCell(x, y, kind);

                GameObject cell = CreateEmpty($"MapCell_{x}_{y}", parent);
                cell.transform.position = CellToWorld(x, y);

                OrigamiFoldTransformStack stack = cell.AddComponent<OrigamiFoldTransformStack>();
                stack.CaptureBaseTransform();

                CreateSpriteVisual(
                    "Visual",
                    cell.transform,
                    Vector3.zero,
                    new Vector2(0.96f, 0.96f),
                    GetCellColor(kind, walkable),
                    0,
                    true);

                cells[x, y] = new CellData
                {
                    gameObject = cell,
                    stack = stack,
                    kind = kind,
                    isWalkable = walkable
                };
            }
        }

        return cells;
    }

    private static void CreateWalkableAreas(CellData[,] cells, int walkableLayer)
    {
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                CellData cell = cells[x, y];

                if (!cell.isWalkable)
                {
                    continue;
                }

                GameObject areaObject = CreateEmpty("WalkableArea", cell.gameObject.transform);
                areaObject.layer = walkableLayer;

                BoxCollider2D collider = areaObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(1f, 1f);

                OrigamiFoldWalkableArea area = areaObject.AddComponent<OrigamiFoldWalkableArea>();
                area.ownerStack = cell.stack;
                area.isWalkable = true;
            }
        }
    }

    private static GameObject CreateNpcPlaceholder(string name, Transform parent, Vector3 position)
    {
        GameObject npc = CreateEmpty(name, parent);
        npc.transform.position = position;

        CreateSpriteVisual(
            "Visual",
            npc.transform,
            Vector3.zero,
            new Vector2(0.45f, 0.45f),
            new Color(1f, 0.92f, 0.15f),
            30,
            false);

        return npc;
    }

    private static bool ConfigureNpcInteractable(
        GameObject npc,
        DialogueData dialogueData,
        Transform player,
        string debugName)
    {
        if (npc == null)
        {
            Debug.LogWarning($"Could not configure NPC dialogue for {debugName}: NPC is missing.");
            return false;
        }

        if (dialogueData == null)
        {
            Debug.LogWarning($"Could not configure NPC dialogue for {debugName}: dialogue data is missing.");
            return false;
        }

        if (player == null)
        {
            Debug.LogWarning($"Could not configure NPC dialogue for {debugName}: player is missing.");
            return false;
        }

        NPCInteractable interactable = npc.GetComponent<NPCInteractable>();

        if (interactable == null)
        {
            interactable = npc.AddComponent<NPCInteractable>();
        }

        GameObject hint = CreateNpcInteractionHint(npc.transform);

        SerializedObject serializedInteractable = new SerializedObject(interactable);
        SetSerializedObjectReference(serializedInteractable, "dialogueData", dialogueData);
        SetSerializedObjectReference(serializedInteractable, "player", player);
        SetSerializedFloat(serializedInteractable, "interactionDistance", 1.5f);
        SetSerializedEnum(serializedInteractable, "interactKey", KeyCode.E);
        SetSerializedObjectReference(serializedInteractable, "interactionHint", hint);
        SetSerializedBool(serializedInteractable, "useGlobalInteractionPrompt", true);
        SetSerializedString(
            serializedInteractable,
            "interactionPromptText",
            InteractionPromptMessage);
        serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

        hint.SetActive(false);
        EditorUtility.SetDirty(interactable);
        return true;
    }

    private static GameObject CreateNpcInteractionHint(Transform parent)
    {
        GameObject hint = CreateEmpty("InteractionHint", parent);
        hint.transform.localPosition = new Vector3(0f, 0.65f, 0f);

        CreateSpriteVisual(
            "Backplate",
            hint.transform,
            Vector3.zero,
            new Vector2(0.3f, 0.3f),
            new Color(0f, 0f, 0f, 0.72f),
            65,
            true);

        GameObject labelObject = CreateEmpty("Label", hint.transform);
        labelObject.transform.localPosition = new Vector3(0f, -0.06f, 0f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "E";
        label.characterSize = 0.16f;
        label.fontSize = 34;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;

        Renderer renderer = labelObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 70;
        }

        return hint;
    }

    private static void SetSerializedObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetSerializedFloat(
        SerializedObject serializedObject,
        string propertyName,
        float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetSerializedEnum(
        SerializedObject serializedObject,
        string propertyName,
        KeyCode value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.intValue = (int)value;
        }
    }

    private static void SetSerializedBool(
        SerializedObject serializedObject,
        string propertyName,
        bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetSerializedString(
        SerializedObject serializedObject,
        string propertyName,
        string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static OrigamiFoldTransformStack[] CreateVillageExit(Transform parent)
    {
        GameObject lineRoot = CreateEmpty("NextLevelExitLine", parent);
        List<OrigamiFoldTransformStack> stacks = new List<OrigamiFoldTransformStack>();

        for (int y = 0; y < MapHeight; y++)
        {
            GameObject exit = CreateEmpty($"NextLevelExit_{ExitBufferX}_{y}", lineRoot.transform);
            exit.transform.position = CellToWorld(ExitBufferX, y);

            OrigamiFoldTransformStack stack = exit.AddComponent<OrigamiFoldTransformStack>();
            stack.CaptureBaseTransform();
            stacks.Add(stack);

            GameObject visual = CreateSpriteVisual(
                "Visual",
                exit.transform,
                Vector3.zero,
                new Vector2(0.82f, 0.82f),
                new Color(0.1f, 1f, 0.25f, 0.72f),
                35,
                false);

            BoxCollider2D collider = exit.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.92f, 0.92f);

            OrigamiFoldSceneExit sceneExit = exit.AddComponent<OrigamiFoldSceneExit>();
            sceneExit.nextSceneName = StubSceneName;
            sceneExit.loadSceneOnEnter = true;
            sceneExit.visualRoot = visual;
        }

        return stacks.ToArray();
    }

    private static void CreateWallFoldPoints(
        Transform parent,
        Camera camera,
        out OrigamiFoldPoint leftPoint,
        out OrigamiFoldPoint rightPoint,
        out OrigamiFoldPoint mergedPoint)
    {
        float wallCenterX = CellToWorld(WallColumnX, 0).x;
        float wallY = CellToWorld(0, 2).y + 0.5f;
        float leftX = wallCenterX - 0.5f;
        float rightX = wallCenterX + 0.5f;

        leftPoint = CreateFoldPoint(
            "Wall_Point_Left",
            parent,
            new Vector3(leftX, wallY, 0f),
            Color.black,
            0.34f,
            0.24f,
            50);
        rightPoint = CreateFoldPoint(
            "Wall_Point_Right",
            parent,
            new Vector3(rightX, wallY, 0f),
            Color.black,
            0.34f,
            0.24f,
            50);
        mergedPoint = CreateFoldPoint(
            "Wall_Point_Merged",
            parent,
            new Vector3(wallCenterX, wallY, 0f),
            Color.cyan,
            0.42f,
            0.28f,
            55);

        OrigamiFoldClickAction clickAction =
            mergedPoint.gameObject.AddComponent<OrigamiFoldClickAction>();
        clickAction.targetCamera = camera;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = "Wall_Point_Merged";

        mergedPoint.gameObject.SetActive(false);
    }

    private static OrigamiFoldStripSqueezeAction CreateWallColumnAction(
        Transform parent,
        CellData[,] cells,
        OrigamiFoldTransformStack[] exitStacks,
        OrigamiFoldActionCoordinator coordinator,
        OrigamiFoldPoint leftPoint,
        OrigamiFoldPoint rightPoint,
        OrigamiFoldPoint mergedPoint)
    {
        GameObject actionObject = CreateEmpty("WallColumn_StripSqueezeAction", parent);
        OrigamiFoldStripSqueezeAction action =
            actionObject.AddComponent<OrigamiFoldStripSqueezeAction>();
        action.animationDuration = 0.3f;
        action.coordinator = coordinator;
        action.useCoordinator = true;
        action.targets = CreateWallTargets(cells, exitStacks);
        action.enableAfterActive = new[] { mergedPoint.gameObject };
        action.disableAfterActive = new[] { leftPoint.gameObject, rightPoint.gameObject };
        action.enableAfterInactive = action.disableAfterActive;
        action.disableAfterInactive = action.enableAfterActive;
        return action;
    }

    private static OrigamiStripContributionTarget[] CreateWallTargets(
        CellData[,] cells,
        OrigamiFoldTransformStack[] exitStacks)
    {
        List<OrigamiStripContributionTarget> targets =
            new List<OrigamiStripContributionTarget>();

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                targets.Add(new OrigamiStripContributionTarget
                {
                    stack = cells[x, y].stack,
                    activeLocalPositionOffset = GetWallFoldOffset(x),
                    activeLocalScaleMultiplier = x == WallColumnX
                        ? new Vector3(0.02f, 1f, 1f)
                        : Vector3.one
                });
            }
        }

        if (exitStacks == null)
        {
            return targets.ToArray();
        }

        foreach (OrigamiFoldTransformStack exitStack in exitStacks)
        {
            if (exitStack == null)
            {
                continue;
            }

            targets.Add(new OrigamiStripContributionTarget
            {
                stack = exitStack,
                activeLocalPositionOffset = GetWallFoldOffset(ExitBufferX),
                activeLocalScaleMultiplier = Vector3.one
            });
        }

        return targets.ToArray();
    }

    private static Vector3 GetWallFoldOffset(int x)
    {
        if (x < WallColumnX)
        {
            return new Vector3(0.5f, 0f, 0f);
        }

        if (x > WallColumnX)
        {
            return new Vector3(-0.5f, 0f, 0f);
        }

        return Vector3.zero;
    }

    private static void ConfigureMergedWallPoint(
        OrigamiFoldPoint mergedPoint,
        OrigamiFoldStripSqueezeAction wallAction,
        Camera camera)
    {
        OrigamiFoldClickAction clickAction =
            mergedPoint.GetComponent<OrigamiFoldClickAction>();

        if (clickAction == null)
        {
            clickAction = mergedPoint.gameObject.AddComponent<OrigamiFoldClickAction>();
        }

        clickAction.targetCamera = camera;
        clickAction.targetStripSqueezeAction = wallAction;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
    }

    private static OrigamiFoldLink CreateWallLink(
        string name,
        Transform parent,
        OrigamiFoldPoint pointA,
        OrigamiFoldPoint pointB,
        OrigamiFoldStripSqueezeAction wallAction)
    {
        GameObject linkObject = CreateEmpty(name, parent);
        OrigamiFoldLink link = linkObject.AddComponent<OrigamiFoldLink>();
        link.pointA = pointA;
        link.pointB = pointB;
        link.bidirectional = false;
        link.targetStripSqueezeAction = wallAction;
        link.activeStateOnExecute = true;
        link.enableOnExecute = new GameObject[0];
        link.disableOnExecute = new GameObject[0];
        return link;
    }

    private static GameObject CreatePlayer(
        Transform parent,
        CellData startCell,
        LayerMask walkableMask)
    {
        GameObject player = CreateEmpty("Player", parent);
        player.transform.position = startCell.gameObject.transform.position;
        TrySetTag(player, "Player");

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
        collider.radius = 0.18f;

        CreateSpriteVisual(
            "Visual",
            player.transform,
            Vector3.zero,
            new Vector2(0.36f, 0.36f),
            new Color(1f, 0.68f, 0.18f),
            70,
            false);

        OrigamiFoldPlayerMover mover = player.AddComponent<OrigamiFoldPlayerMover>();
        mover.moveSpeed = 3.5f;
        mover.bodyRadius = 0.18f;
        mover.sampleProbeRadius = 0.025f;
        mover.walkableMask = walkableMask;
        mover.requireAllSamplesInsideWalkable = true;

        OrigamiFoldPassenger passenger = player.AddComponent<OrigamiFoldPassenger>();
        passenger.walkableMask = walkableMask;
        passenger.probeRadius = 0.18f;
        passenger.currentStack = startCell.stack;
        passenger.disableWhileCarried = new Behaviour[] { mover };

        return player;
    }

    private static void CreateInstructionText(Transform parent)
    {
        GameObject textObject = CreateEmpty("InstructionText", parent);
        textObject.transform.position = new Vector3(-5.15f, 4.15f, 0f);

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = "WASD move. Drag wall points to fold the wall. Enter green exit.";
        text.characterSize = 0.12f;
        text.fontSize = 28;
        text.anchor = TextAnchor.UpperLeft;
        text.alignment = TextAlignment.Left;
        text.color = Color.white;

        Renderer renderer = textObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 100;
        }
    }

    private static OrigamiFoldPoint CreateFoldPoint(
        string name,
        Transform parent,
        Vector3 position,
        Color color,
        float visualSize,
        float colliderRadius,
        int sortingOrder)
    {
        GameObject pointObject = CreateEmpty(name, parent);
        pointObject.transform.position = position;

        CircleCollider2D collider = pointObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = colliderRadius;

        GameObject visual = CreateSpriteVisual(
            "Visual",
            pointObject.transform,
            Vector3.zero,
            new Vector2(visualSize, visualSize),
            color,
            sortingOrder,
            false);

        OrigamiFoldPoint point = pointObject.AddComponent<OrigamiFoldPoint>();
        point.pointId = name;
        point.normalColor = color;
        point.highlightColor = Color.yellow;
        point.visualRenderer = visual.GetComponent<Renderer>();
        return point;
    }

    private static GameObject CreateSpriteVisual(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector2 size,
        Color color,
        int sortingOrder,
        bool square)
    {
        GameObject visual = new GameObject(name);

        if (parent != null)
        {
            visual.transform.SetParent(parent, false);
        }

        visual.transform.localPosition = localPosition;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = square ? FindSquareSprite() : FindRoundSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        if (renderer.sprite != null)
        {
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
        }
        else
        {
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        return visual;
    }

    private static Sprite FindSquareSprite()
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        if (sprite != null)
        {
            return sprite;
        }

        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
    }

    private static Sprite FindRoundSprite()
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        if (sprite != null)
        {
            return sprite;
        }

        return FindSquareSprite();
    }

    private static VillageCellKind GetCellKind(int x, int y)
    {
        if (x == WallColumnX)
        {
            return VillageCellKind.Wall;
        }

        if (x == ExitBufferX)
        {
            return VillageCellKind.ExitBuffer;
        }

        if (y >= 7)
        {
            return VillageCellKind.Blocked;
        }

        if (y == 6 && (x == 1 || x == 2 || x == 3 || x == 6 || x == 7 || x == 8))
        {
            return VillageCellKind.House;
        }

        if (y == 6 && (x == 4 || x == 5))
        {
            return VillageCellKind.Door;
        }

        if ((x == 5 || x == 6) && (y == 3 || y == 4))
        {
            return VillageCellKind.Fire;
        }

        if (x >= VisibleVillageWidth)
        {
            return VillageCellKind.Blocked;
        }

        return VillageCellKind.Walkable;
    }

    private static bool IsWalkableCell(int x, int y, VillageCellKind kind)
    {
        if (x == ExitBufferX)
        {
            return true;
        }

        return kind == VillageCellKind.Walkable;
    }

    private static Color GetCellColor(VillageCellKind kind, bool walkable)
    {
        switch (kind)
        {
            case VillageCellKind.Wall:
                return new Color(0.1f, 0.62f, 0.22f, 1f);

            case VillageCellKind.ExitBuffer:
                return walkable
                    ? new Color(0.08f, 0.40f, 0.18f, 0.85f)
                    : new Color(0.04f, 0.20f, 0.10f, 0.65f);

            case VillageCellKind.House:
                return new Color(0.45f, 0.26f, 0.12f, 1f);

            case VillageCellKind.Door:
                return new Color(0.02f, 0.02f, 0.025f, 1f);

            case VillageCellKind.Fire:
                return new Color(0.72f, 0.08f, 0.04f, 1f);

            case VillageCellKind.Blocked:
                return new Color(0.11f, 0.14f, 0.12f, 0.6f);

            default:
                return new Color(0.86f, 0.88f, 0.86f, 1f);
        }
    }

    private static Vector3 CellToWorld(int x, int y)
    {
        float worldX = (x - (MapWidth - 1) / 2f) * CellSize;
        float worldY = (y - (MapHeight - 1) / 2f) * CellSize;
        return new Vector3(worldX, worldY, 0f);
    }

    private static int ResolveWalkableLayer()
    {
        int walkableLayer = LayerMask.NameToLayer("Walkable");

        if (walkableLayer >= 0)
        {
            return walkableLayer;
        }

        Debug.LogWarning("Walkable layer was not found. Village level uses Default layer for walkable areas.");
        return 0;
    }

    private static GameObject CreateEmpty(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name);

        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void TrySetTag(GameObject gameObject, string tagName)
    {
        try
        {
            gameObject.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{tagName} tag was not found. Component fallback checks will still work.");
        }
    }

    private static void AddScenesToBuildSettings()
    {
        try
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            EnsureSceneInBuildSettingsIfExists(scenes, MainMenuScenePath);
            EnsureSceneInBuildSettingsIfExists(scenes, LoadingScenePath);
            EnsureSceneInBuildSettings(scenes, VillageScenePath);
            EnsureSceneInBuildSettings(scenes, StubScenePath);

            EditorBuildSettings.scenes = scenes.ToArray();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Could not update EditorBuildSettings. {exception.Message}");
        }
    }

    private static void EnsureSceneInBuildSettings(
        List<EditorBuildSettingsScene> scenes,
        string path)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == path)
            {
                scenes[i] = new EditorBuildSettingsScene(path, true);
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
    }

    private static void EnsureSceneInBuildSettingsIfExists(
        List<EditorBuildSettingsScene> scenes,
        string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            Debug.LogWarning($"Scene was not found and was not added to Build Settings: {path}");
            return;
        }

        EnsureSceneInBuildSettings(scenes, path);
    }
}
