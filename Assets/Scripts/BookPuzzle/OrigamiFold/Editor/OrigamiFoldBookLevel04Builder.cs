using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldBookLevel04Builder
{
    private const string LevelScenePath = "Assets/Scenes/Book_Level_04_Greybox.unity";
    private const string FinalCutsceneScenePath = "Assets/Scenes/FinalCutscene.unity";
    private const string FinalCutsceneSceneName = "FinalCutscene";
    private const string FoldNodeSpritePath = "Assets/Art/UI/Node.PNG";
    private const string CrowSpritePath = "Assets/Art/crow.PNG";
    private const int MapWidth = 12;
    private const int MapHeight = 9;
    private const float CellSize = 1f;
    private const int MiddleColumnFoldX3 = 4;
    private const int MiddleColumnFoldX4 = 5;
    private const int RightRowFoldY = 3;
    private const int RightColumnFoldX = 8;
    private const string PlayerSpriteGuid = "77d3b28359b42e440905b56447f58511";
    private const float FoldNodeVisualSize = 0.55f;
    private const float FoldNodeGlowSize = 0.9f;
    private const float FoldNodeColliderRadius = 0.5f;
    private const int FoldNodeSortingOrder = 95;
    private const float CrowEnemyVisualSize = 0.58f;
    private const int CrowEnemySortingOrder = 82;
    private const float CrowEnemyRockTiltAmplitude = 3.2f;
    private const float CrowEnemyRockSpeed = 3.4f;
    private static readonly Vector3 CellContentLocalOffset = Vector3.zero;
    private const float PlayerVisualScale = 0.14f;
    private const float PlayerVisualFootYOffset = 0.02f;

    private static readonly string[] LayoutTopToBottom =
    {
        "............",
        ".....GGGG...",
        "..GGG...G...",
        "..G.G.GGG...",
        "..GGG..G....",
        "...GGG...G..",
        ".....G.G.G..",
        ".GGGGG...S..",
        "............"
    };

    private class CellData
    {
        public GameObject gameObject;
        public OrigamiFoldTransformStack stack;
        public Vector2Int gridPosition;
        public char tile;
    }

    [MenuItem("Tools/PANINI/Origami Fold/Create Book Level 04 Greybox")]
    public static void CreateBookLevel04Greybox()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot rebuild Book Level 04 while Unity is in Play Mode.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Book_Level_04_Greybox";

        Camera mainCamera = CreateMainCamera();

        GameObject levelRoot = CreateEmpty("LEVEL_ROOT", null);
        GameObject foldSystemRoot = CreateEmpty("ORIGAMI_FOLD_SYSTEM", levelRoot.transform);
        GameObject mapRoot = CreateEmpty("BOOK_LEVEL_MAP", levelRoot.transform);
        GameObject cellsRoot = CreateEmpty("Cells", mapRoot.transform);
        GameObject pointsRoot = CreateEmpty("ORIGAMI_FOLD_POINTS", levelRoot.transform);
        GameObject linksRoot = CreateEmpty("ORIGAMI_FOLD_LINKS", levelRoot.transform);
        GameObject actionsRoot = CreateEmpty("ORIGAMI_ACTIONS", levelRoot.transform);
        GameObject playerRoot = CreateEmpty("BOOK_LEVEL_PLAYER", levelRoot.transform);
        GameObject enemiesRoot = CreateEmpty("BOOK_LEVEL_ENEMIES", levelRoot.transform);
        GameObject goalRoot = CreateEmpty("BOOK_LEVEL_GOAL", levelRoot.transform);
        GameObject debugRoot = CreateEmpty("BOOK_LEVEL_DEBUG", levelRoot.transform);

        OrigamiFoldActionCoordinator coordinator = CreateCoordinator(foldSystemRoot.transform);
        CellData[,] cells = CreateMapCells(cellsRoot.transform);

        int walkableLayer = ResolveWalkableLayer();
        int walkableMask = 1 << walkableLayer;
        CreateWalkableAreas(cells, walkableLayer);

        OrigamiFoldStripSqueezeAction middleColumnActionX3 = CreateColumnFoldAction(
            "MiddleColumnFold_x4",
            cells,
            actionsRoot.transform,
            MiddleColumnFoldX3,
            coordinator);
        OrigamiFoldStripSqueezeAction middleColumnActionX4 = CreateColumnFoldAction(
            "MiddleColumnFold_x5",
            cells,
            actionsRoot.transform,
            MiddleColumnFoldX4,
            coordinator);
        OrigamiFoldStripSqueezeAction rightRowAction = CreateRowFoldAction(
            "RightRowFold_y3",
            cells,
            actionsRoot.transform,
            RightRowFoldY,
            coordinator);
        OrigamiFoldStripSqueezeAction rightColumnAction = CreateColumnFoldAction(
            "RightColumnFold_x8",
            cells,
            actionsRoot.transform,
            RightColumnFoldX,
            coordinator);

        CreateMiddleChainFoldControls(
            pointsRoot.transform,
            linksRoot.transform,
            middleColumnActionX3,
            middleColumnActionX4,
            cells[4, 3],
            cells[5, 3]);
        OrigamiFoldTriadGroup rightTriadGroup = CreateRightTriadFoldControls(
            pointsRoot.transform,
            linksRoot.transform,
            actionsRoot.transform,
            rightColumnAction,
            rightRowAction,
            coordinator,
            cells[8, 3]);

        OrigamiFoldLink[] links = linksRoot.GetComponentsInChildren<OrigamiFoldLink>(true);
        CreateDragController(foldSystemRoot.transform, mainCamera, links);

        GameObject player = CreatePlayer(playerRoot.transform, cells[9, 1], walkableMask);
        GameObject respawnPoint = CreateRespawnPoint(playerRoot.transform, cells[9, 1]);
        OrigamiFoldMapResetter resetter = CreateMapResetter(
            goalRoot.transform,
            coordinator,
            middleColumnActionX3,
            middleColumnActionX4,
            rightRowAction,
            rightColumnAction,
            rightTriadGroup);
        OrigamiFoldPatrolMover[] patrols = CreateSkyEnemies(
            enemiesRoot.transform,
            cells,
            walkableMask);
        ConfigureSkyEnemyTraps(
            middleColumnActionX3,
            middleColumnActionX4,
            rightRowAction,
            rightColumnAction,
            patrols);
        OrigamiFoldPuzzleState puzzleState = CreatePuzzleState(
            goalRoot.transform,
            player,
            respawnPoint,
            resetter,
            patrols);
        AssignHazards(enemiesRoot.transform, puzzleState);
        CreateFinalCutsceneTrigger(goalRoot.transform, cells[1, 1]);
        AddSceneToBuildSettings(LevelScenePath);
        AddSceneToBuildSettings(FinalCutsceneScenePath);
        Selection.activeGameObject = levelRoot;
        EditorGUIUtility.PingObject(levelRoot);

        EditorSceneManager.SaveScene(scene, LevelScenePath);
        OrigamiFoldTileGridArtApplier.ApplyTileGridArtToActiveLevel();
        OrigamiFoldTileGridArtApplier.FitActiveLevelCameraToFullTileMap();
        EditorSceneManager.SaveScene(scene, LevelScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Created {LevelScenePath}. "
            + "Fold links: MiddleLeft<->MiddleCenter, MiddleCenter<->MiddleRight, "
            + "RightTop<->RightCorner, RightCorner<->RightRight. "
            + "No MiddleLeft<->MiddleRight or RightTop<->RightRight diagonal/skip link was created.");
    }

    [MenuItem("Tools/PANINI/Art/Apply Book Level 04 Crow Enemy Visuals")]
    [MenuItem("Tools/PANINI/Origami Fold/Apply Book Level 04 Crow Enemy Visuals")]
    public static void ApplyBookLevel04CrowEnemyVisuals()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot apply Book Level 04 crow visuals while Unity is in Play Mode.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
        {
            Debug.LogWarning("No active scene found for Book Level 04 crow visual update.");
            return;
        }

        Sprite crowSprite = FindCrowSprite();

        if (crowSprite == null)
        {
            Debug.LogWarning($"Crow sprite was not found or could not be imported as a Sprite: {CrowSpritePath}");
            return;
        }

        string[] enemyNames =
        {
            "TopSkyEnemy",
            "MiddleSkyEnemy",
            "LowerSkyEnemy"
        };

        int updatedCount = 0;

        for (int i = 0; i < enemyNames.Length; i++)
        {
            GameObject enemy = FindGameObjectInScene(scene, enemyNames[i]);

            if (enemy == null)
            {
                Debug.LogWarning($"Book Level 04 enemy was not found in active scene: {enemyNames[i]}");
                continue;
            }

            ConfigureCrowEnemyVisual(enemy, crowSprite);
            updatedCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"Book Level 04 crow visuals applied. Scene={scene.path}, updatedEnemies={updatedCount}, sprite={CrowSpritePath}");
    }

    private static Camera CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.95f;
        camera.backgroundColor = new Color(0.075f, 0.105f, 0.16f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(0f, 0.08f, -10f);
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
                walkableObject.transform.localPosition = CellContentLocalOffset;

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

    private static void CreateMiddleChainFoldControls(
        Transform pointsParent,
        Transform linksParent,
        OrigamiFoldStripSqueezeAction actionX3,
        OrigamiFoldStripSqueezeAction actionX4,
        CellData columnThreeAnchor,
        CellData columnFourAnchor)
    {
        OrigamiFoldPoint left = CreateAttachedFoldPoint(
            "Middle_Point_Left",
            pointsParent,
            columnThreeAnchor,
            new Vector3(-0.5f, 0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint center = CreateAttachedFoldPoint(
            "Middle_Point_Center",
            pointsParent,
            columnThreeAnchor,
            new Vector3(0.5f, 0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint right = CreateAttachedFoldPoint(
            "Middle_Point_Right",
            pointsParent,
            columnFourAnchor,
            new Vector3(0.5f, 0.5f, 0f),
            Color.black,
            0.24f);

        OrigamiFoldPoint mergedX3 = CreateAttachedMergedStripPoint(
            "Middle_Point_Merged_x3",
            pointsParent,
            columnThreeAnchor,
            new Vector3(0f, 0.5f, 0f),
            actionX3);
        OrigamiFoldPoint mergedX4 = CreateAttachedMergedStripPoint(
            "Middle_Point_Merged_x4",
            pointsParent,
            columnFourAnchor,
            new Vector3(0f, 0.5f, 0f),
            actionX4);
        mergedX3.gameObject.SetActive(false);
        mergedX4.gameObject.SetActive(false);

        actionX3.disableAfterActive = new[] { left.gameObject, center.gameObject };
        actionX3.enableAfterActive = new[] { mergedX3.gameObject };
        actionX3.enableAfterInactive = actionX3.disableAfterActive;
        actionX3.disableAfterInactive = actionX3.enableAfterActive;

        actionX4.disableAfterActive = new[] { center.gameObject, right.gameObject };
        actionX4.enableAfterActive = new[] { mergedX4.gameObject };
        actionX4.enableAfterInactive = actionX4.disableAfterActive;
        actionX4.disableAfterInactive = actionX4.enableAfterActive;

        CreateStripLink("Middle_Link_Left_to_Center", linksParent, left, center, actionX3);
        CreateStripLink("Middle_Link_Center_to_Left", linksParent, center, left, actionX3);
        CreateStripLink("Middle_Link_Center_to_Right", linksParent, center, right, actionX4);
        CreateStripLink("Middle_Link_Right_to_Center", linksParent, right, center, actionX4);
    }

    private static OrigamiFoldTriadGroup CreateRightTriadFoldControls(
        Transform pointsParent,
        Transform linksParent,
        Transform actionsParent,
        OrigamiFoldStripSqueezeAction horizontalAction,
        OrigamiFoldStripSqueezeAction verticalAction,
        OrigamiFoldActionCoordinator coordinator,
        CellData anchorCell)
    {
        OrigamiFoldPoint pointTop = CreateAttachedFoldPoint(
            "Right_Point_Top",
            pointsParent,
            anchorCell,
            new Vector3(-0.5f, 0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointCorner = CreateAttachedFoldPoint(
            "Right_Point_Corner",
            pointsParent,
            anchorCell,
            new Vector3(-0.5f, -0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointRight = CreateAttachedFoldPoint(
            "Right_Point_Right",
            pointsParent,
            anchorCell,
            new Vector3(0.5f, -0.5f, 0f),
            Color.black,
            0.24f);

        OrigamiFoldPoint pointCornerRight = CreateAttachedFoldPoint(
            "Right_Point_CornerRight",
            pointsParent,
            anchorCell,
            new Vector3(0f, -0.5f, 0f),
            new Color(0f, 0.9f, 1f, 1f),
            0.29f);
        OrigamiFoldPoint pointTopAfterColumn = CreateAttachedFoldPoint(
            "Right_Point_Top_AfterColumnFold",
            pointsParent,
            anchorCell,
            new Vector3(0f, 0.5f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointTopCorner = CreateAttachedFoldPoint(
            "Right_Point_TopCorner",
            pointsParent,
            anchorCell,
            new Vector3(-0.5f, 0f, 0f),
            new Color(0f, 0.9f, 1f, 1f),
            0.29f);
        OrigamiFoldPoint pointRightAfterRow = CreateAttachedFoldPoint(
            "Right_Point_Right_AfterRowFold",
            pointsParent,
            anchorCell,
            new Vector3(0.5f, 0f, 0f),
            Color.black,
            0.24f);
        OrigamiFoldPoint pointFinal = CreateAttachedFoldPoint(
            "Right_Point_Final",
            pointsParent,
            anchorCell,
            Vector3.zero,
            new Color(0f, 0.95f, 1f, 1f),
            0.34f);

        GameObject groupObject = CreateEmpty("RightTriadGroup", actionsParent);
        OrigamiFoldTriadGroup group = groupObject.AddComponent<OrigamiFoldTriadGroup>();
        group.state = OrigamiFoldTriadState.Unfolded;
        group.horizontalAction = horizontalAction;
        group.verticalAction = verticalAction;
        group.coordinator = coordinator;
        group.allowSecondFold = true;
        group.visibleWhenUnfolded =
            new[] { pointTop.gameObject, pointCorner.gameObject, pointRight.gameObject };
        group.visibleWhenHorizontalFolded =
            new[] { pointCornerRight.gameObject, pointTopAfterColumn.gameObject };
        group.visibleWhenVerticalFolded =
            new[] { pointTopCorner.gameObject, pointRightAfterRow.gameObject };
        group.visibleWhenBothFolded = new[] { pointFinal.gameObject };
        group.ApplyVisibility();

        AddTriadClick(pointCornerRight, group, OrigamiFoldTriadCommand.ResetHorizontal);
        AddTriadClick(pointTopCorner, group, OrigamiFoldTriadCommand.ResetVertical);
        AddTriadClick(pointFinal, group, OrigamiFoldTriadCommand.ResetAll);

        CreateTriadLink(
            "Right_Link_Top_to_Corner",
            linksParent,
            pointTop,
            pointCorner,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Right_Link_Corner_to_Top",
            linksParent,
            pointCorner,
            pointTop,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Right_Link_Corner_to_Right",
            linksParent,
            pointCorner,
            pointRight,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);
        CreateTriadLink(
            "Right_Link_Right_to_Corner",
            linksParent,
            pointRight,
            pointCorner,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);
        CreateTriadLink(
            "Right_Link_CornerRight_to_TopAfterColumn",
            linksParent,
            pointCornerRight,
            pointTopAfterColumn,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Right_Link_TopAfterColumn_to_CornerRight",
            linksParent,
            pointTopAfterColumn,
            pointCornerRight,
            group,
            OrigamiFoldTriadCommand.FoldVertical);
        CreateTriadLink(
            "Right_Link_TopCorner_to_RightAfterRow",
            linksParent,
            pointTopCorner,
            pointRightAfterRow,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);
        CreateTriadLink(
            "Right_Link_RightAfterRow_to_TopCorner",
            linksParent,
            pointRightAfterRow,
            pointTopCorner,
            group,
            OrigamiFoldTriadCommand.FoldHorizontal);

        return group;
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
        player.transform.position = startCell.gameObject.transform.TransformPoint(CellContentLocalOffset);
        TrySetTag(player, "Player");

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
        collider.radius = 0.12f;
        collider.offset = Vector2.zero;

        CreatePlayerVisual(player.transform);

        OrigamiFoldPlayerMover mover = player.AddComponent<OrigamiFoldPlayerMover>();
        mover.moveSpeed = 3.5f;
        mover.bodyRadius = 0.12f;
        mover.sampleProbeRadius = 0.025f;
        mover.walkableMask = walkableMask;
        mover.requireAllSamplesInsideWalkable = true;

        OrigamiFoldPassenger passenger = player.AddComponent<OrigamiFoldPassenger>();
        passenger.walkableMask = walkableMask;
        passenger.probeRadius = 0.12f;
        passenger.currentStack = startCell.stack;
        passenger.disableWhileCarried = new Behaviour[] { mover };
        passenger.resolveToWalkableAfterCarry = true;
        passenger.resolveSearchRadius = 1.25f;
        passenger.resolveSearchStep = 0.08f;
        passenger.resolveDirectionCount = 16;
        passenger.resolveMoveDuration = 0.1f;

        return player;
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
        visual.transform.localScale = new Vector3(PlayerVisualScale, PlayerVisualScale, 1f);
        visual.transform.localPosition = GetFootAnchoredPlayerVisualPosition(playerSprite, PlayerVisualScale);

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

    private static Sprite FindCrowSprite()
    {
        EnsureTextureImportedAsSprite(CrowSpritePath);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrowSpritePath);

        if (sprite != null)
        {
            return sprite;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CrowSpritePath);

        foreach (Object asset in assets)
        {
            if (asset is Sprite nestedSprite)
            {
                return nestedSprite;
            }
        }

        return null;
    }

    private static void EnsureTextureImportedAsSprite(string path)
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

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
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
        respawnPoint.transform.position = startCell.gameObject.transform.TransformPoint(CellContentLocalOffset);

        CreateQuad(
            "Visual",
            respawnPoint.transform,
            Vector3.zero,
            new Vector3(0.18f, 0.18f, 1f),
            new Color(1f, 1f, 1f, 0.65f),
            65);

        return respawnPoint;
    }

    private static OrigamiFoldMapResetter CreateMapResetter(
        Transform parent,
        OrigamiFoldActionCoordinator coordinator,
        OrigamiFoldStripSqueezeAction middleColumnActionX3,
        OrigamiFoldStripSqueezeAction middleColumnActionX4,
        OrigamiFoldStripSqueezeAction rightRowAction,
        OrigamiFoldStripSqueezeAction rightColumnAction,
        OrigamiFoldTriadGroup rightTriadGroup)
    {
        GameObject resetterObject = CreateEmpty("MapResetter", parent);
        OrigamiFoldMapResetter resetter =
            resetterObject.AddComponent<OrigamiFoldMapResetter>();
        resetter.stripActions = new[]
        {
            middleColumnActionX3,
            middleColumnActionX4,
            rightRowAction,
            rightColumnAction
        };
        resetter.autoFindActions = false;
        resetter.triadGroups = new[] { rightTriadGroup };
        resetter.autoFindTriadGroups = false;
        resetter.coordinator = coordinator;
        resetter.resetTimeoutSeconds = 5f;
        return resetter;
    }

    private static OrigamiFoldPuzzleState CreatePuzzleState(
        Transform parent,
        GameObject player,
        GameObject respawnPoint,
        OrigamiFoldMapResetter resetter,
        OrigamiFoldPatrolMover[] patrols)
    {
        GameObject stateObject = CreateEmpty("PuzzleState", parent);
        OrigamiFoldPuzzleState state = stateObject.AddComponent<OrigamiFoldPuzzleState>();
        state.player = player.transform;
        state.respawnPoint = respawnPoint.transform;
        state.mapResetter = resetter;
        state.resetFoldsOnRespawn = true;
        state.resetProgressOnRespawn = false;
        state.resetPatrolsOnRespawn = true;
        state.autoFindResetObjects = false;
        state.patrols = patrols;
        state.disableWhileRespawning = new Behaviour[]
        {
            player.GetComponent<OrigamiFoldPlayerMover>()
        };
        return state;
    }

    private static OrigamiFoldPatrolMover[] CreateSkyEnemies(
        Transform parent,
        CellData[,] cells,
        int walkableMask)
    {
        return new[]
        {
            CreatePatrolEnemy(
                "TopSkyEnemy",
                parent,
                cells[7, 7],
                Vector3.zero,
                GridToWorldPosition(5.18f, 7f),
                GridToWorldPosition(8.32f, 7f),
                1.15f,
                walkableMask),
            CreatePatrolEnemy(
                "MiddleSkyEnemy",
                parent,
                cells[7, 5],
                new Vector3(0f, 0f, 0f),
                GridToWorldPosition(6.18f, 5f),
                GridToWorldPosition(8.32f, 5f),
                1.1f,
                walkableMask),
            CreatePatrolEnemy(
                "LowerSkyEnemy",
                parent,
                cells[4, 3],
                Vector3.zero,
                GridToWorldPosition(3.18f, 3f),
                GridToWorldPosition(5.32f, 3f),
                1.15f,
                walkableMask)
        };
    }

    private static OrigamiFoldPatrolMover CreatePatrolEnemy(
        string name,
        Transform parent,
        CellData parentCell,
        Vector3 localStartOffset,
        Vector3 leftWorldPosition,
        Vector3 rightWorldPosition,
        float speed,
        int walkableMask)
    {
        GameObject enemy = CreateEmpty(name, parentCell.gameObject.transform);
        enemy.transform.localPosition = localStartOffset;

        GameObject activeVisual = CreateCrowEnemyVisual(enemy.transform);
        ConfigureCrowVisualAnimator(enemy, activeVisual);

        CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.105f;

        OrigamiFoldHazard hazard = enemy.AddComponent<OrigamiFoldHazard>();
        hazard.respawnOnTouch = true;
        hazard.debugName = name;

        GameObject waypointsRoot = CreateEmpty($"{name}_Waypoints", enemy.transform.parent);
        GameObject left = CreateEmpty("PatrolPoint_Left", waypointsRoot.transform);
        GameObject right = CreateEmpty("PatrolPoint_Right", waypointsRoot.transform);
        left.transform.localPosition = enemy.transform.parent.InverseTransformPoint(leftWorldPosition);
        right.transform.localPosition = enemy.transform.parent.InverseTransformPoint(rightWorldPosition);

        OrigamiFoldPatrolMover patrol = enemy.AddComponent<OrigamiFoldPatrolMover>();
        patrol.waypoints = new[] { left.transform, right.transform };
        patrol.moveSpeed = speed;
        patrol.waitAtPointSeconds = 0.18f;
        patrol.pingPong = true;
        patrol.useLocalSpace = true;
        patrol.playOnStart = true;
        patrol.constrainToWalkableAreas = false;
        patrol.walkableMask = walkableMask;
        patrol.walkableProbeRadius = 0.08f;

        OrigamiFoldTrapTarget trapTarget = enemy.AddComponent<OrigamiFoldTrapTarget>();
        trapTarget.activeRoot = activeVisual;
        trapTarget.hazardColliders = new Collider2D[] { collider };
        trapTarget.patrolMover = patrol;
        trapTarget.resetPatrolOnUntrap = true;
        trapTarget.pausePatrolWhenTrapped = true;

        return patrol;
    }

    private static GameObject CreateCrowEnemyVisual(Transform parent)
    {
        Sprite crowSprite = FindCrowSprite();

        if (crowSprite == null)
        {
            Debug.LogWarning(
                $"Crow sprite was not found at {CrowSpritePath}. Falling back to red enemy marker.");
            return CreateQuad(
                "Visual",
                parent,
                Vector3.zero,
                new Vector3(0.28f, 0.28f, 1f),
                new Color(1f, 0.08f, 0.32f, 1f),
                CrowEnemySortingOrder);
        }

        GameObject visual = CreateEmpty("Visual", parent);
        ConfigureCrowVisualObject(visual, crowSprite);
        return visual;
    }

    private static void ConfigureCrowEnemyVisual(GameObject enemy, Sprite crowSprite)
    {
        Transform visualTransform = enemy.transform.Find("Visual");
        GameObject visual = visualTransform != null
            ? visualTransform.gameObject
            : CreateEmpty("Visual", enemy.transform);

        ConfigureCrowVisualObject(visual, crowSprite);

        OrigamiFoldTrapTarget trapTarget = enemy.GetComponent<OrigamiFoldTrapTarget>();

        if (trapTarget != null)
        {
            trapTarget.activeRoot = visual;

            Collider2D hazardCollider = enemy.GetComponent<Collider2D>();

            if (hazardCollider != null)
            {
                trapTarget.hazardColliders = new[] { hazardCollider };
            }
        }

        OrigamiFoldHazard hazard = enemy.GetComponent<OrigamiFoldHazard>();

        if (hazard != null)
        {
            hazard.visualRoot = visual;
        }

        ConfigureCrowVisualAnimator(enemy, visual);
    }

    private static void ConfigureCrowVisualObject(GameObject visual, Sprite crowSprite)
    {
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        MeshRenderer meshRenderer = visual.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            Object.DestroyImmediate(meshRenderer);
        }

        MeshFilter meshFilter = visual.GetComponent<MeshFilter>();

        if (meshFilter != null)
        {
            Object.DestroyImmediate(meshFilter);
        }

        Collider collider = visual.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = visual.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = crowSprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = CrowEnemySortingOrder;

        Bounds bounds = crowSprite.bounds;
        float largestSide = Mathf.Max(bounds.size.x, bounds.size.y);
        float scale = largestSide > 0f ? CrowEnemyVisualSize / largestSide : 1f;
        visual.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void ConfigureCrowVisualAnimator(GameObject enemy, GameObject visual)
    {
        OrigamiFoldPatrolVisualAnimator animator =
            enemy.GetComponent<OrigamiFoldPatrolVisualAnimator>();

        if (animator == null)
        {
            animator = enemy.AddComponent<OrigamiFoldPatrolVisualAnimator>();
        }

        animator.visualRoot = visual.transform;
        animator.spriteRenderer = visual.GetComponent<SpriteRenderer>();
        animator.spriteFacesRight = true;
        animator.flipByHorizontalMovement = true;
        animator.directionThreshold = 0.0005f;
        animator.rockTiltAmplitude = CrowEnemyRockTiltAmplitude;
        animator.rockSpeed = CrowEnemyRockSpeed;
        animator.CaptureBasePose();
    }

    private static void ConfigureSkyEnemyTraps(
        OrigamiFoldStripSqueezeAction middleColumnActionX3,
        OrigamiFoldStripSqueezeAction middleColumnActionX4,
        OrigamiFoldStripSqueezeAction rightRowAction,
        OrigamiFoldStripSqueezeAction rightColumnAction,
        OrigamiFoldPatrolMover[] patrols)
    {
        OrigamiConditionalTrapTarget[] columnX3Targets =
            CreateColumnTrapTargets(patrols, MiddleColumnFoldX3);
        OrigamiConditionalTrapTarget[] columnX4Targets =
            CreateColumnTrapTargets(patrols, MiddleColumnFoldX4);
        OrigamiConditionalTrapTarget[] rowY2Targets =
            CreateRowTrapTargets(patrols, RightRowFoldY);
        OrigamiConditionalTrapTarget[] columnX7Targets =
            CreateColumnTrapTargets(patrols, RightColumnFoldX);

        ConfigurePreTrap(middleColumnActionX3, columnX3Targets);
        ConfigurePreTrap(middleColumnActionX4, columnX4Targets);
        ConfigurePreTrap(rightRowAction, rowY2Targets);
        ConfigurePreTrap(rightColumnAction, columnX7Targets);
    }

    private static void ConfigurePreTrap(
        OrigamiFoldStripSqueezeAction action,
        OrigamiConditionalTrapTarget[] trapTargets)
    {
        action.trapTargetsBeforeActive = true;
        action.conditionalTrapTargetsWhenActive = trapTargets;
    }

    private static OrigamiConditionalTrapTarget[] CreateColumnTrapTargets(
        OrigamiFoldPatrolMover[] patrols,
        int columnX)
    {
        Vector3 columnCenter = GridToWorldPosition(columnX, 0f);
        Vector2 boundsCenter = new Vector2(columnCenter.x, 0f);
        Vector2 boundsSize = new Vector2(CellSize, MapHeight * CellSize);

        return CreateTrapTargets(patrols, boundsCenter, boundsSize);
    }

    private static OrigamiConditionalTrapTarget[] CreateRowTrapTargets(
        OrigamiFoldPatrolMover[] patrols,
        int rowY)
    {
        Vector3 rowCenter = GridToWorldPosition(0f, rowY);
        Vector2 boundsCenter = new Vector2(0f, rowCenter.y);
        Vector2 boundsSize = new Vector2(MapWidth * CellSize, CellSize);

        return CreateTrapTargets(patrols, boundsCenter, boundsSize);
    }

    private static OrigamiConditionalTrapTarget[] CreateTrapTargets(
        OrigamiFoldPatrolMover[] patrols,
        Vector2 boundsCenter,
        Vector2 boundsSize)
    {
        if (patrols == null)
        {
            return new OrigamiConditionalTrapTarget[0];
        }

        List<OrigamiConditionalTrapTarget> trapTargets =
            new List<OrigamiConditionalTrapTarget>();

        for (int i = 0; i < patrols.Length; i++)
        {
            OrigamiFoldPatrolMover patrol = patrols[i];

            if (patrol == null)
            {
                continue;
            }

            OrigamiFoldTrapTarget trapTarget =
                patrol.GetComponent<OrigamiFoldTrapTarget>();

            if (trapTarget == null)
            {
                continue;
            }

            trapTargets.Add(new OrigamiConditionalTrapTarget
            {
                trapTarget = trapTarget,
                requireInsideWorldBounds = true,
                worldBoundsCenter = boundsCenter,
                worldBoundsSize = boundsSize
            });
        }

        return trapTargets.ToArray();
    }

    private static void AssignHazards(Transform root, OrigamiFoldPuzzleState puzzleState)
    {
        OrigamiFoldHazard[] hazards = root.GetComponentsInChildren<OrigamiFoldHazard>(true);

        for (int i = 0; i < hazards.Length; i++)
        {
            hazards[i].puzzleState = puzzleState;
        }
    }

    private static void CreateFinalCutsceneTrigger(Transform parent, CellData parentCell)
    {
        GameObject exit = CreateEmpty("FinalCutsceneTrigger", parent);
        exit.transform.position =
            parentCell.gameObject.transform.TransformPoint(CellContentLocalOffset)
            + new Vector3(0f, 0f, 0f);

        CreateQuad(
            "Visual",
            exit.transform,
            Vector3.zero,
            new Vector3(0.34f, 0.34f, 1f),
            new Color(0.2f, 1f, 0.9f, 1f),
            74);

        BoxCollider2D collider = exit.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.62f, 0.62f);

        OrigamiFoldSceneExit sceneExit = exit.AddComponent<OrigamiFoldSceneExit>();
        sceneExit.nextSceneName = FinalCutsceneSceneName;
        sceneExit.loadSceneOnEnter = true;
        sceneExit.visualRoot = exit;
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

        CreateColumnGuide(parent, "MiddleColumnFoldGuide_x4", MiddleColumnFoldX3);
        CreateColumnGuide(parent, "MiddleColumnFoldGuide_x5", MiddleColumnFoldX4);
        CreateRowGuide(parent, "RightRowFoldGuide_y3", RightRowFoldY);
        CreateColumnGuide(parent, "RightColumnFoldGuide_x8", RightColumnFoldX);
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
            new Vector3(0f, 3.84f, 0f),
            "WASD move. Avoid sky enemies. Drag black points to fold paths.",
            new Color(0.95f, 0.96f, 1f, 1f),
            0.145f,
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

    private static GameObject FindGameObjectInScene(Scene scene, string objectName)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform found = FindChildRecursive(rootObjects[i].transform, objectName);

            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), objectName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool IsWalkableTile(char tile)
    {
        return tile == 'G'
            || tile == 'S';
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
                return new Color(0.28f, 0.62f, 0.48f, 1f);
            case 'S':
                return new Color(1f, 0.62f, 0.22f, 1f);
            default:
                return new Color(0.17f, 0.19f, 0.24f, 1f);
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

        Debug.LogWarning("Walkable layer was not found. Book Level 04 uses Default layer for walkable areas.");
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
