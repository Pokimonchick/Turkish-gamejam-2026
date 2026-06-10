using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldLevelBuilder
{
    private const string LevelScenePath = "Assets/Scenes/Book_Level_01_Greybox.unity";
    private const int MapWidth = 6;
    private const int MapHeight = 5;
    private const float CellSize = 1f;
    private const int RowFoldY = 1;
    private const int ColumnFoldX = 4;

    private static readonly HashSet<Vector2Int> WalkableCells = new HashSet<Vector2Int>
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
        new Vector2Int(0, 2),
        new Vector2Int(1, 2),
        new Vector2Int(2, 2),
        new Vector2Int(2, 3),
        new Vector2Int(3, 2),
        new Vector2Int(5, 2),
        new Vector2Int(5, 3)
    };

    private class CellData
    {
        public GameObject gameObject;
        public OrigamiFoldTransformStack stack;
        public Vector2Int gridPosition;
    }

    public static void CreateBookLevel01Greybox()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot rebuild Book Level 01 while Unity is in Play Mode.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Book_Level_01_Greybox";

        Camera mainCamera = CreateMainCamera();

        GameObject levelRoot = CreateEmpty("LEVEL_ROOT", null);
        GameObject foldSystemRoot = CreateEmpty("ORIGAMI_FOLD_SYSTEM", levelRoot.transform);
        GameObject mapRoot = CreateEmpty("ORIGAMI_MAP", levelRoot.transform);
        GameObject cellsRoot = CreateEmpty("Cells", mapRoot.transform);
        GameObject pointsRoot = CreateEmpty("ORIGAMI_FOLD_POINTS", levelRoot.transform);
        GameObject linksRoot = CreateEmpty("ORIGAMI_FOLD_LINKS", levelRoot.transform);
        GameObject actionsRoot = CreateEmpty("ORIGAMI_ACTIONS", levelRoot.transform);
        GameObject gameplayRoot = CreateEmpty("ORIGAMI_GAMEPLAY", levelRoot.transform);
        GameObject playerRoot = CreateEmpty("ORIGAMI_PLAYER", levelRoot.transform);
        GameObject debugRoot = CreateEmpty("ORIGAMI_DEBUG", levelRoot.transform);

        OrigamiFoldActionCoordinator coordinator = CreateCoordinator(foldSystemRoot.transform);
        CellData[,] cells = CreateMapCells(cellsRoot.transform);

        int walkableLayer = ResolveWalkableLayer();
        int walkableMask = 1 << walkableLayer;
        CreateWalkableAreas(cells, walkableLayer);

        OrigamiFoldStripSqueezeAction rowAction = CreateRowFold(
            cells,
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            coordinator);
        OrigamiFoldStripSqueezeAction columnAction = CreateColumnFold(
            cells,
            actionsRoot.transform,
            pointsRoot.transform,
            linksRoot.transform,
            coordinator);

        OrigamiFoldLink[] links = linksRoot.GetComponentsInChildren<OrigamiFoldLink>(true);
        CreateDragController(foldSystemRoot.transform, mainCamera, links);
        CreateGuides(debugRoot.transform);

        OrigamiFoldMapResetter mapResetter = CreateMapResetter(
            gameplayRoot.transform,
            coordinator,
            rowAction,
            columnAction);

        OrigamiFoldPlayerMover playerMover;
        OrigamiFoldPassenger passenger;
        GameObject player = CreatePlayer(
            playerRoot.transform,
            cells[0, 0],
            walkableMask,
            out playerMover,
            out passenger);
        GameObject respawnPoint = CreateRespawnPoint(gameplayRoot.transform, cells[0, 0]);

        GameObject fireCollectedIndicator = CreateIndicator(
            "FireCollectedIndicator",
            gameplayRoot.transform,
            new Vector3(-2.3f, 3.12f, 0f),
            "FIRE",
            new Color(1f, 0.72f, 0.15f, 1f),
            0.18f);
        fireCollectedIndicator.SetActive(false);

        GameObject completeIndicator = CreateIndicator(
            "CompleteIndicator",
            gameplayRoot.transform,
            new Vector3(2.25f, 3.12f, 0f),
            "COMPLETE",
            new Color(0.2f, 1f, 0.35f, 1f),
            0.18f);
        completeIndicator.SetActive(false);

        OrigamiFoldPuzzleState puzzleState = CreatePuzzleState(
            gameplayRoot.transform,
            player.transform,
            respawnPoint.transform,
            fireCollectedIndicator,
            completeIndicator,
            mapResetter,
            playerMover);

        OrigamiFoldFireShard fireShard = CreateFireShard(cells[2, 3], puzzleState);
        OrigamiFoldExit exit = CreateExit(cells[5, 2], puzzleState);
        OrigamiFoldHazard staticHazard = CreateStaticHazard(cells[1, 2], puzzleState);
        OrigamiFoldPatrolMover trappablePatrol = CreateRowTrappablePatrol(
            cells[2, 1],
            puzzleState,
            rowAction);

        puzzleState.fireShards = new[] { fireShard };
        puzzleState.exits = new[] { exit };
        puzzleState.patrols = new[] { trappablePatrol };
        puzzleState.autoFindResetObjects = true;

        passenger.currentStack = cells[0, 0].stack;

        CreateInstructionText(debugRoot.transform);
        SelectSceneRoot(levelRoot);

        EditorSceneManager.SaveScene(scene, LevelScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created greybox level at {LevelScenePath}. Static hazard: {staticHazard.name}.");
    }

    private static Camera CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.45f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(0f, 0.18f, -10f);
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
            for (int x = 0; x < MapWidth; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                bool walkable = WalkableCells.Contains(gridPosition);
                bool rowBarrier = y == RowFoldY;
                bool columnBarrier = x == ColumnFoldX;
                string name = $"MapCell_{x}_{y}";

                GameObject cellObject = CreateEmpty(name, parent);
                cellObject.transform.localPosition = GridToWorldPosition(x, y);

                OrigamiFoldTransformStack stack =
                    cellObject.AddComponent<OrigamiFoldTransformStack>();
                stack.CaptureBaseTransform();

                Color cellColor = GetCellColor(walkable, rowBarrier, columnBarrier);
                GameObject visual = CreateQuad(
                    "Visual",
                    cellObject.transform,
                    Vector3.zero,
                    new Vector3(0.92f, 0.92f, 1f),
                    cellColor,
                    0);

                if (walkable)
                {
                    visual.name = "WalkableVisual";
                }
                else if (rowBarrier || columnBarrier)
                {
                    visual.name = "BarrierVisual";
                }

                CreateText(
                    $"Label_{x}_{y}",
                    cellObject.transform,
                    new Vector3(0f, -0.34f, -0.01f),
                    $"{x},{y}",
                    new Color(0.85f, 0.88f, 0.9f, 0.9f),
                    0.08f,
                    20);

                cells[x, y] = new CellData
                {
                    gameObject = cellObject,
                    stack = stack,
                    gridPosition = gridPosition
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
                Vector2Int gridPosition = new Vector2Int(x, y);

                if (!WalkableCells.Contains(gridPosition))
                {
                    continue;
                }

                GameObject walkableObject = CreateEmpty("WalkableArea", cells[x, y].gameObject.transform);
                walkableObject.layer = walkableLayer;

                BoxCollider2D collider = walkableObject.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(1.04f, 1.04f);

                OrigamiFoldWalkableArea area = walkableObject.AddComponent<OrigamiFoldWalkableArea>();
                area.ownerStack = cells[x, y].stack;
                area.isWalkable = true;
            }
        }
    }

    private static OrigamiFoldStripSqueezeAction CreateRowFold(
        CellData[,] cells,
        Transform actionsParent,
        Transform pointsParent,
        Transform linksParent,
        OrigamiFoldActionCoordinator coordinator)
    {
        GameObject actionObject = CreateEmpty("Row_StripSqueezeAction", actionsParent);
        OrigamiFoldStripSqueezeAction action =
            actionObject.AddComponent<OrigamiFoldStripSqueezeAction>();
        action.animationDuration = 0.32f;
        action.coordinator = coordinator;
        action.useCoordinator = true;
        action.targets = CreateRowTargets(cells);

        float mapLeftX = GetMapLeftX();
        float mapRightX = GetMapRightX();
        float rowCenterY = GridToWorldPosition(0, RowFoldY).y;
        float rowTopY = rowCenterY + CellSize * 0.5f;
        float rowBottomY = rowCenterY - CellSize * 0.5f;

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "Row_Point_TopLeft",
            pointsParent,
            new Vector3(mapLeftX, rowTopY, 0f),
            Color.white,
            0.23f);
        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "Row_Point_BottomLeft",
            pointsParent,
            new Vector3(mapLeftX, rowBottomY, 0f),
            Color.white,
            0.23f);
        OrigamiFoldPoint topRight = CreateFoldPoint(
            "Row_Point_TopRight",
            pointsParent,
            new Vector3(mapRightX, rowTopY, 0f),
            Color.white,
            0.23f);
        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "Row_Point_BottomRight",
            pointsParent,
            new Vector3(mapRightX, rowBottomY, 0f),
            Color.white,
            0.23f);

        OrigamiFoldPoint mergedLeft = CreateMergedFoldPoint(
            "Row_Point_MergedLeft",
            pointsParent,
            new Vector3(mapLeftX, rowCenterY, 0f),
            action);
        OrigamiFoldPoint mergedRight = CreateMergedFoldPoint(
            "Row_Point_MergedRight",
            pointsParent,
            new Vector3(mapRightX, rowCenterY, 0f),
            action);

        mergedLeft.gameObject.SetActive(false);
        mergedRight.gameObject.SetActive(false);

        action.disableAfterActive = new[]
        {
            topLeft.gameObject,
            bottomLeft.gameObject,
            topRight.gameObject,
            bottomRight.gameObject
        };
        action.enableAfterActive = new[]
        {
            mergedLeft.gameObject,
            mergedRight.gameObject
        };
        action.enableAfterInactive = action.disableAfterActive;
        action.disableAfterInactive = action.enableAfterActive;

        CreateLink("Row_Link_Left_TTB", linksParent, topLeft, bottomLeft, action);
        CreateLink("Row_Link_Left_BTT", linksParent, bottomLeft, topLeft, action);
        CreateLink("Row_Link_Right_TTB", linksParent, topRight, bottomRight, action);
        CreateLink("Row_Link_Right_BTT", linksParent, bottomRight, topRight, action);

        return action;
    }

    private static OrigamiFoldStripSqueezeAction CreateColumnFold(
        CellData[,] cells,
        Transform actionsParent,
        Transform pointsParent,
        Transform linksParent,
        OrigamiFoldActionCoordinator coordinator)
    {
        GameObject actionObject = CreateEmpty("Column_StripSqueezeAction", actionsParent);
        OrigamiFoldStripSqueezeAction action =
            actionObject.AddComponent<OrigamiFoldStripSqueezeAction>();
        action.animationDuration = 0.32f;
        action.coordinator = coordinator;
        action.useCoordinator = true;
        action.targets = CreateColumnTargets(cells);

        float mapTopY = GetMapTopY();
        float mapBottomY = GetMapBottomY();
        float columnCenterX = GridToWorldPosition(ColumnFoldX, 0).x;
        float columnLeftX = columnCenterX - CellSize * 0.5f;
        float columnRightX = columnCenterX + CellSize * 0.5f;

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "Column_Point_TopLeft",
            pointsParent,
            new Vector3(columnLeftX, mapTopY, 0f),
            Color.white,
            0.23f);
        OrigamiFoldPoint topRight = CreateFoldPoint(
            "Column_Point_TopRight",
            pointsParent,
            new Vector3(columnRightX, mapTopY, 0f),
            Color.white,
            0.23f);
        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "Column_Point_BottomLeft",
            pointsParent,
            new Vector3(columnLeftX, mapBottomY, 0f),
            Color.white,
            0.23f);
        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "Column_Point_BottomRight",
            pointsParent,
            new Vector3(columnRightX, mapBottomY, 0f),
            Color.white,
            0.23f);

        OrigamiFoldPoint mergedTop = CreateMergedFoldPoint(
            "Column_Point_MergedTop",
            pointsParent,
            new Vector3(columnCenterX, mapTopY, 0f),
            action);
        OrigamiFoldPoint mergedBottom = CreateMergedFoldPoint(
            "Column_Point_MergedBottom",
            pointsParent,
            new Vector3(columnCenterX, mapBottomY, 0f),
            action);

        mergedTop.gameObject.SetActive(false);
        mergedBottom.gameObject.SetActive(false);

        action.disableAfterActive = new[]
        {
            topLeft.gameObject,
            topRight.gameObject,
            bottomLeft.gameObject,
            bottomRight.gameObject
        };
        action.enableAfterActive = new[]
        {
            mergedTop.gameObject,
            mergedBottom.gameObject
        };
        action.enableAfterInactive = action.disableAfterActive;
        action.disableAfterInactive = action.enableAfterActive;

        CreateLink("Column_Link_Top_LTR", linksParent, topLeft, topRight, action);
        CreateLink("Column_Link_Top_RTL", linksParent, topRight, topLeft, action);
        CreateLink("Column_Link_Bottom_LTR", linksParent, bottomLeft, bottomRight, action);
        CreateLink("Column_Link_Bottom_RTL", linksParent, bottomRight, bottomLeft, action);

        return action;
    }

    private static OrigamiStripContributionTarget[] CreateRowTargets(CellData[,] cells)
    {
        List<OrigamiStripContributionTarget> targets = new List<OrigamiStripContributionTarget>();

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Vector3 offset = Vector3.zero;
                Vector3 scaleMultiplier = Vector3.one;

                if (y < RowFoldY)
                {
                    offset = new Vector3(0f, 0.5f, 0f);
                }
                else if (y == RowFoldY)
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
                    activeLocalScaleMultiplier = scaleMultiplier
                });
            }
        }

        return targets.ToArray();
    }

    private static OrigamiStripContributionTarget[] CreateColumnTargets(CellData[,] cells)
    {
        List<OrigamiStripContributionTarget> targets = new List<OrigamiStripContributionTarget>();

        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                Vector3 offset = Vector3.zero;
                Vector3 scaleMultiplier = Vector3.one;

                if (x < ColumnFoldX)
                {
                    offset = new Vector3(0.5f, 0f, 0f);
                }
                else if (x == ColumnFoldX)
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
                    activeLocalScaleMultiplier = scaleMultiplier
                });
            }
        }

        return targets.ToArray();
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

    private static OrigamiFoldMapResetter CreateMapResetter(
        Transform parent,
        OrigamiFoldActionCoordinator coordinator,
        OrigamiFoldStripSqueezeAction rowAction,
        OrigamiFoldStripSqueezeAction columnAction)
    {
        GameObject resetterObject = CreateEmpty("MapResetter", parent);
        OrigamiFoldMapResetter resetter =
            resetterObject.AddComponent<OrigamiFoldMapResetter>();
        resetter.autoFindActions = false;
        resetter.stripActions = new[] { rowAction, columnAction };
        resetter.coordinator = coordinator;
        resetter.resetTimeoutSeconds = 5f;
        return resetter;
    }

    private static GameObject CreatePlayer(
        Transform parent,
        CellData startCell,
        int walkableMask,
        out OrigamiFoldPlayerMover mover,
        out OrigamiFoldPassenger passenger)
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

        mover = player.AddComponent<OrigamiFoldPlayerMover>();
        mover.moveSpeed = 3.5f;
        mover.bodyRadius = 0.18f;
        mover.sampleProbeRadius = 0.025f;
        mover.walkableMask = walkableMask;
        mover.requireAllSamplesInsideWalkable = true;

        passenger = player.AddComponent<OrigamiFoldPassenger>();
        passenger.walkableMask = walkableMask;
        passenger.probeRadius = 0.18f;
        passenger.currentStack = startCell.stack;
        passenger.disableWhileCarried = new Behaviour[] { mover };

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

    private static OrigamiFoldPuzzleState CreatePuzzleState(
        Transform parent,
        Transform player,
        Transform respawnPoint,
        GameObject fireCollectedIndicator,
        GameObject completeIndicator,
        OrigamiFoldMapResetter mapResetter,
        Behaviour playerMover)
    {
        GameObject stateObject = CreateEmpty("PuzzleState", parent);
        OrigamiFoldPuzzleState state = stateObject.AddComponent<OrigamiFoldPuzzleState>();
        state.player = player;
        state.respawnPoint = respawnPoint;
        state.fireCollectedIndicator = fireCollectedIndicator;
        state.completeIndicator = completeIndicator;
        state.mapResetter = mapResetter;
        state.resetFoldsOnRespawn = true;
        state.resetProgressOnRespawn = true;
        state.resetPatrolsOnRespawn = true;
        state.disableWhileRespawning = new[] { playerMover };
        return state;
    }

    private static OrigamiFoldFireShard CreateFireShard(
        CellData parentCell,
        OrigamiFoldPuzzleState puzzleState)
    {
        GameObject shard = CreateEmpty("FireShard", parentCell.gameObject.transform);
        shard.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        GameObject visual = CreateQuad(
            "Visual",
            shard.transform,
            Vector3.zero,
            new Vector3(0.28f, 0.28f, 1f),
            new Color(1f, 0.78f, 0.08f, 1f),
            70);

        CircleCollider2D collider = shard.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.17f;

        OrigamiFoldFireShard fireShard = shard.AddComponent<OrigamiFoldFireShard>();
        fireShard.puzzleState = puzzleState;
        fireShard.visualRoot = visual;
        fireShard.triggerColliders = new Collider2D[] { collider };
        fireShard.disableCollidersOnCollect = true;

        return fireShard;
    }

    private static OrigamiFoldExit CreateExit(
        CellData parentCell,
        OrigamiFoldPuzzleState puzzleState)
    {
        GameObject exit = CreateEmpty("Exit", parentCell.gameObject.transform);
        exit.transform.localPosition = Vector3.zero;

        GameObject lockedVisual = CreateQuad(
            "LockedVisual",
            exit.transform,
            Vector3.zero,
            new Vector3(0.42f, 0.42f, 1f),
            new Color(0.9f, 0.15f, 0.12f, 1f),
            68);
        GameObject openVisual = CreateQuad(
            "OpenVisual",
            exit.transform,
            Vector3.zero,
            new Vector3(0.42f, 0.42f, 1f),
            new Color(0.15f, 0.9f, 0.25f, 1f),
            69);
        openVisual.SetActive(false);

        BoxCollider2D collider = exit.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.55f, 0.55f);

        OrigamiFoldExit foldExit = exit.AddComponent<OrigamiFoldExit>();
        foldExit.puzzleState = puzzleState;
        foldExit.lockedVisual = lockedVisual;
        foldExit.openVisual = openVisual;
        foldExit.RefreshVisual();

        return foldExit;
    }

    private static OrigamiFoldHazard CreateStaticHazard(
        CellData parentCell,
        OrigamiFoldPuzzleState puzzleState)
    {
        GameObject hazard = CreateEmpty("UpperStaticHazard", parentCell.gameObject.transform);
        hazard.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);

        GameObject visual = CreateQuad(
            "Visual",
            hazard.transform,
            Vector3.zero,
            new Vector3(0.24f, 0.24f, 1f),
            new Color(1f, 0.05f, 0.45f, 1f),
            72);

        CircleCollider2D collider = hazard.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.13f;

        OrigamiFoldHazard foldHazard = hazard.AddComponent<OrigamiFoldHazard>();
        foldHazard.puzzleState = puzzleState;
        foldHazard.respawnOnTouch = true;
        foldHazard.visualRoot = visual;
        foldHazard.debugName = "UpperStaticHazard";

        return foldHazard;
    }

    private static OrigamiFoldPatrolMover CreateRowTrappablePatrol(
        CellData parentCell,
        OrigamiFoldPuzzleState puzzleState,
        OrigamiFoldStripSqueezeAction rowAction)
    {
        GameObject enemy = CreateEmpty("RowTrappablePatrolEnemy", parentCell.gameObject.transform);
        enemy.transform.localPosition = new Vector3(-0.25f, 0f, 0f);

        GameObject activeRoot = CreateEmpty("ActiveRoot", enemy.transform);
        GameObject visual = CreateQuad(
            "Visual",
            activeRoot.transform,
            Vector3.zero,
            new Vector3(0.25f, 0.25f, 1f),
            new Color(1f, 0.05f, 0.45f, 1f),
            75);

        GameObject hazardColliderObject = CreateEmpty("HazardCollider", activeRoot.transform);
        CircleCollider2D hazardCollider =
            hazardColliderObject.AddComponent<CircleCollider2D>();
        hazardCollider.isTrigger = true;
        hazardCollider.radius = 0.13f;

        OrigamiFoldHazard hazard = hazardColliderObject.AddComponent<OrigamiFoldHazard>();
        hazard.puzzleState = puzzleState;
        hazard.respawnOnTouch = true;
        hazard.visualRoot = visual;
        hazard.debugName = "RowTrappablePatrolEnemy";

        GameObject trappedRoot = CreateEmpty("TrappedRoot", enemy.transform);
        CreateQuad(
            "TrappedVisual",
            trappedRoot.transform,
            Vector3.zero,
            new Vector3(0.28f, 0.12f, 1f),
            new Color(0.05f, 0.85f, 1f, 1f),
            74);
        trappedRoot.SetActive(false);

        GameObject waypointRoot = CreateEmpty("Waypoints", enemy.transform);
        Transform waypointA = CreateWaypoint(
            "PatrolPoint_A",
            waypointRoot.transform,
            new Vector3(-0.25f, 0f, 0f));
        Transform waypointB = CreateWaypoint(
            "PatrolPoint_B",
            waypointRoot.transform,
            new Vector3(0.25f, 0f, 0f));

        OrigamiFoldPatrolMover patrol = enemy.AddComponent<OrigamiFoldPatrolMover>();
        patrol.waypoints = new[] { waypointA, waypointB };
        patrol.moveSpeed = 0.8f;
        patrol.waitAtPointSeconds = 0.15f;
        patrol.pingPong = true;
        patrol.useLocalSpace = true;
        patrol.playOnStart = true;

        OrigamiFoldTrapTarget trapTarget = enemy.AddComponent<OrigamiFoldTrapTarget>();
        trapTarget.activeRoot = activeRoot;
        trapTarget.trappedRoot = trappedRoot;
        trapTarget.hazardColliders = new Collider2D[] { hazardCollider };
        trapTarget.patrolMover = patrol;
        trapTarget.resetPatrolOnUntrap = true;
        trapTarget.pausePatrolWhenTrapped = true;
        trapTarget.isTrapped = false;

        rowAction.trapTargetsWhenActive = new[] { trapTarget };

        return patrol;
    }

    private static Transform CreateWaypoint(string name, Transform parent, Vector3 localPosition)
    {
        GameObject waypoint = CreateEmpty(name, parent);
        waypoint.transform.localPosition = localPosition;
        CreateQuad(
            "DebugVisual",
            waypoint.transform,
            Vector3.zero,
            new Vector3(0.08f, 0.08f, 1f),
            new Color(1f, 1f, 1f, 0.35f),
            30);
        return waypoint.transform;
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

    private static OrigamiFoldPoint CreateMergedFoldPoint(
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
            0.3f);
        OrigamiFoldClickAction clickAction =
            point.gameObject.AddComponent<OrigamiFoldClickAction>();
        clickAction.targetStripSqueezeAction = action;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = name;
        return point;
    }

    private static OrigamiFoldLink CreateLink(
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
                new Color(1f, 1f, 1f, 0.16f),
                0.015f,
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
                new Color(1f, 1f, 1f, 0.16f),
                0.015f,
                35);
        }

        CreateRectangleGuide(
            "RowFoldStripGuide",
            parent,
            GetMapLeftX(),
            GetMapRightX(),
            GridToWorldPosition(0, RowFoldY).y - 0.5f,
            GridToWorldPosition(0, RowFoldY).y + 0.5f,
            new Color(1f, 0.84f, 0.12f, 0.95f));

        float columnCenterX = GridToWorldPosition(ColumnFoldX, 0).x;
        CreateRectangleGuide(
            "ColumnFoldStripGuide",
            parent,
            columnCenterX - 0.5f,
            columnCenterX + 0.5f,
            GetMapBottomY(),
            GetMapTopY(),
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
            0.04f,
            50);
    }

    private static GameObject CreateInstructionText(Transform parent)
    {
        return CreateText(
            "InstructionText",
            parent,
            new Vector3(0f, 3.08f, 0f),
            "Find fire. Fold the page to trap enemies and open the path.",
            new Color(0.95f, 0.96f, 1f, 1f),
            0.16f,
            100);
    }

    private static GameObject CreateIndicator(
        string name,
        Transform parent,
        Vector3 position,
        string text,
        Color color,
        float size)
    {
        GameObject indicator = CreateEmpty(name, parent);
        indicator.transform.position = position;

        CreateQuad(
            "Backplate",
            indicator.transform,
            Vector3.zero,
            new Vector3(0.86f, 0.28f, 1f),
            new Color(0.02f, 0.025f, 0.03f, 0.8f),
            82);
        CreateText(
            "Text",
            indicator.transform,
            new Vector3(0f, -0.04f, -0.01f),
            text,
            color,
            size,
            90);

        return indicator;
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

    private static Color GetCellColor(bool walkable, bool rowBarrier, bool columnBarrier)
    {
        if (walkable)
        {
            return new Color(0.22f, 0.55f, 0.34f, 1f);
        }

        if (rowBarrier || columnBarrier)
        {
            return new Color(0.42f, 0.12f, 0.12f, 1f);
        }

        return new Color(0.17f, 0.18f, 0.2f, 1f);
    }

    private static Vector3 GridToWorldPosition(int x, int y)
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

        Debug.LogWarning("Walkable layer was not found. Book Level 01 uses Default layer for walkable areas.");
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

    private static void SelectSceneRoot(GameObject levelRoot)
    {
        Selection.activeGameObject = levelRoot;
        EditorGUIUtility.PingObject(levelRoot);
    }
}
