using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldWorkbenchBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Prototype_BookPuzzle.unity";
    private const string WorkbenchScenePath = "Assets/Scenes/Prototype_OrigamiFold_Workbench.unity";

    private enum WorkbenchStep
    {
        Step1,
        Step2,
        Step3,
        Step3_1,
        Step3_2
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 1")]
    public static void RebuildWorkbenchStep1()
    {
        RebuildWorkbench(WorkbenchStep.Step1);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 2")]
    public static void RebuildWorkbenchStep2()
    {
        RebuildWorkbench(WorkbenchStep.Step2);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3")]
    public static void RebuildWorkbenchStep3()
    {
        RebuildWorkbench(WorkbenchStep.Step3);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.1 Grid Aligned")]
    public static void RebuildWorkbenchStep3_1()
    {
        RebuildWorkbench(WorkbenchStep.Step3_1);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 3.2 Horizontal Controls")]
    public static void RebuildWorkbenchStep3_2()
    {
        RebuildWorkbench(WorkbenchStep.Step3_2);
    }

    private static void RebuildWorkbench(WorkbenchStep step)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Origami fold workbench rebuild is disabled while Unity is in Play Mode.");
            return;
        }

        if (!File.Exists(SourceScenePath))
        {
            Debug.LogWarning($"Source scene not found: {SourceScenePath}");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);

        if (!EditorSceneManager.SaveScene(sourceScene, WorkbenchScenePath, true))
        {
            Debug.LogWarning($"Could not save workbench scene copy: {WorkbenchScenePath}");
            return;
        }

        Scene workbenchScene = EditorSceneManager.OpenScene(WorkbenchScenePath, OpenSceneMode.Single);

        SetLegacyContainersActive(false);
        RebuildOrigamiObjects(step);
        EditorSceneManager.SaveScene(workbenchScene);
        AssetDatabase.Refresh();

        Debug.Log($"Rebuilt origami fold workbench {step}: {WorkbenchScenePath}");
    }

    private static void RebuildOrigamiObjects(WorkbenchStep step)
    {
        DeleteIfExists("ORIGAMI_FOLD_SYSTEM");
        DeleteIfExists("ORIGAMI_FOLD_POINTS");
        DeleteIfExists("ORIGAMI_FOLD_LINKS");
        DeleteIfExists("ORIGAMI_DEBUG");
        DeleteIfExists("ORIGAMI_WORKBENCH_VISUALS");
        DeleteIfExists("ORIGAMI_GRID");
        DeleteIfExists("ORIGAMI_GRID_GUIDES");
        DeleteIfExists("ORIGAMI_ACTIONS");

        GameObject systemRoot = new GameObject("ORIGAMI_FOLD_SYSTEM");
        GameObject pointsRoot = new GameObject("ORIGAMI_FOLD_POINTS");
        GameObject linksRoot = new GameObject("ORIGAMI_FOLD_LINKS");
        GameObject debugRoot = new GameObject("ORIGAMI_DEBUG");
        GameObject visualsRoot = new GameObject("ORIGAMI_WORKBENCH_VISUALS");

        Camera camera = FindMainCameraOrCreate(step);
        CreateWorkbenchVisuals(visualsRoot.transform, step);

        GameObject executeIndicator = CreateExecuteIndicator(debugRoot.transform);
        CreateInstructionText(debugRoot.transform, step);

        bool hasGrid = step != WorkbenchStep.Step1;
        bool hasMergedPoints = step == WorkbenchStep.Step3
            || step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2;
        bool isGridAligned = step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2;

        Vector2 boardOrigin = isGridAligned ? new Vector2(-1.5f, 0f) : new Vector2(-1.5f, -0.4f);
        float cellSize = 1f;
        float cellVisualSize = isGridAligned ? 0.92f : 0.86f;
        float pointSize = isGridAligned ? 0.22f : step == WorkbenchStep.Step3 ? 0.42f : 0.55f;
        float sourcePointColliderRadius = isGridAligned
            ? pointSize * 0.5f
            : step == WorkbenchStep.Step3 ? 0.28f : 0.35f;
        float mergedPointSize = isGridAligned ? 0.30f : 0.68f;
        float mergedPointColliderRadius = isGridAligned ? mergedPointSize * 0.5f : 0.42f;

        Vector3 topLeftPosition;
        Vector3 topRightPosition;
        Vector3 bottomLeftPosition;
        Vector3 bottomRightPosition;
        Vector3 mergedTopPosition;
        Vector3 mergedBottomPosition;

        if (isGridAligned)
        {
            Vector3 collapsibleCenter = GridToLocalPosition(boardOrigin, cellSize, new Vector2Int(1, 0));
            float leftX = collapsibleCenter.x - cellSize * 0.5f;
            float rightX = collapsibleCenter.x + cellSize * 0.5f;
            float topY = collapsibleCenter.y + cellSize * 0.5f;
            float bottomY = collapsibleCenter.y - cellSize * 0.5f;

            topLeftPosition = new Vector3(leftX, topY, 0f);
            topRightPosition = new Vector3(rightX, topY, 0f);
            bottomLeftPosition = new Vector3(leftX, bottomY, 0f);
            bottomRightPosition = new Vector3(rightX, bottomY, 0f);
            mergedTopPosition = topRightPosition;
            mergedBottomPosition = bottomRightPosition;
        }
        else if (step == WorkbenchStep.Step3)
        {
            topLeftPosition = new Vector3(-1f, 0.38f, 0f);
            topRightPosition = new Vector3(0f, 0.38f, 0f);
            bottomLeftPosition = new Vector3(-1f, -1.18f, 0f);
            bottomRightPosition = new Vector3(0f, -1.18f, 0f);
            mergedTopPosition = new Vector3(-0.5f, 0.38f, 0f);
            mergedBottomPosition = new Vector3(-0.5f, -1.18f, 0f);
        }
        else
        {
            topLeftPosition = new Vector3(-1f, 1f, 0f);
            topRightPosition = new Vector3(1f, 1f, 0f);
            bottomLeftPosition = new Vector3(-1f, -1f, 0f);
            bottomRightPosition = new Vector3(1f, -1f, 0f);
            mergedTopPosition = Vector3.zero;
            mergedBottomPosition = Vector3.zero;
        }

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "OrigamiPoint_TopLeft",
            topLeftPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "OrigamiPoint_TopRight",
            topRightPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "OrigamiPoint_BottomLeft",
            bottomLeftPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "OrigamiPoint_BottomRight",
            bottomRightPosition,
            Color.white,
            new Vector3(pointSize, pointSize, 1f),
            sourcePointColliderRadius,
            30,
            pointsRoot.transform);

        OrigamiFoldPoint mergedTop = null;
        OrigamiFoldPoint mergedBottom = null;

        if (hasMergedPoints)
        {
            mergedTop = CreateFoldPoint(
                "OrigamiPoint_MergedTop",
                mergedTopPosition,
                Color.cyan,
                new Vector3(mergedPointSize, mergedPointSize, 1f),
                mergedPointColliderRadius,
                35,
                pointsRoot.transform);

            mergedBottom = CreateFoldPoint(
                "OrigamiPoint_MergedBottom",
                mergedBottomPosition,
                Color.cyan,
                new Vector3(mergedPointSize, mergedPointSize, 1f),
                mergedPointColliderRadius,
                35,
                pointsRoot.transform);
        }

        OrigamiFoldMoveAction compressHorizontalAction = null;

        if (hasGrid)
        {
            compressHorizontalAction = CreateGridAndAction(
                step,
                boardOrigin,
                cellSize,
                cellVisualSize,
                new[] { topLeft, topRight, bottomLeft, bottomRight },
                new[] { mergedTop, mergedBottom });
        }

        if (hasMergedPoints)
        {
            ConfigureMergedClickAction(mergedTop, compressHorizontalAction, camera);
            ConfigureMergedClickAction(mergedBottom, compressHorizontalAction, camera);
            mergedTop.gameObject.SetActive(false);
            mergedBottom.gameObject.SetActive(false);
        }

        if (isGridAligned)
        {
            CreateGridGuides(boardOrigin, cellSize);
        }

        OrigamiFoldLink top = null;
        OrigamiFoldLink bottom = null;
        OrigamiFoldLink left = null;
        OrigamiFoldLink right = null;
        OrigamiFoldLink topUnfold = null;
        OrigamiFoldLink topRightToLeft = null;
        OrigamiFoldLink bottomLeftToRight = null;
        OrigamiFoldLink bottomRightToLeft = null;

        if (step == WorkbenchStep.Step3_2)
        {
            top = CreateFoldLink(
                "OrigamiLink_Top_LTR",
                topLeft,
                topRight,
                executeIndicator,
                linksRoot.transform);

            topRightToLeft = CreateFoldLink(
                "OrigamiLink_Top_RTL",
                topRight,
                topLeft,
                executeIndicator,
                linksRoot.transform);

            bottomLeftToRight = CreateFoldLink(
                "OrigamiLink_Bottom_LTR",
                bottomLeft,
                bottomRight,
                executeIndicator,
                linksRoot.transform);

            bottomRightToLeft = CreateFoldLink(
                "OrigamiLink_Bottom_RTL",
                bottomRight,
                bottomLeft,
                executeIndicator,
                linksRoot.transform);

            ConfigureFoldMoveLink(top, compressHorizontalAction, true);
            ConfigureFoldMoveLink(topRightToLeft, compressHorizontalAction, true);
            ConfigureFoldMoveLink(bottomLeftToRight, compressHorizontalAction, true);
            ConfigureFoldMoveLink(bottomRightToLeft, compressHorizontalAction, true);
        }
        else
        {
            top = CreateFoldLink(
                "OrigamiLink_Top",
                topLeft,
                topRight,
                executeIndicator,
                linksRoot.transform);

            if (hasGrid)
            {
                ConfigureFoldMoveLink(top, compressHorizontalAction, true);
            }
        }

        if (step == WorkbenchStep.Step1 || step == WorkbenchStep.Step2)
        {
            bottom = CreateFoldLink(
                "OrigamiLink_Bottom",
                bottomLeft,
                bottomRight,
                executeIndicator,
                linksRoot.transform);

            left = CreateFoldLink(
                "OrigamiLink_Left",
                topLeft,
                bottomLeft,
                executeIndicator,
                linksRoot.transform);

            right = CreateFoldLink(
                "OrigamiLink_Right",
                topRight,
                bottomRight,
                executeIndicator,
                linksRoot.transform);
        }

        if (step == WorkbenchStep.Step2)
        {
            topUnfold = CreateFoldLink(
                "OrigamiLink_Top_Unfold",
                topRight,
                topLeft,
                executeIndicator,
                linksRoot.transform);

            ConfigureFoldMoveLink(topUnfold, compressHorizontalAction, false);
        }

        OrigamiFoldDragController controller = systemRoot.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = camera;
        controller.snapDistance = 0.45f;
        controller.autoFindLinks = true;

        if (step == WorkbenchStep.Step3_2)
        {
            controller.links = new[]
            {
                top,
                topRightToLeft,
                bottomLeftToRight,
                bottomRightToLeft
            };
        }
        else if (hasMergedPoints)
        {
            controller.links = new[] { top };
        }
        else if (step == WorkbenchStep.Step2)
        {
            controller.links = new[] { top, topUnfold, bottom, left, right };
        }
        else
        {
            controller.links = new[] { top, bottom, left, right };
        }
    }

    private static OrigamiFoldMoveAction CreateGridAndAction(
        WorkbenchStep step,
        Vector2 boardOrigin,
        float cellSize,
        float cellVisualSize,
        OrigamiFoldPoint[] sourcePoints,
        OrigamiFoldPoint[] mergedPoints)
    {
        GameObject gridRoot = new GameObject("ORIGAMI_GRID");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(gridRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        GameObject actionsRoot = new GameObject("ORIGAMI_ACTIONS");

        OrigamiFoldBoard board = gridRoot.AddComponent<OrigamiFoldBoard>();
        board.cellSize = cellSize;
        board.originLocalPosition = boardOrigin;
        board.foldAnimationDuration = 0.25f;

        OrigamiFoldCell cell0 = CreateCell(
            "Cell_0_0",
            new Vector2Int(0, 0),
            new Color(0.16f, 0.46f, 0.78f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell1 = CreateCell(
            "Cell_1_0",
            new Vector2Int(1, 0),
            new Color(0.84f, 0.66f, 0.22f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell2 = CreateCell(
            "Cell_2_0",
            new Vector2Int(2, 0),
            new Color(0.28f, 0.68f, 0.38f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        OrigamiFoldCell buffer = CreateCell(
            "Cell_3_0_Buffer",
            new Vector2Int(3, 0),
            new Color(0.36f, 0.22f, 0.42f),
            cellVisualSize,
            board,
            cellsRoot.transform);

        GameObject actionObject = new GameObject("OrigamiFoldMoveAction_CompressHorizontal");
        actionObject.transform.SetParent(actionsRoot.transform);

        OrigamiFoldMoveAction action = actionObject.AddComponent<OrigamiFoldMoveAction>();
        action.board = board;
        action.isActive = false;
        action.movesWhenActive = new[]
        {
            new OrigamiCellMove
            {
                cell = cell2,
                targetGridPosition = new Vector2Int(1, 0)
            },
            new OrigamiCellMove
            {
                cell = buffer,
                targetGridPosition = new Vector2Int(2, 0)
            }
        };
        action.movesWhenInactive = new[]
        {
            new OrigamiCellMove
            {
                cell = cell2,
                targetGridPosition = new Vector2Int(2, 0)
            },
            new OrigamiCellMove
            {
                cell = buffer,
                targetGridPosition = new Vector2Int(3, 0)
            }
        };

        if (step == WorkbenchStep.Step3
            || step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2)
        {
            action.enableWhenActive = ToGameObjects(mergedPoints);
            action.disableWhenActive = Combine(cell1.gameObject, ToGameObjects(sourcePoints));
            action.enableWhenInactive = Combine(cell1.gameObject, ToGameObjects(sourcePoints));
            action.disableWhenInactive = ToGameObjects(mergedPoints);
        }
        else
        {
            action.enableWhenActive = new GameObject[0];
            action.disableWhenActive = new[] { cell1.gameObject };
            action.enableWhenInactive = new[] { cell1.gameObject };
            action.disableWhenInactive = new GameObject[0];
        }

        board.SnapCellToGrid(cell0);
        board.SnapCellToGrid(cell1);
        board.SnapCellToGrid(cell2);
        board.SnapCellToGrid(buffer);

        return action;
    }

    private static OrigamiFoldCell CreateCell(
        string objectName,
        Vector2Int gridPosition,
        Color color,
        float cellVisualSize,
        OrigamiFoldBoard board,
        Transform parent)
    {
        GameObject cellObject = new GameObject(objectName);
        cellObject.transform.SetParent(parent);

        OrigamiFoldCell cell = cellObject.AddComponent<OrigamiFoldCell>();
        cell.gridPosition = gridPosition;
        cell.initialGridPosition = gridPosition;
        cellObject.transform.localPosition = board.GridToLocalPosition(gridPosition);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(cellObject.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(cellVisualSize, cellVisualSize, 1f);

        Collider visualCollider = visual.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = 10;

        CreateCellLabel(objectName, cellObject.transform);

        return cell;
    }

    private static void CreateCellLabel(string textValue, Transform parent)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent);
        labelObject.transform.localPosition = new Vector3(0f, -0.31f, -0.02f);

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = textValue;
        text.characterSize = 0.06f;
        text.fontSize = 18;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;

        Renderer renderer = labelObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 20;
        }
    }

    private static void CreateGridGuides(Vector2 boardOrigin, float cellSize)
    {
        GameObject guidesRoot = new GameObject("ORIGAMI_GRID_GUIDES");
        Color gridColor = new Color(1f, 1f, 1f, 0.28f);
        Color regionColor = new Color(1f, 0.9f, 0.18f, 0.45f);

        for (int i = 0; i <= 4; i++)
        {
            float x = boardOrigin.x + (i * cellSize) - (cellSize * 0.5f);
            CreateGuideLine(
                $"Guide_Vertical_{i}",
                new Vector3(x, 0f, 0.1f),
                new Vector3(0.025f, 1.18f, 1f),
                gridColor,
                guidesRoot.transform);
        }

        Vector3 collapsibleCenter = GridToLocalPosition(boardOrigin, cellSize, new Vector2Int(1, 0));
        CreateGuideLine(
            "Guide_Collapsible_Left",
            new Vector3(collapsibleCenter.x - 0.5f, collapsibleCenter.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Right",
            new Vector3(collapsibleCenter.x + 0.5f, collapsibleCenter.y, 0.08f),
            new Vector3(0.035f, 1.08f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Top",
            new Vector3(collapsibleCenter.x, collapsibleCenter.y + 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            guidesRoot.transform);

        CreateGuideLine(
            "Guide_Collapsible_Bottom",
            new Vector3(collapsibleCenter.x, collapsibleCenter.y - 0.5f, 0.08f),
            new Vector3(1.08f, 0.035f, 1f),
            regionColor,
            guidesRoot.transform);
    }

    private static void CreateGuideLine(
        string objectName,
        Vector3 position,
        Vector3 scale,
        Color color,
        Transform parent)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = objectName;
        line.transform.SetParent(parent);
        line.transform.position = position;
        line.transform.localScale = scale;

        Collider collider = line.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = line.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = 5;
    }

    private static Vector3 GridToLocalPosition(Vector2 origin, float cellSize, Vector2Int gridPosition)
    {
        return new Vector3(
            origin.x + gridPosition.x * cellSize,
            origin.y + gridPosition.y * cellSize,
            0f);
    }

    private static void ConfigureMergedClickAction(
        OrigamiFoldPoint mergedPoint,
        OrigamiFoldMoveAction action,
        Camera camera)
    {
        if (mergedPoint == null)
        {
            return;
        }

        OrigamiFoldClickAction clickAction = mergedPoint.gameObject
            .AddComponent<OrigamiFoldClickAction>();

        clickAction.targetCamera = camera;
        clickAction.targetMoveAction = action;
        clickAction.activeStateOnClick = false;
        clickAction.ignoreWhileActionAnimating = true;
        clickAction.debugName = mergedPoint.pointId;
    }

    private static GameObject[] ToGameObjects(OrigamiFoldPoint[] points)
    {
        if (points == null)
        {
            return new GameObject[0];
        }

        int count = 0;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                count++;
            }
        }

        GameObject[] objects = new GameObject[count];
        int index = 0;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                objects[index] = points[i].gameObject;
                index++;
            }
        }

        return objects;
    }

    private static GameObject[] Combine(GameObject first, GameObject[] rest)
    {
        int restLength = rest == null ? 0 : rest.Length;
        int extra = first == null ? 0 : 1;
        GameObject[] objects = new GameObject[restLength + extra];
        int index = 0;

        if (first != null)
        {
            objects[index] = first;
            index++;
        }

        for (int i = 0; i < restLength; i++)
        {
            objects[index] = rest[i];
            index++;
        }

        return objects;
    }

    private static void DeleteIfExists(string objectName)
    {
        GameObject existing = FindSceneObject(objectName);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void SetLegacyContainersActive(bool active)
    {
        string[] legacyContainers =
        {
            "MANAGERS",
            "BOOK",
            "PATH_GRAPH",
            "ACTORS",
            "INTERACTABLES",
            "UI",
            "FOLD_ACTIONS"
        };

        for (int i = 0; i < legacyContainers.Length; i++)
        {
            GameObject legacy = FindSceneObject(legacyContainers[i]);

            if (legacy != null)
            {
                legacy.SetActive(active);
            }
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate.name == objectName
                && candidate.scene == activeScene
                && candidate.hideFlags == HideFlags.None)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Camera FindMainCameraOrCreate(WorkbenchStep step)
    {
        Camera camera = Camera.main;

        if (camera != null)
        {
            ConfigureCamera(camera, step);
            return camera;
        }

        GameObject existing = GameObject.Find("Main Camera");

        if (existing != null)
        {
            camera = existing.GetComponent<Camera>();

            if (camera != null)
            {
                ConfigureCamera(camera, step);
                return camera;
            }
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        camera = cameraObject.AddComponent<Camera>();
        ConfigureCamera(camera, step);
        cameraObject.AddComponent<AudioListener>();

        return camera;
    }

    private static void ConfigureCamera(Camera camera, WorkbenchStep step)
    {
        if (step == WorkbenchStep.Step3_1
            || step == WorkbenchStep.Step3_2)
        {
            camera.transform.position = new Vector3(0f, 0.2f, -10f);
            camera.orthographicSize = 3.1f;
        }
        else
        {
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographicSize = 3.6f;
        }

        camera.orthographic = true;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateWorkbenchVisuals(Transform parent, WorkbenchStep step)
    {
        if (step == WorkbenchStep.Step1)
        {
            CreateWorkbenchTile(
                "WorkbenchTile_TopLeft",
                new Vector3(-0.5f, 0.5f, 0.2f),
                new Color(0.18f, 0.28f, 0.42f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            CreateWorkbenchTile(
                "WorkbenchTile_TopRight",
                new Vector3(0.5f, 0.5f, 0.2f),
                new Color(0.25f, 0.38f, 0.28f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            CreateWorkbenchTile(
                "WorkbenchTile_BottomLeft",
                new Vector3(-0.5f, -0.5f, 0.2f),
                new Color(0.42f, 0.30f, 0.18f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            CreateWorkbenchTile(
                "WorkbenchTile_BottomRight",
                new Vector3(0.5f, -0.5f, 0.2f),
                new Color(0.36f, 0.24f, 0.40f),
                new Vector3(0.95f, 0.95f, 1f),
                parent);

            return;
        }

        CreateWorkbenchTile(
            "WorkbenchPlate",
            step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2
                ? new Vector3(0f, 0f, 0.35f)
                : new Vector3(0f, -0.4f, 0.35f),
            new Color(0.12f, 0.14f, 0.16f),
            step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2
                ? new Vector3(4.25f, 1.15f, 1f)
                : new Vector3(4.3f, 1.25f, 1f),
            parent);
    }

    private static void CreateWorkbenchTile(
        string objectName,
        Vector3 position,
        Color color,
        Vector3 scale,
        Transform parent)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tile.name = objectName;
        tile.transform.SetParent(parent);
        tile.transform.position = position;
        tile.transform.localScale = scale;

        Collider collider = tile.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = tile.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = 0;
    }

    private static OrigamiFoldPoint CreateFoldPoint(
        string objectName,
        Vector3 position,
        Color color,
        Vector3 visualScale,
        float colliderRadius,
        int sortingOrder,
        Transform parent)
    {
        GameObject pointObject = new GameObject(objectName);
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;
        pointObject.transform.localScale = Vector3.one;

        CircleCollider2D collider = pointObject.AddComponent<CircleCollider2D>();
        collider.radius = colliderRadius;
        collider.isTrigger = true;

        OrigamiFoldPoint point = pointObject.AddComponent<OrigamiFoldPoint>();
        point.pointId = objectName;
        point.normalColor = color;
        point.highlightColor = Color.yellow;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(pointObject.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = visualScale;

        Collider visualCollider = visual.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(color);
        renderer.sortingOrder = sortingOrder;
        point.visualRenderer = renderer;

        return point;
    }

    private static OrigamiFoldLink CreateFoldLink(
        string objectName,
        OrigamiFoldPoint a,
        OrigamiFoldPoint b,
        GameObject executeIndicator,
        Transform parent)
    {
        GameObject linkObject = new GameObject(objectName);
        linkObject.transform.SetParent(parent);

        OrigamiFoldLink link = linkObject.AddComponent<OrigamiFoldLink>();
        link.pointA = a;
        link.pointB = b;
        link.bidirectional = true;
        link.enableOnExecute = new[] { executeIndicator };
        link.disableOnExecute = new GameObject[0];

        return link;
    }

    private static void ConfigureFoldMoveLink(
        OrigamiFoldLink link,
        OrigamiFoldMoveAction action,
        bool activeStateOnExecute)
    {
        if (link == null)
        {
            return;
        }

        link.bidirectional = false;
        link.targetMoveAction = action;
        link.activeStateOnExecute = activeStateOnExecute;
    }

    private static GameObject CreateExecuteIndicator(Transform parent)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.name = "ExecuteIndicator";
        indicator.transform.SetParent(parent);
        indicator.transform.position = new Vector3(0f, -2.1f, 0f);
        indicator.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

        Collider collider = indicator.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = indicator.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(Color.green);
        renderer.sortingOrder = 40;
        indicator.SetActive(false);

        return indicator;
    }

    private static void CreateInstructionText(Transform parent, WorkbenchStep step)
    {
        GameObject textObject = new GameObject("InstructionText");
        textObject.transform.SetParent(parent);
        textObject.transform.position = step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2
            ? new Vector3(-2.65f, 2.65f, 0f)
            : new Vector3(-2.8f, 2.85f, 0f);

        TextMesh text = textObject.AddComponent<TextMesh>();

        if (step == WorkbenchStep.Step3_2)
        {
            text.text = "Drag top or bottom edge to fold. Click cyan point to unfold.";
        }
        else if (step == WorkbenchStep.Step3_1)
        {
            text.text = "Drag TL -> TR to fold. Click cyan point to unfold.";
        }
        else if (step == WorkbenchStep.Step3)
        {
            text.text = "Drag TL -> TR to fold. Click merged point to unfold.";
        }
        else if (step == WorkbenchStep.Step2)
        {
            text.text = "Drag TopLeft -> TopRight to fold. Drag TopRight -> TopLeft to unfold.";
        }
        else
        {
            text.text = "Drag neighboring points. Diagonals do not work.";
        }

        text.characterSize = step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2 ? 0.11f : 0.13f;
        text.fontSize = step == WorkbenchStep.Step3_1 || step == WorkbenchStep.Step3_2 ? 24 : 28;
        text.anchor = TextAnchor.UpperLeft;
        text.color = Color.white;

        Renderer renderer = textObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 50;
        }
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = FindVisualShader();

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static Shader FindVisualShader()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Unlit/Color");
    }
}
