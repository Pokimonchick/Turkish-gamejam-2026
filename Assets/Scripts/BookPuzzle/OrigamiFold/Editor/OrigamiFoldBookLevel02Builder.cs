using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OrigamiFoldBookLevel02Builder
{
    private const string LevelScenePath = "Assets/Scenes/Book_Level_02_Greybox.unity";
    private const string FoldNodeSpritePath = "Assets/Art/UI/Node.PNG";
    private const int MapWidth = 12;
    private const int MapHeight = 9;
    private const float CellSize = 1f;
    private const int CenterColumnFoldX = 5;
    private const int TriadColumnFoldX = 7;
    private const int TriadRowFoldY = 5;
    private const string PlayerSpriteGuid = "77d3b28359b42e440905b56447f58511";
    private const string DefaultFootstepProfilePath = "Assets/Resources/Audio/DefaultFootstepAudioProfile.asset";
    private const string DialogueSystemPrefabPath = "Assets/Prefabs/Dialog/DialogueSystem.prefab";
    private const string InteractionPromptMessage = "E - interaction";
    private const string PreferredFontAssetPath = "Assets/Fonts/artist_nouveau SDF.asset";
    private const string TimurDialoguePath = "Assets/ScriptableObjects/Dialogues/Book_Level_02_Timur.asset";
    private const string TimurSpriteSearchName = "\u0422\u0438\u043c\u0443\u0440";
    private const string NextSceneName = "Book_Level_03_Greybox";
    private const float FoldNodeVisualSize = 0.55f;
    private const float FoldNodeGlowSize = 0.9f;
    private const float FoldNodeColliderRadius = 0.5f;
    private const int FoldNodeSortingOrder = 95;
    private const bool ShowCellDebugOverlay = false;
    private const float PlayerFootprintRadius = 0.12f;
    private const float PlayerVisualFootYOffset = 0.02f;
    private static readonly Vector3 PlayerVisualLocalScale = new Vector3(0.22f, 0.22f, 1f);

    private static readonly string[] LayoutTopToBottom =
    {
        "............",
        ".....F.GBG..",
        "..GGGFGGNG..",
        ".GGGGFGGFFF.",
        "GGGGG..FB.P.",
        "GG.....GBGG.",
        ".....GGGGGF.",
        "....SGG.....",
        "............"
    };

    private class CellData
    {
        public GameObject gameObject;
        public OrigamiFoldTransformStack stack;
        public Vector2Int gridPosition;
        public char tile;
    }

    public static void CreateBookLevel02Greybox()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot rebuild Book Level 02 while Unity is in Play Mode.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Book_Level_02_Greybox";

        Camera mainCamera = CreateMainCamera();
        DialogueData timurDialogue = EnsureTimurDialogueData();

        GameObject levelRoot = CreateEmpty("LEVEL_ROOT", null);
        OrigamiFoldLevelAudioBuilder.CreateGameplayAudio(
            levelRoot.transform,
            OrigamiFoldLevelAudioBuilder.ForestAmbiencePath);
        GameObject foldSystemRoot = CreateEmpty("ORIGAMI_FOLD_SYSTEM", levelRoot.transform);
        GameObject mapRoot = CreateEmpty("BOOK_LEVEL_MAP", levelRoot.transform);
        GameObject cellsRoot = CreateEmpty("Cells", mapRoot.transform);
        GameObject pointsRoot = CreateEmpty("ORIGAMI_FOLD_POINTS", levelRoot.transform);
        GameObject linksRoot = CreateEmpty("ORIGAMI_FOLD_LINKS", levelRoot.transform);
        GameObject actionsRoot = CreateEmpty("ORIGAMI_ACTIONS", levelRoot.transform);
        GameObject playerRoot = CreateEmpty("BOOK_LEVEL_PLAYER", levelRoot.transform);
        GameObject npcsRoot = CreateEmpty("BOOK_LEVEL_NPCS", levelRoot.transform);
        GameObject debugRoot = CreateEmpty("BOOK_LEVEL_DEBUG", levelRoot.transform);

        OrigamiFoldActionCoordinator coordinator = CreateCoordinator(foldSystemRoot.transform);
        CellData[,] cells = CreateMapCells(cellsRoot.transform);

        int walkableLayer = ResolveWalkableLayer();
        int walkableMask = 1 << walkableLayer;
        CreateWalkableAreas(cells, walkableLayer);

        OrigamiFoldStripSqueezeAction centerColumnAction = CreateColumnFoldAction(
            "CenterColumnFold_x5",
            cells,
            actionsRoot.transform,
            CenterColumnFoldX,
            coordinator);
        OrigamiFoldStripSqueezeAction triadColumnAction = CreateColumnFoldAction(
            "TriadColumnFold_x7",
            cells,
            actionsRoot.transform,
            TriadColumnFoldX,
            coordinator);
        OrigamiFoldStripSqueezeAction triadRowAction = CreateRowFoldAction(
            "TriadRowFold_y5",
            cells,
            actionsRoot.transform,
            TriadRowFoldY,
            coordinator);

        CreateCenterFoldControls(
            pointsRoot.transform,
            linksRoot.transform,
            centerColumnAction,
            cells[CenterColumnFoldX, 3]);
        CreateTriadFoldControls(
            pointsRoot.transform,
            linksRoot.transform,
            actionsRoot.transform,
            triadColumnAction,
            triadRowAction,
            coordinator,
            cells[TriadColumnFoldX, TriadRowFoldY]);

        OrigamiFoldLink[] links = linksRoot.GetComponentsInChildren<OrigamiFoldLink>(true);
        CreateDragController(foldSystemRoot.transform, mainCamera, links);
        GameObject player = CreatePlayer(playerRoot.transform, cells[1, 3], walkableMask);
        CreateRespawnPoint(playerRoot.transform, cells[1, 3]);
        DialogueManager dialogueManager = CreateDialogueSystem(levelRoot.transform);
        EnsureInteractionPromptUi(dialogueManager);
        CreateEventSystemIfMissing();
        CreateNpcPlaceholder(npcsRoot.transform, cells[8, 6]);
        CreateNpcZonePlaceholder(npcsRoot.transform, cells[10, 4]);
        GameObject finish = CreateExitPlaceholder(npcsRoot.transform, cells[5, 1]);
        finish.SetActive(false);
        CreateTimurNpc(npcsRoot.transform, cells[10, 3], player.transform, timurDialogue, finish);
        AddSceneToBuildSettings(LevelScenePath);
        Selection.activeGameObject = levelRoot;
        EditorGUIUtility.PingObject(levelRoot);

        EditorSceneManager.SaveScene(scene, LevelScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Created {LevelScenePath}. "
            + "Fold links: CenterLeft<->CenterRight, TriadA<->TriadB, TriadB<->TriadC, "
            + "TriadAB<->TriadC_AfterColumn, TriadA_AfterRow<->TriadBC. "
            + "No TriadA<->TriadC diagonal link was created.");
    }

    public static void ApplyBookLevel02WalkabilityLayoutToScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot update Book Level 02 walkability while Unity is in Play Mode.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        DialogueData timurDialogue = EnsureTimurDialogueData();
        int walkableLayer = ResolveWalkableLayer();
        int walkableCount = 0;
        int blockedCount = 0;
        int missingCells = 0;

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                GameObject cell = GameObject.Find($"MapCell_{x}_{y}");

                if (cell == null)
                {
                    missingCells++;
                    continue;
                }

                char tile = GetLayoutTile(x, y);
                bool isWalkable = IsWalkableTile(tile);
                SyncGreyboxVisual(cell.transform, tile);
                SyncWalkableArea(cell, isWalkable, walkableLayer);
                SyncCellDebugOverlay(cell.transform, isWalkable);

                if (isWalkable)
                {
                    walkableCount++;
                }
                else
                {
                    blockedCount++;
                }
            }
        }

        CleanupPresentationDebugObjects();

        Transform levelRoot = FindOrCreateRoot("LEVEL_ROOT");
        Transform npcsRoot = FindOrCreateChild(levelRoot, "BOOK_LEVEL_NPCS");
        Transform player = FindPlayerTransform();
        DialogueManager dialogueManager = CreateDialogueSystem(levelRoot);
        EnsureInteractionPromptUi(dialogueManager);
        CreateEventSystemIfMissing();

        DestroySceneObjectsNamed("Timur_NPC");
        DestroySceneObjectsNamed("ExitPlaceholder");
        Transform timurCell = FindMapCellTransform(10, 3);
        Transform finishCell = FindMapCellTransform(5, 1);

        GameObject finish = null;
        if (finishCell != null)
        {
            finish = CreateExitPlaceholder(npcsRoot, finishCell);
            finish.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Could not create Book Level 02 finish: MapCell_5_1 is missing.");
        }

        if (timurCell != null)
        {
            CreateTimurNpc(npcsRoot, timurCell, player, timurDialogue, finish);
        }
        else
        {
            Debug.LogWarning("Could not create Timur NPC: MapCell_10_3 is missing.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, LevelScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Updated Book Level 02 gameplay layout. "
            + $"Walkable cells: {walkableCount}, blocked cells: {blockedCount}, missing cells: {missingCells}. "
            + "Timur NPC target: 10,3. Finish target: 5,1.");
    }

    public static void CleanBookLevel02PresentationDebug()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot clean Book Level 02 presentation debug while Unity is in Play Mode.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        CleanupPresentationDebugObjects();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, LevelScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Book Level 02 presentation debug cleaned: grid, coordinate labels, and walkable highlights removed.");
    }

    private static char GetLayoutTile(int x, int y)
    {
        return LayoutTopToBottom[MapHeight - 1 - y][x];
    }

    private static void SyncGreyboxVisual(Transform cell, char tile)
    {
        Transform visual = FindDirectChild(cell, "Visual");

        if (visual == null)
        {
            return;
        }

        Renderer renderer = visual.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        renderer.sharedMaterial = CreateMaterial(GetCellColor(tile));
    }

    private static void SyncWalkableArea(GameObject cell, bool isWalkable, int walkableLayer)
    {
        DestroyExtraDirectChildrenNamed(cell.transform, "WalkableArea", isWalkable ? 1 : 0);
        Transform walkable = FindDirectChild(cell.transform, "WalkableArea");

        if (!isWalkable)
        {
            if (walkable != null)
            {
                Object.DestroyImmediate(walkable.gameObject);
            }

            return;
        }

        if (walkable == null)
        {
            walkable = CreateEmpty("WalkableArea", cell.transform).transform;
        }

        walkable.gameObject.SetActive(true);
        walkable.gameObject.layer = walkableLayer;
        walkable.localPosition = Vector3.zero;
        walkable.localRotation = Quaternion.identity;
        walkable.localScale = Vector3.one;

        BoxCollider2D collider = walkable.GetComponent<BoxCollider2D>();

        if (collider == null)
        {
            collider = walkable.gameObject.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
        collider.size = new Vector2(1f, 1f);
        collider.offset = Vector2.zero;

        OrigamiFoldWalkableArea area = walkable.GetComponent<OrigamiFoldWalkableArea>();

        if (area == null)
        {
            area = walkable.gameObject.AddComponent<OrigamiFoldWalkableArea>();
        }

        area.ownerStack = cell.GetComponent<OrigamiFoldTransformStack>();
        area.isWalkable = true;
    }

    private static void SyncCellDebugOverlay(Transform cell, bool isWalkable)
    {
        if (!ShowCellDebugOverlay)
        {
            RemoveCellDebugOverlay(cell);
            return;
        }

        SyncWalkableDebugHighlight(cell, isWalkable);
    }

    private static void SyncWalkableDebugHighlight(Transform cell, bool isWalkable)
    {
        DestroyExtraDirectChildrenNamed(cell, "WalkableDebugHighlight", isWalkable ? 1 : 0);
        Transform highlight = FindDirectChild(cell, "WalkableDebugHighlight");

        if (!isWalkable)
        {
            if (highlight != null)
            {
                Object.DestroyImmediate(highlight.gameObject);
            }

            return;
        }

        if (highlight == null)
        {
            CreateQuad(
                "WalkableDebugHighlight",
                cell,
                new Vector3(0f, 0f, -0.045f),
                new Vector3(0.86f, 0.86f, 1f),
                new Color(0.1f, 1f, 0.42f, 0.28f),
                62);
            return;
        }

        highlight.gameObject.SetActive(true);
        highlight.localPosition = new Vector3(0f, 0f, -0.045f);
        highlight.localRotation = Quaternion.identity;
        highlight.localScale = new Vector3(0.86f, 0.86f, 1f);

        Renderer renderer = highlight.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(new Color(0.1f, 1f, 0.42f, 0.28f));
            renderer.sortingOrder = 62;
        }
    }

    private static void CleanupPresentationDebugObjects()
    {
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Transform cell = FindMapCellTransform(x, y);

                if (cell != null)
                {
                    RemoveCellDebugOverlay(cell);
                }
            }
        }

        DestroySceneObjectsNamed("GridGuides");
    }

    private static void RemoveCellDebugOverlay(Transform cell)
    {
        DestroyExtraDirectChildrenNamed(cell, "WalkableDebugHighlight", 0);
        DestroyExtraDirectChildrenNamed(cell, "Grid_Top", 0);
        DestroyExtraDirectChildrenNamed(cell, "Grid_Bottom", 0);
        DestroyExtraDirectChildrenNamed(cell, "Grid_Left", 0);
        DestroyExtraDirectChildrenNamed(cell, "Grid_Right", 0);
        DestroyExtraDirectChildrenNamed(cell, "CoordinateLabel", 0);
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static void DestroyExtraDirectChildrenNamed(Transform parent, string childName, int keepCount)
    {
        int seen = 0;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child.name != childName)
            {
                continue;
            }

            seen++;

            if (seen > keepCount)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static DialogueData EnsureTimurDialogueData()
    {
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/Dialogues");

        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(TimurDialoguePath);

        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, TimurDialoguePath);
        }

        Sprite timurPortrait = FindTimurSprite();
        dialogue.dialogueId = "book_level_02_timur";
        dialogue.lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerName = "Тимур",
                text = "Айсулу, за этой дорогой начинается новый путь. Слушай огонь и не спорь со складками страницы.",
                portrait = timurPortrait
            },
            new DialogueLine
            {
                speakerName = "Тимур",
                text = "Когда будешь готова, иди к зеленому огоньку у нижней тропы.",
                portrait = timurPortrait
            }
        };

        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);

        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static Sprite FindTimurSprite()
    {
        string[] guids = AssetDatabase.FindAssets(TimurSpriteSearchName);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (!IsSpriteTexturePath(path))
            {
                continue;
            }

            EnsureTextureIsSprite(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                return sprite;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
            {
                if (assets[assetIndex] is Sprite nestedSprite)
                {
                    return nestedSprite;
                }
            }
        }

        Debug.LogWarning("Timur sprite was not found. Expected an asset named Тимур.");
        return null;
    }

    private static bool IsSpriteTexturePath(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".png"
            || extension == ".jpg"
            || extension == ".jpeg"
            || extension == ".psd";
    }

    private static void EnsureTextureIsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static DialogueManager CreateDialogueSystem(Transform parent)
    {
        DialogueManager existing =
            Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);

        if (existing != null)
        {
            return existing;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueSystemPrefabPath);
        GameObject dialogueObject = null;

        if (prefab != null)
        {
            dialogueObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (dialogueObject != null)
            {
                dialogueObject.name = "DialogueSystem";
                dialogueObject.transform.SetParent(parent, false);
            }
        }

        if (dialogueObject == null)
        {
            Debug.LogWarning("DialogueSystem prefab was not found. Creating a minimal dialogue UI for Book Level 02.");
            dialogueObject = CreateMinimalDialogueSystem(parent);
        }

        return dialogueObject.GetComponentInChildren<DialogueManager>(true);
    }

    private static GameObject CreateMinimalDialogueSystem(Transform parent)
    {
        GameObject dialogueObject = CreateEmpty("DialogueSystem", parent);
        Canvas canvas = CreateCanvas(dialogueObject.transform);
        GameObject dialogRoot = CreateUiObject("DialogRoot", canvas.transform);
        Image panel = dialogRoot.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.72f);

        RectTransform panelRect = dialogRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.18f, 0.05f);
        panelRect.anchorMax = new Vector2(0.82f, 0.28f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI speakerText = CreateUiText("SpeakerNameText", dialogRoot.transform, 34, TextAlignmentOptions.Center);
        RectTransform speakerRect = speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 0.68f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.offsetMin = new Vector2(20f, 0f);
        speakerRect.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI bodyText = CreateUiText("BodyText", dialogRoot.transform, 30, TextAlignmentOptions.Center);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 0.72f);
        bodyRect.offsetMin = new Vector2(42f, 22f);
        bodyRect.offsetMax = new Vector2(-42f, -8f);

        DialogueManager manager = dialogueObject.AddComponent<DialogueManager>();
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("dialogRoot").objectReferenceValue = dialogRoot;
        serialized.FindProperty("speakerNameText").objectReferenceValue = speakerText;
        serialized.FindProperty("bodyText").objectReferenceValue = bodyText;
        serialized.FindProperty("uiFontAsset").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        dialogRoot.SetActive(false);
        return dialogueObject;
    }

    private static void EnsureInteractionPromptUi(DialogueManager dialogueManager)
    {
        InteractionPromptUI existing =
            Object.FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);

        if (existing != null)
        {
            return;
        }

        Canvas canvas = null;

        if (dialogueManager != null)
        {
            canvas = dialogueManager.GetComponentInChildren<Canvas>(true);
        }

        if (canvas == null)
        {
            GameObject uiRoot = CreateEmpty("BookLevel02DialogueCanvas", null);
            canvas = CreateCanvas(uiRoot.transform);
        }

        GameObject promptOwner = CreateUiObject("InteractionPromptUI", canvas.transform);
        InteractionPromptUI prompt = promptOwner.AddComponent<InteractionPromptUI>();

        GameObject root = CreateUiObject("InteractionPrompt", promptOwner.transform);
        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -70f);
        rootRect.sizeDelta = new Vector2(360f, 58f);

        TextMeshProUGUI promptText = CreateUiText("PromptText", root.transform, 28, TextAlignmentOptions.Center);
        promptText.text = InteractionPromptMessage;
        promptText.color = Color.white;
        promptText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);

        RectTransform textRect = promptText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 6f);
        textRect.offsetMax = new Vector2(-18f, -6f);

        prompt.root = root;
        prompt.promptText = promptText;
        prompt.defaultMessage = InteractionPromptMessage;
        prompt.uiFontAsset = promptText.font;
        root.SetActive(false);
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = CreateEmpty("Canvas", parent);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static TextMeshProUGUI CreateUiText(
        string name,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.color = Color.white;
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPath);
        return text;
    }

    private static void CreateEventSystemIfMissing()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
    }

    private static GameObject CreateTimurNpc(
        Transform parent,
        CellData parentCell,
        Transform player,
        DialogueData dialogueData,
        GameObject enableAfterDialogue)
    {
        return CreateTimurNpc(parent, parentCell.gameObject.transform, player, dialogueData, enableAfterDialogue);
    }

    private static GameObject CreateTimurNpc(
        Transform parent,
        Transform parentCell,
        Transform player,
        DialogueData dialogueData,
        GameObject enableAfterDialogue)
    {
        GameObject npc = CreateEmpty("Timur_NPC", parent);
        npc.transform.position = parentCell.position + new Vector3(0f, 0.04f, 0f);

        OrigamiFoldTransformAttachment attachment = npc.AddComponent<OrigamiFoldTransformAttachment>();
        attachment.target = parentCell;
        attachment.targetLocalPosition = new Vector3(0f, 0.04f, 0f);

        SpriteRenderer visual = CreateTimurVisual(npc.transform);
        CircleCollider2D trigger = npc.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.62f;
        trigger.offset = new Vector2(0f, 0.26f);

        NPCInteractable interactable = npc.AddComponent<NPCInteractable>();
        ConfigureNpcInteractable(interactable, dialogueData, player);

        DialogueFadeOutOnCompletion fadeOut = npc.AddComponent<DialogueFadeOutOnCompletion>();
        fadeOut.targetRoot = npc;
        fadeOut.fadeDuration = 0.75f;
        fadeOut.spriteRenderers = visual == null ? new SpriteRenderer[0] : new[] { visual };
        fadeOut.collidersToDisable = new Collider2D[] { trigger };
        fadeOut.behavioursToDisable = new Behaviour[] { interactable };
        fadeOut.enableAfterFade = enableAfterDialogue == null
            ? new GameObject[0]
            : new[] { enableAfterDialogue };

        return npc;
    }

    private static SpriteRenderer CreateTimurVisual(Transform parent)
    {
        Sprite timurSprite = FindTimurSprite();
        GameObject visualObject = CreateEmpty("Visual", parent);
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = timurSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 76;

        if (timurSprite != null)
        {
            float targetHeight = 1.2f;
            float scale = timurSprite.bounds.size.y > 0f
                ? targetHeight / timurSprite.bounds.size.y
                : 1f;
            visualObject.transform.localScale = new Vector3(scale, scale, 1f);
            visualObject.transform.localPosition =
                new Vector3(
                    -timurSprite.bounds.center.x * scale,
                    -timurSprite.bounds.min.y * scale - 0.5f,
                    0f);
        }
        else
        {
            renderer.enabled = false;
            CreateQuad(
                "FallbackVisual",
                parent,
                new Vector3(0f, 0.05f, 0f),
                new Vector3(0.42f, 0.42f, 1f),
                new Color(0.95f, 0.78f, 0.18f, 1f),
                76);
        }

        return renderer;
    }

    private static void ConfigureNpcInteractable(
        NPCInteractable interactable,
        DialogueData dialogueData,
        Transform player)
    {
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("dialogueData").objectReferenceValue = dialogueData;
        serialized.FindProperty("player").objectReferenceValue = player;
        serialized.FindProperty("interactionDistance").floatValue = 1.65f;
        SetEnumPropertyByName(serialized.FindProperty("interactKey"), nameof(KeyCode.E));
        serialized.FindProperty("useGlobalInteractionPrompt").boolValue = true;
        serialized.FindProperty("interactionPromptText").stringValue = InteractionPromptMessage;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnumPropertyByName(SerializedProperty property, string enumName)
    {
        if (property == null)
        {
            return;
        }

        for (int i = 0; i < property.enumNames.Length; i++)
        {
            if (property.enumNames[i] == enumName)
            {
                property.enumValueIndex = i;
                return;
            }
        }
    }

    private static Transform FindOrCreateRoot(string rootName)
    {
        GameObject root = GameObject.Find(rootName);

        if (root != null)
        {
            return root.transform;
        }

        return CreateEmpty(rootName, null).transform;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            return child;
        }

        return CreateEmpty(childName, parent).transform;
    }

    private static Transform FindPlayerTransform()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            return playerObject.transform;
        }

        OrigamiFoldPlayerMover mover =
            Object.FindFirstObjectByType<OrigamiFoldPlayerMover>(FindObjectsInactive.Include);
        return mover == null ? null : mover.transform;
    }

    private static Transform FindMapCellTransform(int x, int y)
    {
        GameObject cell = GameObject.Find($"MapCell_{x}_{y}");
        return cell == null ? null : cell.transform;
    }

    private static void DestroySceneObjectsNamed(string objectName)
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<GameObject> objectsToDestroy = new List<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i] == null)
            {
                continue;
            }

            if (allObjects[i].name == objectName)
            {
                objectsToDestroy.Add(allObjects[i]);
            }
        }

        for (int i = 0; i < objectsToDestroy.Count; i++)
        {
            if (objectsToDestroy[i] != null)
            {
                Object.DestroyImmediate(objectsToDestroy[i]);
            }
        }
    }

    private static Camera CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.1f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(0f, 0.12f, -10f);
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static OrigamiFoldActionCoordinator CreateCoordinator(Transform parent)
    {
        GameObject coordinatorObject = CreateEmpty("OrigamiFoldActionCoordinator", parent);
        return coordinatorObject.AddComponent<OrigamiFoldActionCoordinator>();
    }

    private static CellData[,] CreateMapCells(Transform parent)
    {
        CellData[,] cells = new CellData[MapWidth, MapHeight];

        for (int y = 0; y < MapHeight; y++)
        {
            string row = LayoutTopToBottom[MapHeight - 1 - y];

            for (int x = 0; x < MapWidth; x++)
            {
                char tile = row[x];
                GameObject cellObject = CreateEmpty($"MapCell_{x}_{y}", parent);
                cellObject.transform.localPosition = GridToWorldPosition(x, y);

                OrigamiFoldTransformStack stack =
                    cellObject.AddComponent<OrigamiFoldTransformStack>();
                stack.CaptureBaseTransform();

                CreateQuad(
                    "Visual",
                    cellObject.transform,
                    Vector3.zero,
                    new Vector3(0.92f, 0.92f, 1f),
                    GetCellColor(tile),
                    0);
                if (ShowCellDebugOverlay)
                {
                    CreateCellDebugOverlay(cellObject.transform, x, y, IsWalkableTile(tile));
                }

                cells[x, y] = new CellData
                {
                    gameObject = cellObject,
                    stack = stack,
                    gridPosition = new Vector2Int(x, y),
                    tile = tile
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
                if (!IsWalkableTile(cells[x, y].tile))
                {
                    continue;
                }

                GameObject walkableObject = CreateEmpty("WalkableArea", cells[x, y].gameObject.transform);
                walkableObject.layer = walkableLayer;

                BoxCollider2D collider = walkableObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(1f, 1f);

                OrigamiFoldWalkableArea area = walkableObject.AddComponent<OrigamiFoldWalkableArea>();
                area.ownerStack = cells[x, y].stack;
                area.isWalkable = true;
            }
        }
    }

    private static void CreateCellDebugOverlay(Transform parent, int x, int y, bool isWalkable)
    {
        if (isWalkable)
        {
            CreateQuad(
                "WalkableDebugHighlight",
                parent,
                new Vector3(0f, 0f, -0.045f),
                new Vector3(0.86f, 0.86f, 1f),
                new Color(0.1f, 1f, 0.42f, 0.28f),
                62);
        }

        CreateCellGridLine(
            "Grid_Top",
            parent,
            new Vector3(0f, 0.5f, -0.04f),
            new Vector3(1f, 0.018f, 1f));
        CreateCellGridLine(
            "Grid_Bottom",
            parent,
            new Vector3(0f, -0.5f, -0.04f),
            new Vector3(1f, 0.018f, 1f));
        CreateCellGridLine(
            "Grid_Left",
            parent,
            new Vector3(-0.5f, 0f, -0.04f),
            new Vector3(0.018f, 1f, 1f));
        CreateCellGridLine(
            "Grid_Right",
            parent,
            new Vector3(0.5f, 0f, -0.04f),
            new Vector3(0.018f, 1f, 1f));

        CreateText(
            "CoordinateLabel",
            parent,
            new Vector3(0f, 0f, -0.05f),
            $"{x},{y}",
            new Color(0.02f, 0.02f, 0.03f, 0.72f),
            0.09f,
            86);
    }

    private static void CreateCellGridLine(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale)
    {
        CreateQuad(
            name,
            parent,
            localPosition,
            localScale,
            new Color(1f, 1f, 1f, 0.38f),
            84);
    }

    private static OrigamiFoldStripSqueezeAction CreateColumnFoldAction(
        string name,
        CellData[,] cells,
        Transform parent,
        int columnX,
        OrigamiFoldActionCoordinator coordinator)
    {
        GameObject actionObject = CreateEmpty(name, parent);
        OrigamiFoldStripSqueezeAction action =
            actionObject.AddComponent<OrigamiFoldStripSqueezeAction>();
        action.animationDuration = 0.32f;
        action.coordinator = coordinator;
        action.useCoordinator = true;
        action.targets = CreateColumnTargets(cells, columnX);
        return action;
    }

    private static OrigamiFoldStripSqueezeAction CreateRowFoldAction(
        string name,
        CellData[,] cells,
        Transform parent,
        int rowY,
        OrigamiFoldActionCoordinator coordinator)
    {
        GameObject actionObject = CreateEmpty(name, parent);
        OrigamiFoldStripSqueezeAction action =
            actionObject.AddComponent<OrigamiFoldStripSqueezeAction>();
        action.animationDuration = 0.32f;
        action.coordinator = coordinator;
        action.useCoordinator = true;
        action.targets = CreateRowTargets(cells, rowY);
        return action;
    }

    private static OrigamiStripContributionTarget[] CreateColumnTargets(
        CellData[,] cells,
        int columnX)
    {
        List<OrigamiStripContributionTarget> targets = new List<OrigamiStripContributionTarget>();

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Vector3 offset = Vector3.zero;
                Vector3 scaleMultiplier = Vector3.one;

                if (x < columnX)
                {
                    offset = new Vector3(0.5f, 0f, 0f);
                }
                else if (x == columnX)
                {
                    scaleMultiplier = new Vector3(0.02f, 1f, 1f);
                }
                else
                {
                    offset = new Vector3(-0.5f, 0f, 0f);
                }

                targets.Add(new OrigamiStripContributionTarget
                {
                    stack = cells[x, y].stack,
                    activeLocalPositionOffset = offset,
                    activeLocalScaleMultiplier = scaleMultiplier,
                    overridePassengerCarryOffset = x == columnX && IsWalkableTile(cells[x, y].tile),
                    passengerActiveLocalPositionOffset = GetColumnStripPassengerOffset(cells, x, y)
                });
            }
        }

        return targets.ToArray();
    }

    private static OrigamiStripContributionTarget[] CreateRowTargets(CellData[,] cells, int rowY)
    {
        List<OrigamiStripContributionTarget> targets = new List<OrigamiStripContributionTarget>();

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Vector3 offset = Vector3.zero;
                Vector3 scaleMultiplier = Vector3.one;

                if (y < rowY)
                {
                    offset = new Vector3(0f, 0.5f, 0f);
                }
                else if (y == rowY)
                {
                    scaleMultiplier = new Vector3(1f, 0.02f, 1f);
                }
                else
                {
                    offset = new Vector3(0f, -0.5f, 0f);
                }

                targets.Add(new OrigamiStripContributionTarget
                {
                    stack = cells[x, y].stack,
                    activeLocalPositionOffset = offset,
                    activeLocalScaleMultiplier = scaleMultiplier,
                    overridePassengerCarryOffset = y == rowY && IsWalkableTile(cells[x, y].tile),
                    passengerActiveLocalPositionOffset = GetRowStripPassengerOffset(cells, x, y)
                });
            }
        }

        return targets.ToArray();
    }

    private static Vector3 GetColumnStripPassengerOffset(CellData[,] cells, int x, int y)
    {
        if (IsWalkableCoordinate(cells, x + 1, y))
        {
            return new Vector3(0.5f, 0f, 0f);
        }

        if (IsWalkableCoordinate(cells, x - 1, y))
        {
            return new Vector3(-0.5f, 0f, 0f);
        }

        return Vector3.zero;
    }

    private static Vector3 GetRowStripPassengerOffset(CellData[,] cells, int x, int y)
    {
        if (IsWalkableCoordinate(cells, x, y + 1))
        {
            return new Vector3(0f, 0.5f, 0f);
        }

        if (IsWalkableCoordinate(cells, x, y - 1))
        {
            return new Vector3(0f, -0.5f, 0f);
        }

        return Vector3.zero;
    }

    private static void CreateCenterFoldControls(
        Transform pointsParent,
        Transform linksParent,
        OrigamiFoldStripSqueezeAction action,
        CellData anchorCell)
    {
        OrigamiFoldPoint left = CreateAttachedFoldPoint(
            "Point_CenterLeft",
            pointsParent,
            anchorCell,
            new Vector3(-0.5f, 0f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint right = CreateAttachedFoldPoint(
            "Point_CenterRight",
            pointsParent,
            anchorCell,
            new Vector3(0.5f, 0f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint merged = CreateAttachedMergedStripPoint(
            "Point_CenterMerged",
            pointsParent,
            anchorCell,
            Vector3.zero,
            action);
        merged.gameObject.SetActive(false);

        action.disableAfterActive = new[] { left.gameObject, right.gameObject };
        action.enableAfterActive = new[] { merged.gameObject };
        action.enableAfterInactive = action.disableAfterActive;
        action.disableAfterInactive = action.enableAfterActive;

        CreateStripLink("Center_Link_LTR", linksParent, left, right, action);
        CreateStripLink("Center_Link_RTL", linksParent, right, left, action);
    }

    private static void CreateTriadFoldControls(
        Transform pointsParent,
        Transform linksParent,
        Transform actionsParent,
        OrigamiFoldStripSqueezeAction horizontalAction,
        OrigamiFoldStripSqueezeAction verticalAction,
        OrigamiFoldActionCoordinator coordinator,
        CellData anchorCell)
    {
        OrigamiFoldPoint pointA = CreateAttachedFoldPoint(
            "Point_TriadA",
            pointsParent,
            anchorCell,
            new Vector3(-0.5f, 0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointB = CreateAttachedFoldPoint(
            "Point_TriadB",
            pointsParent,
            anchorCell,
            new Vector3(0.5f, 0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointC = CreateAttachedFoldPoint(
            "Point_TriadC",
            pointsParent,
            anchorCell,
            new Vector3(0.5f, -0.5f, 0f),
            Color.black,
            0.24f);

        OrigamiFoldPoint pointAB = CreateAttachedFoldPoint(
            "Point_TriadAB",
            pointsParent,
            anchorCell,
            new Vector3(0f, 0.5f, 0f),
            new Color(0f, 0.9f, 1f, 1f),
            0.29f);
        OrigamiFoldPoint pointCAfterHorizontal = CreateAttachedFoldPoint(
            "Point_TriadC_AfterColumnFold",
            pointsParent,
            anchorCell,
            new Vector3(0f, -0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointBC = CreateAttachedFoldPoint(
            "Point_TriadBC",
            pointsParent,
            anchorCell,
            new Vector3(0.5f, 0f, 0f),
            new Color(0f, 0.9f, 1f, 1f),
            0.29f);
        OrigamiFoldPoint pointAAfterVertical = CreateAttachedFoldPoint(
            "Point_TriadA_AfterRowFold",
            pointsParent,
            anchorCell,
            new Vector3(-0.5f, 0f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointABC = CreateAttachedFoldPoint(
            "Point_TriadABC_Final",
            pointsParent,
            anchorCell,
            Vector3.zero,
            new Color(0f, 0.95f, 1f, 1f),
            0.34f);

        GameObject groupObject = CreateEmpty("TriadGroup", actionsParent);
        OrigamiFoldTriadGroup group = groupObject.AddComponent<OrigamiFoldTriadGroup>();
        group.state = OrigamiFoldTriadState.Unfolded;
        group.horizontalAction = horizontalAction;
        group.verticalAction = verticalAction;
        group.coordinator = coordinator;
        group.allowSecondFold = true;
        group.visibleWhenUnfolded = new[] { pointA.gameObject, pointB.gameObject, pointC.gameObject };
        group.visibleWhenHorizontalFolded =
            new[] { pointAB.gameObject, pointCAfterHorizontal.gameObject };
        group.visibleWhenVerticalFolded =
            new[] { pointBC.gameObject, pointAAfterVertical.gameObject };
        group.visibleWhenBothFolded = new[] { pointABC.gameObject };
        group.ApplyVisibility();

        AddTriadClick(pointAB, group, OrigamiFoldTriadCommand.ResetHorizontal);
        AddTriadClick(pointBC, group, OrigamiFoldTriadCommand.ResetVertical);
        AddTriadClick(pointABC, group, OrigamiFoldTriadCommand.ResetAll);

        CreateTriadLink(
            "Triad_Link_A_to_B",
            linksParent,
            pointA,
            pointB,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);
        CreateTriadLink(
            "Triad_Link_B_to_A",
            linksParent,
            pointB,
            pointA,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);
        CreateTriadLink(
            "Triad_Link_B_to_C",
            linksParent,
            pointB,
            pointC,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Triad_Link_C_to_B",
            linksParent,
            pointC,
            pointB,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Triad_Link_AB_to_CAfterColumn",
            linksParent,
            pointAB,
            pointCAfterHorizontal,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Triad_Link_CAfterColumn_to_AB",
            linksParent,
            pointCAfterHorizontal,
            pointAB,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Triad_Link_AAfterRow_to_BC",
            linksParent,
            pointAAfterVertical,
            pointBC,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);
        CreateTriadLink(
            "Triad_Link_BC_to_AAfterRow",
            linksParent,
            pointBC,
            pointAAfterVertical,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);
    }

    private static void AddTriadClick(
        OrigamiFoldPoint point,
        OrigamiFoldTriadGroup group,
        OrigamiFoldTriadCommand command)
    {
        OrigamiFoldClickAction click = point.gameObject.AddComponent<OrigamiFoldClickAction>();
        click.targetTriadGroup = group;
        click.triadCommandOnClick = command;
        click.ignoreWhileActionAnimating = true;
        click.debugName = point.name;
    }

    private static void CreateDragController(
        Transform parent,
        Camera targetCamera,
        OrigamiFoldLink[] links)
    {
        GameObject controllerObject = CreateEmpty("OrigamiFoldDragController", parent);
        OrigamiFoldDragController controller =
            controllerObject.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = targetCamera;
        controller.snapDistance = 0.5f;
        controller.autoFindLinks = true;
        controller.links = links;
        controller.lineWidth = 0.045f;
    }

    private static GameObject CreatePlayer(Transform parent, CellData startCell, int walkableMask)
    {
        GameObject player = CreateEmpty("Player", parent);
        player.transform.position = startCell.gameObject.transform.position;
        TrySetTag(player, "Player");

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
        collider.radius = PlayerFootprintRadius;

        CreatePlayerVisual(player.transform);

        OrigamiFoldPlayerMover mover = player.AddComponent<OrigamiFoldPlayerMover>();
        mover.moveSpeed = 3.5f;
        mover.bodyRadius = PlayerFootprintRadius;
        mover.sampleProbeRadius = 0.025f;
        mover.walkableMask = walkableMask;
        mover.requireAllSamplesInsideWalkable = true;
        ConfigurePlayerFootsteps(mover);

        OrigamiFoldPassenger passenger = player.AddComponent<OrigamiFoldPassenger>();
        passenger.walkableMask = walkableMask;
        passenger.probeRadius = PlayerFootprintRadius;
        passenger.currentStack = startCell.stack;
        passenger.disableWhileCarried = new Behaviour[] { mover };
        passenger.resolveToWalkableAfterCarry = true;
        passenger.resolveSearchRadius = 1.25f;
        passenger.resolveSearchStep = 0.08f;
        passenger.resolveDirectionCount = 16;
        passenger.resolveMoveDuration = 0.1f;

        return player;
    }

    private static void ConfigurePlayerFootsteps(OrigamiFoldPlayerMover mover)
    {
        FootstepAudioProfile profile =
            AssetDatabase.LoadAssetAtPath<FootstepAudioProfile>(DefaultFootstepProfilePath);

        SerializedObject serialized = new SerializedObject(mover);
        serialized.FindProperty("defaultFootstepProfile").objectReferenceValue = profile;
        serialized.FindProperty("minFootstepInterval").floatValue = 0.28f;
        serialized.FindProperty("maxFootstepInterval").floatValue = 0.42f;
        serialized.FindProperty("footstepVolume").floatValue = 0.8f;
        serialized.FindProperty("avoidRepeatingFootstepSound").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (profile == null)
        {
            Debug.LogWarning($"Default footstep profile was not found at {DefaultFootstepProfilePath}.");
        }
    }

    private static GameObject CreatePlayerVisual(Transform parent)
    {
        Sprite playerSprite = FindPlayerSprite();

        if (playerSprite == null)
        {
            Debug.LogWarning("Aisulu player sprite was not found. Falling back to placeholder player square.");
            return CreateQuad(
                "Visual",
                parent,
                Vector3.zero,
                new Vector3(0.34f, 0.34f, 1f),
                new Color(1f, 0.68f, 0.22f, 1f),
                80);
        }

        GameObject visual = CreateEmpty("Visual", parent);
        visual.transform.localScale = PlayerVisualLocalScale;
        visual.transform.localPosition = GetFootAnchoredPlayerVisualPosition(playerSprite, PlayerVisualLocalScale.x);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = playerSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 70;

        PaperDollWalkAnimator animator = visual.AddComponent<PaperDollWalkAnimator>();
        ConfigurePaperDollAnimator(animator, visual.transform, renderer);
        return visual;
    }

    private static Vector3 GetFootAnchoredPlayerVisualPosition(Sprite sprite, float visualScale)
    {
        Bounds bounds = sprite.bounds;
        float xOffset = -bounds.center.x * visualScale;
        float yOffset = PlayerVisualFootYOffset - bounds.min.y * visualScale;
        return new Vector3(xOffset, yOffset, 0f);
    }

    private static Sprite FindPlayerSprite()
    {
        string path = AssetDatabase.GUIDToAssetPath(PlayerSpriteGuid);

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite != null)
        {
            return sprite;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        foreach (Object asset in assets)
        {
            if (asset is Sprite nestedSprite)
            {
                return nestedSprite;
            }
        }

        return null;
    }

    private static void ConfigurePaperDollAnimator(
        PaperDollWalkAnimator animator,
        Transform visualRoot,
        SpriteRenderer renderer)
    {
        SerializedObject serialized = new SerializedObject(animator);
        serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
        serialized.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        serialized.FindProperty("idleRockTiltAmplitude").floatValue = 1.5f;
        serialized.FindProperty("idleRockSpeed").floatValue = 3.46f;
        serialized.FindProperty("walkRockTiltAmplitude").floatValue = 7f;
        serialized.FindProperty("walkRockSpeed").floatValue = 7f;
        serialized.FindProperty("walkBobHeight").floatValue = 0.05f;
        serialized.FindProperty("walkSideOffset").floatValue = 0.03f;
        serialized.FindProperty("steppedMotion").boolValue = true;
        serialized.FindProperty("stepiness").floatValue = 1f;
        serialized.FindProperty("stepsPerCycle").intValue = 4;
        serialized.FindProperty("snapSteppedPoses").boolValue = true;
        serialized.FindProperty("returnSmoothness").floatValue = 0f;
        serialized.FindProperty("flipByDirection").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateRespawnPoint(Transform parent, CellData startCell)
    {
        GameObject respawnPoint = CreateEmpty("RespawnPoint", parent);
        respawnPoint.transform.position = startCell.gameObject.transform.position;

        CreateQuad(
            "Visual",
            respawnPoint.transform,
            Vector3.zero,
            new Vector3(0.18f, 0.18f, 1f),
            new Color(1f, 1f, 1f, 0.65f),
            65);

        return respawnPoint;
    }

    private static void CreateNpcPlaceholder(Transform parent, CellData parentCell)
    {
        GameObject npc = CreateEmpty("NPC_Placeholder", parent);
        npc.transform.position = parentCell.gameObject.transform.position + new Vector3(0f, 0.05f, 0f);
        CreateQuad(
            "Visual",
            npc.transform,
            Vector3.zero,
            new Vector3(0.38f, 0.38f, 1f),
            new Color(1f, 0.86f, 0.12f, 1f),
            75);
    }

    private static void CreateNpcZonePlaceholder(Transform parent, CellData parentCell)
    {
        GameObject zone = CreateEmpty("NPC_Zone_Placeholder", parent);
        zone.transform.position = parentCell.gameObject.transform.position + new Vector3(0f, 0.23f, 0f);
    }

    private static GameObject CreateExitPlaceholder(Transform parent, CellData parentCell)
    {
        return CreateExitPlaceholder(parent, parentCell.gameObject.transform);
    }

    private static GameObject CreateExitPlaceholder(Transform parent, Transform parentCell)
    {
        GameObject exit = CreateEmpty("ExitPlaceholder", parent);
        exit.transform.position = parentCell.position + new Vector3(0.25f, -0.22f, 0f);
        CreateQuad(
            "Visual",
            exit.transform,
            Vector3.zero,
            new Vector3(0.24f, 0.24f, 1f),
            new Color(0.2f, 1f, 0.35f, 1f),
            74);

        BoxCollider2D collider = exit.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.65f, 0.65f);

        OrigamiFoldSceneExit sceneExit = exit.AddComponent<OrigamiFoldSceneExit>();
        sceneExit.nextSceneName = NextSceneName;
        sceneExit.loadSceneOnEnter = true;
        sceneExit.visualRoot = exit;
        return exit;
    }

    private static OrigamiFoldPoint CreateFoldPoint(
        string name,
        Transform parent,
        Vector3 position,
        Color color,
        float size)
    {
        GameObject pointObject = CreateEmpty(name, parent);
        pointObject.transform.position = position;

        CircleCollider2D collider = pointObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = FoldNodeColliderRadius;

        Renderer visualRenderer = CreateFoldPointVisual(pointObject);

        OrigamiFoldPoint point = pointObject.AddComponent<OrigamiFoldPoint>();
        point.pointId = name;
        point.visualRenderer = visualRenderer;
        point.normalColor = color;
        point.highlightColor = Color.yellow;
        return point;
    }

    private static Renderer CreateFoldPointVisual(GameObject pointObject)
    {
        OrigamiFoldPointVisual visual = pointObject.AddComponent<OrigamiFoldPointVisual>();
        Sprite nodeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FoldNodeSpritePath);

        SerializedObject serialized = new SerializedObject(visual);
        serialized.FindProperty("nodeSprite").objectReferenceValue = nodeSprite;
        serialized.FindProperty("normalColor").colorValue = Color.white;
        serialized.FindProperty("highlightedColor").colorValue = new Color(1f, 0.92f, 0.25f, 1f);
        serialized.FindProperty("glowColor").colorValue = new Color(0.12f, 0.85f, 1f, 0.32f);
        serialized.FindProperty("highlightedGlowColor").colorValue = new Color(1f, 0.72f, 0.18f, 0.62f);
        serialized.FindProperty("visualSize").floatValue = FoldNodeVisualSize;
        serialized.FindProperty("glowSize").floatValue = FoldNodeGlowSize;
        serialized.FindProperty("sortingOrder").intValue = FoldNodeSortingOrder;
        serialized.FindProperty("hideLegacyRenderer").boolValue = true;
        serialized.FindProperty("pulseSpeed").floatValue = 2.2f;
        serialized.FindProperty("pulseAmount").floatValue = 0.18f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (nodeSprite == null)
        {
            Debug.LogWarning($"Fold node sprite was not found at {FoldNodeSpritePath}.", pointObject);
        }

        visual.EnsureVisuals();
        return visual.MainRenderer;
    }

    private static OrigamiFoldPoint CreateAttachedFoldPoint(
        string name,
        Transform parent,
        CellData anchorCell,
        Vector3 anchorLocalPosition,
        Color color,
        float size)
    {
        Vector3 worldPosition = anchorCell.gameObject.transform.TransformPoint(anchorLocalPosition);
        OrigamiFoldPoint point = CreateFoldPoint(
            name,
            parent,
            worldPosition,
            color,
            size);

        OrigamiFoldTransformAttachment attachment =
            point.gameObject.AddComponent<OrigamiFoldTransformAttachment>();
        attachment.target = anchorCell.gameObject.transform;
        attachment.targetLocalPosition = anchorLocalPosition;
        attachment.SnapToTarget();

        return point;
    }

    private static OrigamiFoldPoint CreateMergedStripPoint(
        string name,
        Transform parent,
        Vector3 position,
        OrigamiFoldStripSqueezeAction action)
    {
        OrigamiFoldPoint point = CreateFoldPoint(
            name,
            parent,
            position,
            new Color(0f, 0.9f, 1f, 1f),
            0.31f);
        OrigamiFoldClickAction clickAction =
            point.gameObject.AddComponent<OrigamiFoldClickAction>();
        clickAction.targetStripSqueezeAction = action;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = name;
        return point;
    }

    private static OrigamiFoldPoint CreateAttachedMergedStripPoint(
        string name,
        Transform parent,
        CellData anchorCell,
        Vector3 anchorLocalPosition,
        OrigamiFoldStripSqueezeAction action)
    {
        OrigamiFoldPoint point = CreateAttachedFoldPoint(
            name,
            parent,
            anchorCell,
            anchorLocalPosition,
            new Color(0f, 0.9f, 1f, 1f),
            0.31f);
        OrigamiFoldClickAction clickAction =
            point.gameObject.AddComponent<OrigamiFoldClickAction>();
        clickAction.targetStripSqueezeAction = action;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = name;
        return point;
    }

    private static OrigamiFoldLink CreateStripLink(
        string name,
        Transform parent,
        OrigamiFoldPoint pointA,
        OrigamiFoldPoint pointB,
        OrigamiFoldStripSqueezeAction action)
    {
        GameObject linkObject = CreateEmpty(name, parent);
        OrigamiFoldLink link = linkObject.AddComponent<OrigamiFoldLink>();
        link.pointA = pointA;
        link.pointB = pointB;
        link.bidirectional = false;
        link.targetStripSqueezeAction = action;
        link.activeStateOnExecute = true;
        return link;
    }

    private static OrigamiFoldLink CreateTriadLink(
        string name,
        Transform parent,
        OrigamiFoldPoint pointA,
        OrigamiFoldPoint pointB,
        OrigamiFoldTriadGroup group,
        OrigamiFoldTriadCommand command)
    {
        GameObject linkObject = CreateEmpty(name, parent);
        OrigamiFoldLink link = linkObject.AddComponent<OrigamiFoldLink>();
        link.pointA = pointA;
        link.pointB = pointB;
        link.bidirectional = false;
        link.targetTriadGroup = group;
        link.triadCommand = command;
        return link;
    }

    private static void CreateGuides(Transform parent)
    {
        GameObject gridRoot = CreateEmpty("GridGuides", parent);

        for (int x = 0; x <= MapWidth; x++)
        {
            float worldX = GetMapLeftX() + x * CellSize;
            CreateLine(
                $"GridLine_X_{x}",
                gridRoot.transform,
                new[]
                {
                    new Vector3(worldX, GetMapBottomY(), 0.02f),
                    new Vector3(worldX, GetMapTopY(), 0.02f)
                },
                new Color(1f, 1f, 1f, 0.14f),
                0.014f,
                35);
        }

        for (int y = 0; y <= MapHeight; y++)
        {
            float worldY = GetMapBottomY() + y * CellSize;
            CreateLine(
                $"GridLine_Y_{y}",
                gridRoot.transform,
                new[]
                {
                    new Vector3(GetMapLeftX(), worldY, 0.02f),
                    new Vector3(GetMapRightX(), worldY, 0.02f)
                },
                new Color(1f, 1f, 1f, 0.14f),
                0.014f,
                35);
        }

        CreateColumnGuide(parent, "CenterColumnFoldGuide_x5", CenterColumnFoldX);
        CreateColumnGuide(parent, "TriadColumnFoldGuide_x7", TriadColumnFoldX);
        CreateRowGuide(parent, "TriadRowFoldGuide_y5", TriadRowFoldY);
    }

    private static void CreateColumnGuide(Transform parent, string name, int columnX)
    {
        float centerX = GridToWorldPosition(columnX, 0).x;
        CreateRectangleGuide(
            name,
            parent,
            centerX - 0.5f,
            centerX + 0.5f,
            GetMapBottomY(),
            GetMapTopY(),
            new Color(1f, 0.84f, 0.12f, 0.95f));
    }

    private static void CreateRowGuide(Transform parent, string name, int rowY)
    {
        float centerY = GridToWorldPosition(0, rowY).y;
        CreateRectangleGuide(
            name,
            parent,
            GetMapLeftX(),
            GetMapRightX(),
            centerY - 0.5f,
            centerY + 0.5f,
            new Color(1f, 0.84f, 0.12f, 0.95f));
    }

    private static void CreateRectangleGuide(
        string name,
        Transform parent,
        float left,
        float right,
        float bottom,
        float top,
        Color color)
    {
        CreateLine(
            name,
            parent,
            new[]
            {
                new Vector3(left, bottom, 0.03f),
                new Vector3(left, top, 0.03f),
                new Vector3(right, top, 0.03f),
                new Vector3(right, bottom, 0.03f),
                new Vector3(left, bottom, 0.03f)
            },
            color,
            0.035f,
            50);
    }

    private static GameObject CreateInstructionText(Transform parent)
    {
        return CreateText(
            "InstructionText",
            parent,
            new Vector3(0f, 3.82f, 0f),
            "WASD move. Drag black points to fold the map.",
            new Color(0.95f, 0.96f, 1f, 1f),
            0.16f,
            100);
    }

    private static GameObject CreateText(
        string name,
        Transform parent,
        Vector3 localPosition,
        string text,
        Color color,
        float characterSize,
        int sortingOrder)
    {
        GameObject textObject = CreateEmpty(name, parent);
        textObject.transform.localPosition = localPosition;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 48;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
        }

        return textObject;
    }

    private static GameObject CreateQuad(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        int sortingOrder)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localScale = localScale;

        Collider collider = quad.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = quad.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(color);
            renderer.sortingOrder = sortingOrder;
        }

        return quad;
    }

    private static LineRenderer CreateLine(
        string name,
        Transform parent,
        Vector3[] points,
        Color color,
        float width,
        int sortingOrder)
    {
        GameObject lineObject = CreateEmpty(name, parent);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = sortingOrder;
        line.material = CreateMaterial(color);
        return line;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
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

    private static bool IsWalkableTile(char tile)
    {
        return tile == 'G'
            || tile == 'B'
            || tile == 'P'
            || tile == 'S'
            || tile == 'N';
    }

    private static bool IsWalkableCoordinate(CellData[,] cells, int x, int y)
    {
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
        {
            return false;
        }

        return cells[x, y] != null && IsWalkableTile(cells[x, y].tile);
    }

    private static Color GetCellColor(char tile)
    {
        switch (tile)
        {
            case 'G':
            case 'S':
                return new Color(0.22f, 0.58f, 0.34f, 1f);
            case 'B':
                return new Color(0.48f, 0.25f, 0.11f, 1f);
            case 'P':
                return new Color(0.55f, 0.25f, 0.72f, 1f);
            case 'N':
                return new Color(0.68f, 0.34f, 0.78f, 1f);
            case 'F':
                return new Color(1f, 0.82f, 0.08f, 0.46f);
            default:
                return new Color(0.18f, 0.19f, 0.21f, 1f);
        }
    }

    private static Vector3 GridToWorldPosition(float x, float y)
    {
        return new Vector3(
            (x - (MapWidth - 1) * 0.5f) * CellSize,
            (y - (MapHeight - 1) * 0.5f) * CellSize,
            0f);
    }

    private static float GetMapLeftX()
    {
        return GridToWorldPosition(0, 0).x - CellSize * 0.5f;
    }

    private static float GetMapRightX()
    {
        return GridToWorldPosition(MapWidth - 1, 0).x + CellSize * 0.5f;
    }

    private static float GetMapBottomY()
    {
        return GridToWorldPosition(0, 0).y - CellSize * 0.5f;
    }

    private static float GetMapTopY()
    {
        return GridToWorldPosition(0, MapHeight - 1).y + CellSize * 0.5f;
    }

    private static int ResolveWalkableLayer()
    {
        int walkableLayer = LayerMask.NameToLayer("Walkable");

        if (walkableLayer >= 0)
        {
            return walkableLayer;
        }

        Debug.LogWarning("Walkable layer was not found. Book Level 02 uses Default layer for walkable areas.");
        return 0;
    }

    private static void TrySetTag(GameObject gameObject, string tag)
    {
        try
        {
            gameObject.tag = tag;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{tag} tag was not found. Player checks will use component fallback.");
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
