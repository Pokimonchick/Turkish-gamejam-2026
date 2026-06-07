using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class DialogPrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Dialog";
    private const string DialogueSystemPrefabPath = PrefabFolder + "/DialogueSystem.prefab";
    private const string TestNpcPrefabPath = PrefabFolder + "/TestNPC_Dialogue.prefab";
    private const string PreferredFontSourcePath = "Assets/artist_nouveau.ttf";
    private const string PreferredFontAssetPath = "Assets/Fonts/artist_nouveau SDF.asset";
    private const string FallbackFontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private const string DialogueFontCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
        "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
        " .,!?;:-/()[]\"'«»";

    [MenuItem("Tools/Dialog/Create Dialog Prefabs")]
    public static void CreatePrefabsIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsurePreferredFontAsset();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(DialogueSystemPrefabPath) == null)
        {
            CreateDialogueSystemPrefab();
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(TestNpcPrefabPath) == null)
        {
            CreateTestNpcPrefab();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Dialog/Repair Dialogue Font")]
    public static void RepairDialogueFont()
    {
        EnsurePreferredFontAsset();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Dialog/Rebuild Dialog Prefabs")]
    public static void RebuildPrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsurePreferredFontAsset();
        CreateDialogueSystemPrefab();
        CreateTestNpcPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateDialogueSystemPrefab()
    {
        var root = new GameObject("Dialogue System");
        var manager = root.AddComponent<DialogueManager>();

        var canvas = CreateCanvas(root.transform);
        var dialogRoot = CreateDialogUi(canvas.transform);
        var speakerNameText = dialogRoot.transform.Find("Dialog Panel/Speaker Name Text")
            .GetComponent<TextMeshProUGUI>();
        var bodyText = dialogRoot.transform.Find("Dialog Panel/Body Text")
            .GetComponent<TextMeshProUGUI>();
        var portraitImage = dialogRoot.transform.Find("Dialog Panel/Portrait Image")
            .GetComponent<Image>();
        var continueHintObject = dialogRoot.transform.Find("Dialog Panel/Continue Hint Text").gameObject;
        dialogRoot.SetActive(false);

        AssignDialogueManager(manager, dialogRoot, speakerNameText, bodyText, portraitImage, continueHintObject);

        PrefabUtility.SaveAsPrefabAsset(root, DialogueSystemPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(parent, false);

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
        body.color = new Color(0.94f, 0.92f, 0.86f);

        var hint = CreateTmpText("Continue Hint Text", panel.transform, "E / Enter / Click\nPress Space to Skip", 26f, FontStyles.Normal);
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

    private static TextMeshProUGUI CreateTmpText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles fontStyle)
    {
        var gameObject = CreateUiObject(name, parent);
        var tmp = gameObject.AddComponent<TextMeshProUGUI>();
        var fontAsset = LoadPreferredFontAsset();
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static TMP_FontAsset LoadPreferredFontAsset()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);
        if (IsUsableFontAsset(fontAsset))
        {
            return fontAsset;
        }

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);
    }

    private static void EnsurePreferredFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);
        if (IsUsableFontAsset(existing))
        {
            return;
        }

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PreferredFontSourcePath);
        if (sourceFont == null)
        {
            return;
        }

        EnsureFolder("Assets/Fonts");

        var metaPath = PreferredFontAssetPath + ".meta";
        var meta = File.Exists(metaPath) ? File.ReadAllText(metaPath) : null;

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(PreferredFontAssetPath);
            if (!string.IsNullOrEmpty(meta))
            {
                File.WriteAllText(metaPath, meta);
            }

            AssetDatabase.Refresh();
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            128,
            12,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
        {
            return;
        }

        fontAsset.name = "artist_nouveau SDF";
        fontAsset.TryAddCharacters(DialogueFontCharacters, out _, false);

        AssetDatabase.CreateAsset(fontAsset, PreferredFontAssetPath);
        AddFontSubAssets(fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(PreferredFontAssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void AddFontSubAssets(TMP_FontAsset fontAsset)
    {
        if (fontAsset.material != null)
        {
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        if (fontAsset.atlasTextures == null)
        {
            return;
        }

        foreach (var texture in fontAsset.atlasTextures)
        {
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

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
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
        serializedObject.FindProperty("uiSourceFont").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Font>(PreferredFontSourcePath);
        serializedObject.FindProperty("uiFontAsset").objectReferenceValue = LoadPreferredFontAsset();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTestNpcPrefab()
    {
        var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "Test NPC";
        npc.transform.localScale = new Vector3(0.75f, 1f, 0.75f);
        SetRendererColor(npc, new Color(1f, 0.68f, 0.28f));

        var interactable = npc.AddComponent<NPCInteractable>();
        var serializedObject = new SerializedObject(interactable);
        serializedObject.FindProperty("dialogueData").objectReferenceValue = null;
        serializedObject.FindProperty("player").objectReferenceValue = null;
        serializedObject.FindProperty("interactionDistance").floatValue = 3f;
        serializedObject.FindProperty("interactKey").intValue = (int)KeyCode.E;
        serializedObject.FindProperty("interactionHint").objectReferenceValue = null;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(npc, TestNpcPrefabPath);
        Object.DestroyImmediate(npc);
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
        if (renderer == null)
        {
            return;
        }

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
}
