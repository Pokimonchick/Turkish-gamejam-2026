using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class DialogueArtAndPrologueBuilder
{
    private const string DialogueSystemPrefabPath = "Assets/Prefabs/Dialog/DialogueSystem.prefab";
    private const string DialoguesFolder = "Assets/ScriptableObjects/Dialogues";
    private const string SpeakersFolder = "Assets/ScriptableObjects/Dialogues/Speakers";
    private const string ProloguePath = DialoguesFolder + "/Prologue_Fire_Remembers.asset";
    private const string VillageMotherDialoguePath = DialoguesFolder + "/Village_NPC_Intro.asset";
    private const string VillageElderDialoguePath = DialoguesFolder + "/Village_NPC_WallHint.asset";
    private const string PreferredFontSourcePath = "Assets/artist_nouveau.ttf";
    private const string PreferredFontAssetPath = "Assets/Fonts/artist_nouveau SDF.asset";

    private const string DialogueFontCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
        "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
        " .,!?;:-/()[]\"'«»—";

    [MenuItem("Tools/PANINI/Dialog/Rebuild Dialogue Art And Prologue")]
    public static void RebuildDialogueArtAndPrologue()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot rebuild dialogue art and prologue while Unity is in Play Mode.");
            return;
        }

        DialogueArtAssets art = FindDialogueArtAssets();
        EnsureFolder(DialoguesFolder);
        EnsureFolder(SpeakersFolder);

        DialogueSpeakerProfile mother = EnsureSpeakerProfile(
            SpeakersFolder + "/Speaker_Mother.asset",
            "mother",
            "Мама",
            null);
        DialogueSpeakerProfile aisulu = EnsureSpeakerProfile(
            SpeakersFolder + "/Speaker_Aisulu.asset",
            "aisulu",
            "Айсулу",
            art.aisuluPortrait);
        DialogueSpeakerProfile umay = EnsureSpeakerProfile(
            SpeakersFolder + "/Speaker_Umay.asset",
            "umay",
            "Умай",
            art.umayPortrait);
        DialogueSpeakerProfile elder = EnsureSpeakerProfile(
            SpeakersFolder + "/Speaker_Elder_Village01.asset",
            "elder_village_01",
            "Старейшина",
            art.elderPortrait);

        DialogueData prologue = EnsurePrologueDialogue(aisulu, mother);
        EnsureSingleLineDialogue(
            VillageMotherDialoguePath,
            "Village_NPC_Mother",
            mother,
            "Будь осторожна, Айсулу. За стеной начинается путь сказания.");
        EnsureSingleLineDialogue(
            VillageElderDialoguePath,
            "Village_NPC_Elder_Wall",
            elder,
            "Огонь исчез. Айсулу, найди путь за стеной.");
        UpdateDialogueSystemPrefab(art);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Dialogue art and prologue rebuilt. Prologue lines: {prologue.lines.Count}");
    }

    private static DialogueArtAssets FindDialogueArtAssets()
    {
        DialogueArtAssets art = new DialogueArtAssets();

        art.dialogueFrame = LoadSpriteFromSingleCandidate("img 9586", "9586");
        art.topPattern = LoadSpriteFromSingleCandidate("img 9590", "9590");
        art.namePlate = LoadNamePlateSprite();
        art.aisuluPortrait = LoadSpriteFromSingleCandidate("img 9592", "9592");
        art.umayPortrait = LoadSpriteFromSingleCandidate("img 9593", "9593");
        art.elderPortrait = LoadElderPortraitSprite();

        Debug.Log(
            "Dialogue art assets found: "
            + $"frame={GetAssetPath(art.dialogueFrame)}, "
            + $"topPattern={GetAssetPath(art.topPattern)}, "
            + $"namePlate={GetAssetPath(art.namePlate)}, "
            + $"aisulu={GetAssetPath(art.aisuluPortrait)}, "
            + $"umay={GetAssetPath(art.umayPortrait)}, "
            + $"elder={GetAssetPath(art.elderPortrait)}");

        return art;
    }

    private static Sprite LoadSpriteFromSingleCandidate(string label, string token)
    {
        List<string> candidates = FindAssetPathsContaining(token);

        if (candidates.Count == 1)
        {
            return LoadSprite(candidates[0]);
        }

        Debug.LogWarning(
            $"{label} was not found unambiguously. Candidates: {FormatCandidates(candidates)}");
        return null;
    }

    private static Sprite LoadNamePlateSprite()
    {
        List<string> candidates = FindAssetPathsContaining("6356");
        string exactBlackPlatePath = "Assets/Art/Ассеты/IMG_6356 4.png";

        if (candidates.Contains(exactBlackPlatePath))
        {
            return LoadSprite(exactBlackPlatePath);
        }

        if (candidates.Count == 1)
        {
            return LoadSprite(candidates[0]);
        }

        Debug.LogWarning(
            $"img 6356 name plate was not found unambiguously. Candidates: {FormatCandidates(candidates)}");
        return null;
    }

    private static Sprite LoadElderPortraitSprite()
    {
        List<string> candidates = FindAssetPathsContaining("9601");

        if (candidates.Count == 1)
        {
            return LoadSprite(candidates[0]);
        }

        List<string> explicitCandidates = new List<string>();

        for (int i = 0; i < candidates.Count; i++)
        {
            string fileName = Path.GetFileNameWithoutExtension(candidates[i]).ToLowerInvariant();

            if (fileName.Contains("village01")
                || fileName.Contains("village_01")
                || fileName.Contains("elder")
                || fileName.Contains("стар"))
            {
                explicitCandidates.Add(candidates[i]);
            }
        }

        if (explicitCandidates.Count == 1)
        {
            return LoadSprite(explicitCandidates[0]);
        }

        Debug.LogWarning(
            $"img 9601 elder portrait is ambiguous and was not assigned. Candidates: {FormatCandidates(candidates)}");
        return null;
    }

    private static List<string> FindAssetPathsContaining(string token)
    {
        string[] guids = AssetDatabase.FindAssets(token, new[] { "Assets" });
        List<string> paths = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
            string fileName = Path.GetFileName(path);

            if (fileName.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            string extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension == ".png"
                || extension == ".jpg"
                || extension == ".jpeg"
                || extension == ".psd")
            {
                paths.Add(path);
            }
        }

        paths.Sort(StringComparer.OrdinalIgnoreCase);
        return paths;
    }

    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
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

        Debug.LogWarning($"Could not load Sprite from asset: {path}");
        return null;
    }

    private static DialogueSpeakerProfile EnsureSpeakerProfile(
        string path,
        string speakerId,
        string displayName,
        Sprite portrait)
    {
        DialogueSpeakerProfile profile =
            AssetDatabase.LoadAssetAtPath<DialogueSpeakerProfile>(path);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<DialogueSpeakerProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.speakerId = speakerId;
        profile.displayName = displayName;
        profile.portrait = portrait;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static DialogueData EnsurePrologueDialogue(
        DialogueSpeakerProfile aisulu,
        DialogueSpeakerProfile mother)
    {
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(ProloguePath);

        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, ProloguePath);
        }

        dialogue.dialogueId = "prologue_fire_remembers";
        dialogue.lines = new List<DialogueLine>
        {
            CreateLine(aisulu, "Мама, почему огонь в камине будто живой? Он всё время шевелится, словно хочет что-то сказать."),
            CreateLine(mother, "Огонь никогда не стоит на месте. Он то дышит, то танцует, а также согревает и помнит тех, кто сидел рядом с ним в долгие зимние ночи."),
            CreateLine(aisulu, "Помнит? Разве огонь может помнить?"),
            CreateLine(mother, "В старых сказаниях говорили, что огонь помнит не хуже старейшин. Он помнит руки, которые бережно подкладывали в него сухие ветви, помнит песни, которые пели возле очага, и помнит сердца, в которых было уважение."),
            CreateLine(aisulu, "А если с огнём обращались плохо?"),
            CreateLine(mother, "Тогда огонь мог обидеться. Не так, как обижаются люди, а тише и страшнее. Он мог потускнеть, почернеть от злых дел, а иногда — уйти туда, где его снова станут беречь."),
            CreateLine(aisulu, "Но ведь огонь нужен людям. Как он может уйти от них?"),
            CreateLine(mother, "Когда первые люди получили огонь, они не считали его простой вещью. Они склоняли перед ним головы, потому что он согрел их в холода, защитил от диких зверей, помог приготовить пищу и сделал ночь не такой страшной. Для них огонь был не слугой, а живым даром небес."),
            CreateLine(aisulu, "Значит, люди сначала любили огонь?"),
            CreateLine(mother, "Да, дитя моё. Но даже самый великий дар можно перестать замечать, если он каждый день горит рядом с тобой."),
            CreateLine(mother, "Есть одна сказка. Про девочку Айсулу, кочевое поселение среди степи и Умай — небесную хранительницу священного огня.")
        };

        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static DialogueData EnsureSingleLineDialogue(
        string path,
        string dialogueId,
        DialogueSpeakerProfile speakerProfile,
        string text)
    {
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);

        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, path);
        }

        dialogue.dialogueId = dialogueId;
        dialogue.lines = new List<DialogueLine>
        {
            CreateLine(speakerProfile, text)
        };

        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static DialogueLine CreateLine(DialogueSpeakerProfile speakerProfile, string text)
    {
        return new DialogueLine
        {
            speakerProfile = speakerProfile,
            speakerName = speakerProfile != null ? speakerProfile.displayName : string.Empty,
            text = text,
            portrait = speakerProfile != null ? speakerProfile.portrait : null
        };
    }

    private static void UpdateDialogueSystemPrefab(DialogueArtAssets art)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueSystemPrefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"DialogueSystem prefab was not found: {DialogueSystemPrefabPath}");
            return;
        }

        TMP_FontAsset fontAsset = ResolveProjectTmpFontAsset();
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PreferredFontSourcePath);
        GameObject root = PrefabUtility.LoadPrefabContents(DialogueSystemPrefabPath);
        DialogueManager manager = root.GetComponentInChildren<DialogueManager>(true);

        if (manager == null)
        {
            Debug.LogWarning("DialogueSystem prefab has no DialogueManager.");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        GameObject dialogRoot = GetObjectReference<GameObject>(serializedManager, "dialogRoot");
        TextMeshProUGUI speakerNameText =
            GetObjectReference<TextMeshProUGUI>(serializedManager, "speakerNameText");
        TextMeshProUGUI bodyText =
            GetObjectReference<TextMeshProUGUI>(serializedManager, "bodyText");
        Image portraitImage = GetObjectReference<Image>(serializedManager, "portraitImage");
        GameObject continueHintObject =
            GetObjectReference<GameObject>(serializedManager, "continueHintObject");

        Canvas canvas = root.GetComponentInChildren<Canvas>(true);

        if (canvas == null)
        {
            canvas = CreateCanvas(root.transform);
        }

        ConfigureCanvas(canvas);

        if (dialogRoot == null)
        {
            dialogRoot = FindDeepChild(root.transform, "DialogRoot")?.gameObject
                ?? FindDeepChild(root.transform, "Dialog Root")?.gameObject
                ?? CreateUiObject("Dialog Root", canvas.transform);
        }

        dialogRoot.name = "DialogRoot";
        dialogRoot.transform.SetParent(canvas.transform, false);
        Stretch(dialogRoot.GetComponent<RectTransform>());

        Image topPatternImage = EnsureImageObject("TopPatternImage", canvas.transform, art.topPattern);
        ConfigureTopPattern(topPatternImage);
        SetLayerRecursive(topPatternImage.gameObject, LayerMask.NameToLayer("UI"));

        Image dialogueFrameImage = EnsureImageObject(
            "DialogueFrameImage",
            dialogRoot.transform,
            art.dialogueFrame);
        ConfigureDialogueFrame(dialogueFrameImage);

        Image namePlateImage = EnsureImageObject("NamePlateImage", dialogRoot.transform, art.namePlate);
        ConfigureNamePlate(namePlateImage);

        speakerNameText = EnsureTmpTextObject("SpeakerNameText", namePlateImage.transform, speakerNameText);
        bodyText = EnsureTmpTextObject("BodyText", dialogueFrameImage.transform, bodyText);
        portraitImage = EnsureImageObject("PortraitImage", dialogRoot.transform, null, portraitImage);
        continueHintObject = EnsureContinueHint(
            dialogueFrameImage.transform,
            continueHintObject,
            fontAsset);

        Image dialogPanelImage = FindDeepChild(dialogRoot.transform, "Dialog Panel")?.GetComponent<Image>();

        if (dialogPanelImage != null)
        {
            dialogPanelImage.color = new Color(0f, 0f, 0f, 0f);
            dialogPanelImage.raycastTarget = false;
        }

        if (portraitImage != null)
        {
            ConfigurePortrait(portraitImage);
        }

        if (speakerNameText != null)
        {
            ConfigureSpeakerName(speakerNameText, fontAsset);
        }

        if (bodyText != null)
        {
            ConfigureBodyText(bodyText, fontAsset);
        }

        if (continueHintObject != null)
        {
            ApplyFontToTmpChildren(continueHintObject, fontAsset);
        }

        dialogRoot.SetActive(false);
        topPatternImage.gameObject.SetActive(false);

        SetObjectReference(serializedManager, "dialogRoot", dialogRoot);
        SetObjectReference(serializedManager, "speakerNameText", speakerNameText);
        SetObjectReference(serializedManager, "bodyText", bodyText);
        SetObjectReference(serializedManager, "portraitImage", portraitImage);
        SetObjectReference(serializedManager, "continueHintObject", continueHintObject);
        SetObjectReference(serializedManager, "dialogueFrameImage", dialogueFrameImage);
        SetObjectReference(serializedManager, "topPatternImage", topPatternImage);
        SetObjectReference(serializedManager, "namePlateImage", namePlateImage);
        SetObjectReference(serializedManager, "uiSourceFont", sourceFont);
        SetObjectReference(serializedManager, "uiFontAsset", fontAsset);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, DialogueSystemPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("DialogueSystem prefab art refs updated.");
    }

    private static Image EnsureImageObject(
        string name,
        Transform parent,
        Sprite sprite,
        Image existing = null)
    {
        Transform child = existing != null ? existing.transform : FindDeepChild(parent, name);
        GameObject gameObject = child != null ? child.gameObject : CreateUiObject(name, parent);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();

        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }

        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    private static TextMeshProUGUI EnsureTmpTextObject(
        string name,
        Transform parent,
        TextMeshProUGUI existing = null)
    {
        Transform child = existing != null ? existing.transform : FindDeepChild(parent, name);
        GameObject gameObject = child != null ? child.gameObject : CreateUiObject(name, parent);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();

        if (text == null)
        {
            text = gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static GameObject EnsureContinueHint(
        Transform parent,
        GameObject existing,
        TMP_FontAsset fontAsset)
    {
        GameObject gameObject = existing != null
            ? existing
            : FindDeepChild(parent, "ContinueHint")?.gameObject ?? CreateUiObject("ContinueHint", parent);
        gameObject.name = "ContinueHint";
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();

        if (rect == null)
        {
            gameObject = CreateUiObject("ContinueHint", parent);
            rect = gameObject.GetComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-65f, 32f);
        rect.sizeDelta = new Vector2(470f, 42f);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();

        if (text == null)
        {
            text = gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (text == null)
        {
            text = gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.text = "E / Enter    Space — пропуск";
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 26f;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = new Color(0.16f, 0.08f, 0.06f, 0.86f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform textRect = text.GetComponent<RectTransform>();

        if (text.gameObject != gameObject && textRect != null)
        {
            text.gameObject.name = "ContinueHintText";
            text.transform.SetParent(gameObject.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        if (fontAsset != null)
        {
            text.font = fontAsset;
        }

        ApplyFontToTmpChildren(gameObject, fontAsset);
        return gameObject;
    }

    private static void ConfigureTopPattern(Image image)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 104f);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        rect.SetAsFirstSibling();
    }

    private static void ConfigureDialogueFrame(Image image)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 35f);
        rect.sizeDelta = new Vector2(1550f, 270f);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        rect.SetSiblingIndex(1);
    }

    private static void ConfigureNamePlate(Image image)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 305f);
        rect.sizeDelta = new Vector2(360f, 80f);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        rect.SetSiblingIndex(2);
    }

    private static void ConfigurePortrait(Image image)
    {
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(500f, 260f);
        rect.sizeDelta = new Vector2(360f, 470f);
        image.preserveAspect = true;
        image.raycastTarget = false;
        rect.SetSiblingIndex(0);
    }

    private static void ConfigureSpeakerName(TextMeshProUGUI text, TMP_FontAsset fontAsset)
    {
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(20f, 8f);
        rect.offsetMax = new Vector2(-20f, -8f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 42f;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        if (fontAsset != null)
        {
            text.font = fontAsset;
        }
    }

    private static void ConfigureBodyText(TextMeshProUGUI text, TMP_FontAsset fontAsset)
    {
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(190f, 55f);
        rect.offsetMax = new Vector2(-190f, -80f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 38f;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = new Color(0.12f, 0.08f, 0.06f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;

        if (fontAsset != null)
        {
            text.font = fontAsset;
        }
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
        canvasObject.transform.SetParent(parent, false);
        int uiLayer = LayerMask.NameToLayer("UI");

        if (uiLayer >= 0)
        {
            canvasObject.layer = uiLayer;
        }

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = 100;
        canvas.worldCamera = null;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static TMP_FontAsset ResolveProjectTmpFontAsset()
    {
        TMP_FontAsset preferredFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);

        if (IsUsableFontAsset(preferredFont))
        {
            return preferredFont;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PreferredFontSourcePath);

        if (sourceFont == null)
        {
            return null;
        }

        EnsureFolder("Assets/Fonts");
        if (preferredFont != null)
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

    private static void ApplyFontToTmpChildren(GameObject root, TMP_FontAsset fontAsset)
    {
        if (root == null || fontAsset == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].font = fontAsset;
            EditorUtility.SetDirty(texts[i]);
        }
    }

    private static void SetLayerRecursive(GameObject root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.layer = layer;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            SetLayerRecursive(root.transform.GetChild(i).gameObject, layer);
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
            {
                return child;
            }

            Transform result = FindDeepChild(child, name);

            if (result != null)
            {
                return result;
            }
        }

        return null;
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

    private static T GetObjectReference<T>(SerializedObject serializedObject, string propertyName)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static void SetObjectReference(
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

    private static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');

        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static string FormatCandidates(List<string> candidates)
    {
        return candidates == null || candidates.Count == 0
            ? "<none>"
            : string.Join(", ", candidates);
    }

    private static string GetAssetPath(UnityEngine.Object asset)
    {
        return asset != null ? AssetDatabase.GetAssetPath(asset) : "<none>";
    }

    private class DialogueArtAssets
    {
        public Sprite dialogueFrame;
        public Sprite topPattern;
        public Sprite namePlate;
        public Sprite aisuluPortrait;
        public Sprite umayPortrait;
        public Sprite elderPortrait;
    }
}
