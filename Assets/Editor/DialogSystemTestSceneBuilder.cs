using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DialogSystemTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Test/Test_DialogSystem.unity";
    private const string DialogueDataPath = "Assets/ScriptableObjects/Dialogues/TestDialogue.asset";
    private const string TmpFontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string ContinueHintText = "E / Enter / Click\nPress Space to Skip";

    [MenuItem("Tools/Dialog/Create Test Dialog Scene")]
    public static void CreateTestSceneIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EnsureTmpEssentialResources();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EnsureExistingTestSceneUi();
            return;
        }

        CreateOrReplaceTestScene();
    }

    [MenuItem("Tools/Dialog/Rebuild Test Dialog Scene")]
    public static void CreateOrReplaceTestScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Scenes/Test");
        EnsureTmpEssentialResources();
        EnsurePlayerTag();

        var previousActiveScene = SceneManager.GetActiveScene();
        var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        testScene.name = "Test_DialogSystem";
        SceneManager.SetActiveScene(testScene);

        var mainCamera = CreateMainCamera();
        var canvas = CreateCanvas();
        CreateEventSystem();

        var dialogRoot = CreateDialogUi(canvas.transform);
        var speakerNameText = dialogRoot.transform.Find("Dialog Panel/Speaker Name Text")
            .GetComponent<TextMeshProUGUI>();
        var bodyText = dialogRoot.transform.Find("Dialog Panel/Body Text")
            .GetComponent<TextMeshProUGUI>();
        var portraitImage = dialogRoot.transform.Find("Dialog Panel/Portrait Image")
            .GetComponent<Image>();
        var continueHintText = dialogRoot.transform.Find("Dialog Panel/Continue Hint Text").gameObject;
        dialogRoot.SetActive(false);

        var interactionHint = CreateInteractionHint(canvas.transform);
        interactionHint.SetActive(false);

        var dialogueSystem = new GameObject("Dialogue System");
        var dialogueManager = dialogueSystem.AddComponent<DialogueManager>();
        AssignDialogueManager(dialogueManager, dialogRoot, speakerNameText, bodyText, portraitImage, continueHintText);

        var player = CreatePlayer();
        var npc = CreateNpc(interactionHint);

        Selection.objects = new Object[] { mainCamera, canvas.gameObject, dialogueSystem, player, npc };

        EditorSceneManager.SaveScene(testScene, ScenePath);
        EditorSceneManager.CloseScene(testScene, true);

        if (previousActiveScene.IsValid())
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreateMainCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1.5f, -10f);

        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;

        cameraObject.AddComponent<AudioListener>();
        return cameraObject;
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas");
        canvasObject.layer = LayerMask.NameToLayer("UI");

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 4f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateDialogUi(Transform canvasTransform)
    {
        var dialogRoot = CreateUiObject("Dialog Root", canvasTransform);
        Stretch(dialogRoot.GetComponent<RectTransform>());

        var panel = CreateUiObject("Dialog Panel", dialogRoot.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.04f);
        panelRect.anchorMax = new Vector2(0.95f, 0.34f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.035f, 0.045f, 0.88f);

        var portrait = CreateUiObject("Portrait Image", panel.transform);
        var portraitRect = portrait.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0f);
        portraitRect.anchorMax = new Vector2(0f, 1f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(32f, 0f);
        portraitRect.sizeDelta = new Vector2(220f, -48f);

        var portraitImage = portrait.AddComponent<Image>();
        portraitImage.color = new Color(1f, 1f, 1f, 0.18f);
        portraitImage.preserveAspect = true;

        var speaker = CreateTmpText("Speaker Name Text", panel.transform, "\u0421\u0442\u0430\u0440\u0438\u043a", 38f, FontStyles.Bold);
        var speakerRect = speaker.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.offsetMin = new Vector2(284f, -84f);
        speakerRect.offsetMax = new Vector2(-36f, -24f);
        speaker.alignment = TextAlignmentOptions.Left;
        speaker.color = new Color(1f, 0.78f, 0.35f);

        var body = CreateTmpText(
            "Body Text",
            panel.transform,
            "\u042d\u0442\u0430 \u043a\u043d\u0438\u0433\u0430 \u043d\u0435 \u0442\u0430\u043a\u0430\u044f \u043f\u0440\u043e\u0441\u0442\u0430\u044f, \u043a\u0430\u043a \u043a\u0430\u0436\u0435\u0442\u0441\u044f.",
            34f,
            FontStyles.Normal);
        var bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(284f, 78f);
        bodyRect.offsetMax = new Vector2(-36f, -104f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.color = new Color(0.94f, 0.92f, 0.86f);

        var hint = CreateTmpText("Continue Hint Text", panel.transform, ContinueHintText, 26f, FontStyles.Normal);
        var hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-36f, 26f);
        hintRect.sizeDelta = new Vector2(420f, 68f);
        hint.alignment = TextAlignmentOptions.Right;
        hint.color = new Color(1f, 0.78f, 0.35f);

        return dialogRoot;
    }

    private static GameObject CreateInteractionHint(Transform canvasTransform)
    {
        var hint = CreateTmpText("Interaction Hint Text", canvasTransform, "Press E", 32f, FontStyles.Bold);
        var rect = hint.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 145f);
        rect.sizeDelta = new Vector2(260f, 60f);
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(1f, 0.84f, 0.45f);
        return hint.gameObject;
    }

    private static GameObject CreatePlayer()
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(-1.8f, 0f, 0f);
        player.transform.localScale = new Vector3(0.75f, 1f, 0.75f);
        SetRendererColor(player, new Color(0.25f, 0.55f, 1f));
        return player;
    }

    private static GameObject CreateNpc(GameObject interactionHint)
    {
        var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "Test NPC";
        npc.transform.position = new Vector3(0.7f, 0f, 0f);
        npc.transform.localScale = new Vector3(0.75f, 1f, 0.75f);
        SetRendererColor(npc, new Color(1f, 0.68f, 0.28f));

        var interactable = npc.AddComponent<NPCInteractable>();
        var serializedObject = new SerializedObject(interactable);
        serializedObject.FindProperty("dialogueData").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DialogueData>(DialogueDataPath);
        serializedObject.FindProperty("interactionDistance").floatValue = 3f;
        serializedObject.FindProperty("interactKey").intValue = (int)KeyCode.E;
        serializedObject.FindProperty("interactionHint").objectReferenceValue = interactionHint;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return npc;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static TextMeshProUGUI CreateTmpText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle)
    {
        var gameObject = CreateUiObject(name, parent);
        var tmp = gameObject.AddComponent<TextMeshProUGUI>();
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontAssetPath);
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
            tmp.fontSharedMaterial = fontAsset.material;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static void AssignDialogueManager(
        DialogueManager manager,
        GameObject dialogRoot,
        TextMeshProUGUI speakerNameText,
        TextMeshProUGUI bodyText,
        Image portraitImage,
        GameObject continueHintObject)
    {
        var serializedObject = new SerializedObject(manager);
        serializedObject.FindProperty("dialogRoot").objectReferenceValue = dialogRoot;
        serializedObject.FindProperty("speakerNameText").objectReferenceValue = speakerNameText;
        serializedObject.FindProperty("bodyText").objectReferenceValue = bodyText;
        serializedObject.FindProperty("portraitImage").objectReferenceValue = portraitImage;
        serializedObject.FindProperty("continueHintObject").objectReferenceValue = continueHintObject;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetRendererColor(GameObject gameObject, Color color)
    {
        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(shader);
            renderer.sharedMaterial.color = color;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folder = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void EnsureTmpEssentialResources()
    {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
        {
            return;
        }

        TMP_PackageResourceImporter.ImportResources(true, false, false);
        AssetDatabase.Refresh();
    }

    private static void EnsureExistingTestSceneUi()
    {
        var scene = GetLoadedScene(ScenePath);
        var openedForUpdate = !scene.IsValid();
        if (openedForUpdate)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            if (!scene.IsValid())
            {
                return;
            }

            var hint = FindInScene(scene, "Continue Hint Text");
            if (hint == null)
            {
                return;
            }

            var hintText = hint.GetComponent<TextMeshProUGUI>();
            if (hintText != null)
            {
                hintText.text = ContinueHintText;
                hintText.alignment = TextAlignmentOptions.Right;
                hintText.textWrappingMode = TextWrappingModes.Normal;
                EditorUtility.SetDirty(hintText);
            }

            var hintRect = hint.GetComponent<RectTransform>();
            if (hintRect != null)
            {
                hintRect.sizeDelta = new Vector2(420f, 68f);
                hintRect.anchoredPosition = new Vector2(-36f, 26f);
                EditorUtility.SetDirty(hintRect);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (openedForUpdate && scene.IsValid())
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static Scene GetLoadedScene(string scenePath)
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.path == scenePath)
            {
                return scene;
            }
        }

        return default;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var item in transforms)
            {
                if (item.gameObject.name == objectName)
                {
                    return item.gameObject;
                }
            }
        }

        return null;
    }

    private static void EnsurePlayerTag()
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");

        for (var i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "Player")
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Player";
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }
}
