using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldWorkbenchBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Prototype_BookPuzzle.unity";
    private const string WorkbenchScenePath = "Assets/Scenes/Prototype_OrigamiFold_Workbench.unity";

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 1")]
    public static void RebuildWorkbenchStep1()
    {
        RebuildWorkbench(includeGridStep: false);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Rebuild Workbench Step 2")]
    public static void RebuildWorkbenchStep2()
    {
        RebuildWorkbench(includeGridStep: true);
    }

    private static void RebuildWorkbench(bool includeGridStep)
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
        RebuildOrigamiObjects(includeGridStep);
        EditorSceneManager.SaveScene(workbenchScene);
        AssetDatabase.Refresh();

        string stepName = includeGridStep ? "Step 2" : "Step 1";
        Debug.Log($"Rebuilt origami fold workbench {stepName}: {WorkbenchScenePath}");
    }

    private static void RebuildOrigamiObjects(bool includeGridStep)
    {
        DeleteIfExists("ORIGAMI_FOLD_SYSTEM");
        DeleteIfExists("ORIGAMI_FOLD_POINTS");
        DeleteIfExists("ORIGAMI_FOLD_LINKS");
        DeleteIfExists("ORIGAMI_DEBUG");
        DeleteIfExists("ORIGAMI_WORKBENCH_VISUALS");
        DeleteIfExists("ORIGAMI_GRID");
        DeleteIfExists("ORIGAMI_ACTIONS");

        GameObject systemRoot = new GameObject("ORIGAMI_FOLD_SYSTEM");
        GameObject pointsRoot = new GameObject("ORIGAMI_FOLD_POINTS");
        GameObject linksRoot = new GameObject("ORIGAMI_FOLD_LINKS");
        GameObject debugRoot = new GameObject("ORIGAMI_DEBUG");
        GameObject visualsRoot = new GameObject("ORIGAMI_WORKBENCH_VISUALS");

        Camera camera = FindMainCameraOrCreate();
        CreateWorkbenchVisuals(visualsRoot.transform, includeGridStep);

        GameObject executeIndicator = CreateExecuteIndicator(debugRoot.transform);
        CreateInstructionText(debugRoot.transform, includeGridStep);

        OrigamiFoldPoint topLeft = CreateFoldPoint(
            "OrigamiPoint_TopLeft",
            new Vector3(-1f, 1f, 0f),
            pointsRoot.transform);

        OrigamiFoldPoint topRight = CreateFoldPoint(
            "OrigamiPoint_TopRight",
            new Vector3(1f, 1f, 0f),
            pointsRoot.transform);

        OrigamiFoldPoint bottomLeft = CreateFoldPoint(
            "OrigamiPoint_BottomLeft",
            new Vector3(-1f, -1f, 0f),
            pointsRoot.transform);

        OrigamiFoldPoint bottomRight = CreateFoldPoint(
            "OrigamiPoint_BottomRight",
            new Vector3(1f, -1f, 0f),
            pointsRoot.transform);

        OrigamiFoldMoveAction compressHorizontalAction = null;

        if (includeGridStep)
        {
            compressHorizontalAction = CreateStep2GridAndAction();
        }

        OrigamiFoldLink top = CreateFoldLink(
            "OrigamiLink_Top",
            topLeft,
            topRight,
            executeIndicator,
            linksRoot.transform);

        OrigamiFoldLink topUnfold = null;

        if (includeGridStep)
        {
            top.bidirectional = false;
            top.targetMoveAction = compressHorizontalAction;
            top.activeStateOnExecute = true;

            topUnfold = CreateFoldLink(
                "OrigamiLink_Top_Unfold",
                topRight,
                topLeft,
                executeIndicator,
                linksRoot.transform);

            topUnfold.bidirectional = false;
            topUnfold.targetMoveAction = compressHorizontalAction;
            topUnfold.activeStateOnExecute = false;
        }

        OrigamiFoldLink bottom = CreateFoldLink(
            "OrigamiLink_Bottom",
            bottomLeft,
            bottomRight,
            executeIndicator,
            linksRoot.transform);

        OrigamiFoldLink left = CreateFoldLink(
            "OrigamiLink_Left",
            topLeft,
            bottomLeft,
            executeIndicator,
            linksRoot.transform);

        OrigamiFoldLink right = CreateFoldLink(
            "OrigamiLink_Right",
            topRight,
            bottomRight,
            executeIndicator,
            linksRoot.transform);

        OrigamiFoldDragController controller = systemRoot.AddComponent<OrigamiFoldDragController>();
        controller.targetCamera = camera;
        controller.snapDistance = 0.45f;
        controller.autoFindLinks = true;
        controller.links = includeGridStep
            ? new[] { top, topUnfold, bottom, left, right }
            : new[] { top, bottom, left, right };
    }

    private static OrigamiFoldMoveAction CreateStep2GridAndAction()
    {
        GameObject gridRoot = new GameObject("ORIGAMI_GRID");
        GameObject cellsRoot = new GameObject("Cells");
        cellsRoot.transform.SetParent(gridRoot.transform);
        cellsRoot.transform.localPosition = Vector3.zero;

        GameObject actionsRoot = new GameObject("ORIGAMI_ACTIONS");

        OrigamiFoldBoard board = gridRoot.AddComponent<OrigamiFoldBoard>();
        board.cellSize = 1f;
        board.originLocalPosition = new Vector2(-1.5f, -0.4f);
        board.foldAnimationDuration = 0.25f;

        OrigamiFoldCell cell0 = CreateCell(
            "Cell_0_0",
            new Vector2Int(0, 0),
            new Color(0.16f, 0.46f, 0.78f),
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell1 = CreateCell(
            "Cell_1_0",
            new Vector2Int(1, 0),
            new Color(0.84f, 0.66f, 0.22f),
            board,
            cellsRoot.transform);

        OrigamiFoldCell cell2 = CreateCell(
            "Cell_2_0",
            new Vector2Int(2, 0),
            new Color(0.28f, 0.68f, 0.38f),
            board,
            cellsRoot.transform);

        OrigamiFoldCell buffer = CreateCell(
            "Cell_3_0_Buffer",
            new Vector2Int(3, 0),
            new Color(0.68f, 0.32f, 0.74f),
            board,
            cellsRoot.transform);

        GameObject actionObject = new GameObject("OrigamiFoldMoveAction_CompressHorizontal");
        actionObject.transform.SetParent(actionsRoot.transform);

        OrigamiFoldMoveAction action = actionObject
            .AddComponent<OrigamiFoldMoveAction>();
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
        action.enableWhenActive = new GameObject[0];
        action.disableWhenActive = new[] { cell1.gameObject };
        action.enableWhenInactive = new[] { cell1.gameObject };
        action.disableWhenInactive = new GameObject[0];

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
        visual.transform.localScale = new Vector3(0.86f, 0.86f, 1f);

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
        labelObject.transform.localPosition = new Vector3(-0.36f, 0.2f, -0.02f);

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = textValue;
        text.characterSize = 0.11f;
        text.fontSize = 24;
        text.anchor = TextAnchor.MiddleLeft;
        text.color = Color.white;

        Renderer renderer = labelObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 20;
        }
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

    private static Camera FindMainCameraOrCreate()
    {
        Camera camera = Camera.main;

        if (camera != null)
        {
            ConfigureCamera(camera);
            return camera;
        }

        GameObject existing = GameObject.Find("Main Camera");

        if (existing != null)
        {
            camera = existing.GetComponent<Camera>();

            if (camera != null)
            {
                ConfigureCamera(camera);
                return camera;
            }
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        camera = cameraObject.AddComponent<Camera>();
        ConfigureCamera(camera);
        cameraObject.AddComponent<AudioListener>();

        return camera;
    }

    private static void ConfigureCamera(Camera camera)
    {
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 3.5f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateWorkbenchVisuals(Transform parent, bool includeGridStep)
    {
        if (includeGridStep)
        {
            CreateWorkbenchTile(
                "WorkbenchPlate",
                new Vector3(0f, -0.4f, 0.35f),
                new Color(0.12f, 0.14f, 0.16f),
                new Vector3(4.3f, 1.25f, 1f),
                parent);

            return;
        }

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

    private static OrigamiFoldPoint CreateFoldPoint(string objectName, Vector3 position, Transform parent)
    {
        GameObject pointObject = new GameObject(objectName);
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;
        pointObject.transform.localScale = Vector3.one;

        CircleCollider2D collider = pointObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.35f;
        collider.isTrigger = true;

        OrigamiFoldPoint point = pointObject.AddComponent<OrigamiFoldPoint>();
        point.pointId = objectName;
        point.normalColor = Color.white;
        point.highlightColor = Color.yellow;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(pointObject.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        Collider visualCollider = visual.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(point.normalColor);
        renderer.sortingOrder = 30;
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

    private static GameObject CreateExecuteIndicator(Transform parent)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.name = "ExecuteIndicator";
        indicator.transform.SetParent(parent);
        indicator.transform.position = new Vector3(0f, -2.35f, 0f);
        indicator.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

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

    private static void CreateInstructionText(Transform parent, bool includeGridStep)
    {
        GameObject textObject = new GameObject("InstructionText");
        textObject.transform.SetParent(parent);
        textObject.transform.position = new Vector3(-3.15f, 2.75f, 0f);

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = includeGridStep
            ? "Drag TopLeft -> TopRight to fold. Drag TopRight -> TopLeft to unfold."
            : "Drag neighboring points. Diagonals do not work.";
        text.characterSize = 0.15f;
        text.fontSize = 30;
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
