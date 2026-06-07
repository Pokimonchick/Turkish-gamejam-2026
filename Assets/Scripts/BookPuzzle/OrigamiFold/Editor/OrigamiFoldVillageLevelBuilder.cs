using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldVillageLevelBuilder
{
    private const string VillageScenePath = "Assets/Scenes/Village_Level_01_Greybox.unity";
    private const string StubScenePath = "Assets/Scenes/Village_Level_02_Stub.unity";
    private const string StubSceneName = "Village_Level_02_Stub";
    private const int MapWidth = 11;
    private const int VisibleVillageWidth = 10;
    private const int MapHeight = 7;
    private const int WallColumnX = 9;
    private const int ExitBufferX = 10;
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

        CreateVillageScene();
        CreateStubScene();
        AddScenesToBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created village greybox scenes: {VillageScenePath}, {StubScenePath}");
    }

    private static void CreateVillageScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Village_Level_01_Greybox";

        Camera camera = CreateCamera("Main Camera", new Vector3(0f, 0.25f, -10f), 5.15f);

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

        GameObject coordinatorObject = CreateEmpty("OrigamiFoldActionCoordinator", foldSystemRoot.transform);
        OrigamiFoldActionCoordinator coordinator =
            coordinatorObject.AddComponent<OrigamiFoldActionCoordinator>();

        CellData[,] cells = CreateCells(cellsRoot.transform);
        int walkableLayer = ResolveWalkableLayer();
        LayerMask walkableMask = new LayerMask
        {
            value = 1 << walkableLayer
        };
        CreateWalkableAreas(cells, walkableLayer);

        CreateNpcPlaceholder("NPC_3_5", npcsRoot.transform, CellToWorld(3, 5));
        CreateNpcPlaceholder("NPC_8_2", npcsRoot.transform, CellToWorld(8, 2));

        OrigamiFoldTransformStack exitStack;
        CreateVillageExit(exitRoot.transform, out exitStack);

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
            exitStack,
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

        CreatePlayer(playerRoot.transform, cells[1, 0], walkableMask);
        CreateInstructionText(debugRoot.transform);

        Selection.activeGameObject = levelRoot;
        EditorSceneManager.SaveScene(scene, VillageScenePath);
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

    private static void CreateNpcPlaceholder(string name, Transform parent, Vector3 position)
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
    }

    private static void CreateVillageExit(
        Transform parent,
        out OrigamiFoldTransformStack exitStack)
    {
        GameObject exit = CreateEmpty("NextLevelExit", parent);
        exit.transform.position = CellToWorld(ExitBufferX, 2);

        exitStack = exit.AddComponent<OrigamiFoldTransformStack>();
        exitStack.CaptureBaseTransform();

        GameObject visual = CreateSpriteVisual(
            "Visual",
            exit.transform,
            Vector3.zero,
            new Vector2(0.54f, 0.54f),
            new Color(0.1f, 1f, 0.25f),
            35,
            false);

        BoxCollider2D collider = exit.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.62f, 0.62f);

        OrigamiFoldSceneExit sceneExit = exit.AddComponent<OrigamiFoldSceneExit>();
        sceneExit.nextSceneName = StubSceneName;
        sceneExit.loadSceneOnEnter = true;
        sceneExit.visualRoot = visual;
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
        OrigamiFoldTransformStack exitStack,
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
        action.targets = CreateWallTargets(cells, exitStack);
        action.enableAfterActive = new[] { mergedPoint.gameObject };
        action.disableAfterActive = new[] { leftPoint.gameObject, rightPoint.gameObject };
        action.enableAfterInactive = action.disableAfterActive;
        action.disableAfterInactive = action.enableAfterActive;
        return action;
    }

    private static OrigamiStripContributionTarget[] CreateWallTargets(
        CellData[,] cells,
        OrigamiFoldTransformStack exitStack)
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

        targets.Add(new OrigamiStripContributionTarget
        {
            stack = exitStack,
            activeLocalPositionOffset = GetWallFoldOffset(ExitBufferX),
            activeLocalScaleMultiplier = Vector3.one
        });

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

    private static void CreatePlayer(
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

        if (y == 6 && (x == 1 || x == 2 || x == 3 || x == 6 || x == 7 || x == 8))
        {
            return VillageCellKind.House;
        }

        if (y == 6 && (x == 4 || x == 5))
        {
            return VillageCellKind.Door;
        }

        if ((x == 4 || x == 5) && (y == 2 || y == 3))
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
            return y == 2 || y == 3;
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
        return new Vector3((x - 5) * CellSize, (y - 3) * CellSize, 0f);
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
}
