using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldBookLevel02Builder
{
    private const string LevelScenePath = "Assets/Scenes/Book_Level_02_Greybox.unity";
    private const int MapWidth = 10;
    private const int MapHeight = 7;
    private const float CellSize = 1f;
    private const int CenterColumnFoldX = 4;
    private const int TriadColumnFoldX = 6;
    private const int TriadRowFoldY = 4;

    private static readonly string[] LayoutTopToBottom =
    {
        "....F.GBG.",
        "...GFGGNG.",
        "FGGGFGGFFF",
        ".GGG..FB.P",
        "GGGG..FBGG",
        ".GG.GGFGGF",
        "...SGG...."
    };

    private class CellData
    {
        public GameObject gameObject;
        public OrigamiFoldTransformStack stack;
        public Vector2Int gridPosition;
        public char tile;
    }

    [MenuItem("Tools/PANINI/Origami Fold/Create Book Level 02 Greybox")]
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

        GameObject levelRoot = CreateEmpty("LEVEL_ROOT", null);
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
            "CenterColumnFold_x4",
            cells,
            actionsRoot.transform,
            CenterColumnFoldX,
            coordinator);
        OrigamiFoldStripSqueezeAction triadColumnAction = CreateColumnFoldAction(
            "TriadColumnFold_x6",
            cells,
            actionsRoot.transform,
            TriadColumnFoldX,
            coordinator);
        OrigamiFoldStripSqueezeAction triadRowAction = CreateRowFoldAction(
            "TriadRowFold_y4",
            cells,
            actionsRoot.transform,
            TriadRowFoldY,
            coordinator);

        CreateCenterFoldControls(
            pointsRoot.transform,
            linksRoot.transform,
            centerColumnAction,
            cells[CenterColumnFoldX, 2]);
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
        CreatePlayer(playerRoot.transform, cells[0, 2], walkableMask);
        CreateRespawnPoint(playerRoot.transform, cells[0, 2]);
        CreateNpcPlaceholder(npcsRoot.transform, cells[7, 5]);
        CreateNpcZonePlaceholder(npcsRoot.transform, cells[9, 3]);
        CreateExitPlaceholder(npcsRoot.transform, cells[9, 3]);
        CreateGuides(debugRoot.transform);
        CreateInstructionText(debugRoot.transform);

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

                CreateText(
                    $"Label_{x}_{y}",
                    cellObject.transform,
                    new Vector3(0f, -0.34f, -0.01f),
                    $"{x},{y}",
                    new Color(0.85f, 0.88f, 0.9f, 0.72f),
                    0.075f,
                    20);

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
        collider.radius = 0.18f;

        CreateQuad(
            "Visual",
            player.transform,
            Vector3.zero,
            new Vector3(0.34f, 0.34f, 1f),
            new Color(1f, 0.68f, 0.22f, 1f),
            80);

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
        passenger.resolveToWalkableAfterCarry = true;
        passenger.resolveSearchRadius = 1.25f;
        passenger.resolveSearchStep = 0.08f;
        passenger.resolveDirectionCount = 16;
        passenger.resolveMoveDuration = 0.1f;

        return player;
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
        CreateText(
            "Label",
            zone.transform,
            Vector3.zero,
            "NPC",
            new Color(0.95f, 0.75f, 1f, 1f),
            0.16f,
            76);
    }

    private static void CreateExitPlaceholder(Transform parent, CellData parentCell)
    {
        GameObject exit = CreateEmpty("ExitPlaceholder", parent);
        exit.transform.position = parentCell.gameObject.transform.position + new Vector3(0.25f, -0.22f, 0f);
        CreateQuad(
            "Visual",
            exit.transform,
            Vector3.zero,
            new Vector3(0.24f, 0.24f, 1f),
            new Color(0.2f, 1f, 0.35f, 1f),
            74);
    }

    private static OrigamiFoldPoint CreateFoldPoint(
        string name,
        Transform parent,
        Vector3 position,
        Color color,
        float size)
    {
        GameObject pointObject = CreateQuad(
            name,
            parent,
            position,
            new Vector3(size, size, 1f),
            color,
            90);

        CircleCollider2D collider = pointObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.58f;

        OrigamiFoldPoint point = pointObject.AddComponent<OrigamiFoldPoint>();
        point.pointId = name;
        point.visualRenderer = pointObject.GetComponent<Renderer>();
        point.normalColor = color;
        point.highlightColor = Color.yellow;
        return point;
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

        CreateColumnGuide(parent, "CenterColumnFoldGuide_x4", CenterColumnFoldX);
        CreateColumnGuide(parent, "TriadColumnFoldGuide_x6", TriadColumnFoldX);
        CreateRowGuide(parent, "TriadRowFoldGuide_y4", TriadRowFoldY);
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
