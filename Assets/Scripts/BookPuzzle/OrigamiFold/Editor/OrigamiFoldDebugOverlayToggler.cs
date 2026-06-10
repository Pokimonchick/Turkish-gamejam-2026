using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldDebugOverlayToggler
{
    private const string VillageLevel01ScenePath = "Assets/Scenes/Village_Level_01_Greybox.unity";
    private const string BookLevel01ScenePath = "Assets/Scenes/Book_Level_01_Greybox.unity";
    private const string BookLevel02ScenePath = "Assets/Scenes/Book_Level_02_Greybox.unity";
    private const string BookLevel03ScenePath = "Assets/Scenes/Book_Level_03_Greybox.unity";
    private const string BookLevel04ScenePath = "Assets/Scenes/Book_Level_04_Greybox.unity";

    private const string HighlightName = "WalkableDebugHighlight";
    private const string GridTopName = "Grid_Top";
    private const string GridBottomName = "Grid_Bottom";
    private const string GridLeftName = "Grid_Left";
    private const string GridRightName = "Grid_Right";
    private const string CoordinateLabelName = "CoordinateLabel";

    private static readonly LevelDebugTarget[] LevelTargets =
    {
        new LevelDebugTarget("Village Level 01", VillageLevel01ScenePath),
        new LevelDebugTarget("Book Level 01", BookLevel01ScenePath),
        new LevelDebugTarget("Book Level 02", BookLevel02ScenePath),
        new LevelDebugTarget("Book Level 03", BookLevel03ScenePath),
        new LevelDebugTarget("Book Level 04", BookLevel04ScenePath)
    };

    private static readonly string[] SceneWideDebugObjectNames =
    {
        "GridGuides",
        "RowFoldStripGuide",
        "ColumnFoldStripGuide",
        "CenterColumnFoldGuide_x5",
        "TriadColumnFoldGuide_x7",
        "TriadColumnFoldGuide_x6",
        "TriadRowFoldGuide_y5",
        "LeftRowFoldGuide_y2",
        "MiddleColumnFoldGuide_x4",
        "MiddleColumnFoldGuide_x5",
        "RightRowFoldGuide_y3",
        "RightColumnFoldGuide_x8"
    };

    private readonly struct LevelDebugTarget
    {
        public readonly string DisplayName;
        public readonly string ScenePath;

        public LevelDebugTarget(string displayName, string scenePath)
        {
            DisplayName = displayName;
            ScenePath = scenePath;
        }
    }

    private readonly struct MapCellInfo
    {
        public readonly Transform Transform;
        public readonly int X;
        public readonly int Y;

        public MapCellInfo(Transform transform, int x, int y)
        {
            Transform = transform;
            X = x;
            Y = y;
        }
    }

    [MenuItem("Tools/PANINI/Origami Fold/Toggle Active Level Debug Overlay")]
    public static void ToggleActiveLevelDebugOverlay()
    {
        if (!CanEditDebugOverlay("toggle the active level debug overlay"))
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("Cannot toggle debug overlay because the active scene is not saved.");
            return;
        }

        int changedObjects = ToggleOpenSceneDebugOverlay(null, out bool isEnabled);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"{scene.name} debug overlay {(isEnabled ? "enabled" : "disabled")}. "
            + $"Objects changed: {changedObjects}.");
    }

    [MenuItem("Tools/PANINI/Origami Fold/Toggle All Level Debug Overlays")]
    public static void ToggleAllLevelDebugOverlays()
    {
        if (!CanEditDebugOverlay("toggle all level debug overlays"))
        {
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;
        bool shouldEnable = ShouldEnableAllLevelOverlays();
        int totalChangedObjects = 0;
        int changedScenes = 0;

        for (int i = 0; i < LevelTargets.Length; i++)
        {
            LevelDebugTarget target = LevelTargets[i];

            if (!OpenLevelScene(target))
            {
                continue;
            }

            int changedObjects = ToggleOpenSceneDebugOverlay(shouldEnable, out _);
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            totalChangedObjects += changedObjects;
            changedScenes++;
        }

        RestoreOriginalScene(originalScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Level debug overlays {(shouldEnable ? "enabled" : "disabled")} for {changedScenes} scene(s). "
            + $"Objects changed: {totalChangedObjects}.");
    }

    [MenuItem("Tools/PANINI/Origami Fold/Toggle Village Level 01 Debug Overlay")]
    public static void ToggleVillageLevel01DebugOverlay()
    {
        ToggleConfiguredLevelDebugOverlay(LevelTargets[0]);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Toggle Book Level 01 Debug Overlay")]
    public static void ToggleBookLevel01DebugOverlay()
    {
        ToggleConfiguredLevelDebugOverlay(LevelTargets[1]);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Toggle Book Level 02 Debug Overlay")]
    public static void ToggleBookLevel02DebugOverlay()
    {
        ToggleConfiguredLevelDebugOverlay(LevelTargets[2]);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Toggle Book Level 03 Debug Overlay")]
    public static void ToggleBookLevel03DebugOverlay()
    {
        ToggleConfiguredLevelDebugOverlay(LevelTargets[3]);
    }

    [MenuItem("Tools/PANINI/Origami Fold/Toggle Book Level 04 Debug Overlay")]
    public static void ToggleBookLevel04DebugOverlay()
    {
        ToggleConfiguredLevelDebugOverlay(LevelTargets[4]);
    }

    private static void ToggleConfiguredLevelDebugOverlay(LevelDebugTarget target)
    {
        if (!CanEditDebugOverlay($"toggle {target.DisplayName} debug overlay"))
        {
            return;
        }

        if (!OpenLevelScene(target))
        {
            return;
        }

        int changedObjects = ToggleOpenSceneDebugOverlay(null, out bool isEnabled);
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"{target.DisplayName} debug overlay {(isEnabled ? "enabled" : "disabled")}. "
            + $"Objects changed: {changedObjects}.");
    }

    private static bool CanEditDebugOverlay(string actionName)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning($"Cannot {actionName} while Unity is in Play Mode.");
            return false;
        }

        return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    private static bool OpenLevelScene(LevelDebugTarget target)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(target.ScenePath) == null)
        {
            Debug.LogWarning($"{target.DisplayName} scene was not found: {target.ScenePath}");
            return false;
        }

        EditorSceneManager.OpenScene(target.ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static void RestoreOriginalScene(string originalScenePath)
    {
        if (string.IsNullOrEmpty(originalScenePath)
            || AssetDatabase.LoadAssetAtPath<SceneAsset>(originalScenePath) == null)
        {
            return;
        }

        EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
    }

    private static bool ShouldEnableAllLevelOverlays()
    {
        bool foundAnyLevel = false;
        bool allLevelsHaveOverlay = true;

        for (int i = 0; i < LevelTargets.Length; i++)
        {
            LevelDebugTarget target = LevelTargets[i];

            if (!OpenLevelScene(target))
            {
                continue;
            }

            List<MapCellInfo> cells = FindMapCells();

            if (cells.Count == 0)
            {
                Debug.LogWarning($"{target.DisplayName} has no MapCell_x_y objects.");
                allLevelsHaveOverlay = false;
                continue;
            }

            foundAnyLevel = true;

            if (!HasDebugOverlay(cells))
            {
                allLevelsHaveOverlay = false;
            }
        }

        return !foundAnyLevel || !allLevelsHaveOverlay;
    }

    private static int ToggleOpenSceneDebugOverlay(bool? forceEnabled, out bool isEnabled)
    {
        List<MapCellInfo> cells = FindMapCells();

        if (cells.Count == 0)
        {
            isEnabled = false;
            Debug.LogWarning($"{SceneManager.GetActiveScene().name} has no MapCell_x_y objects.");
            return 0;
        }

        isEnabled = forceEnabled ?? !HasDebugOverlay(cells);
        return isEnabled ? CreateDebugOverlay(cells) : CleanupDebugOverlay(cells);
    }

    private static int CreateDebugOverlay(List<MapCellInfo> cells)
    {
        CleanupDebugOverlay(cells);

        int changedObjects = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            MapCellInfo cell = cells[i];

            if (IsWalkableCell(cell.Transform))
            {
                CreateQuad(
                    HighlightName,
                    cell.Transform,
                    new Vector3(0f, 0f, -0.045f),
                    new Vector3(0.86f, 0.86f, 1f),
                    new Color(0.1f, 1f, 0.42f, 0.28f),
                    62);
                changedObjects++;
            }

            CreateCellGridLine(
                GridTopName,
                cell.Transform,
                new Vector3(0f, 0.5f, -0.04f),
                new Vector3(1f, 0.018f, 1f));
            CreateCellGridLine(
                GridBottomName,
                cell.Transform,
                new Vector3(0f, -0.5f, -0.04f),
                new Vector3(1f, 0.018f, 1f));
            CreateCellGridLine(
                GridLeftName,
                cell.Transform,
                new Vector3(-0.5f, 0f, -0.04f),
                new Vector3(0.018f, 1f, 1f));
            CreateCellGridLine(
                GridRightName,
                cell.Transform,
                new Vector3(0.5f, 0f, -0.04f),
                new Vector3(0.018f, 1f, 1f));
            CreateText(
                CoordinateLabelName,
                cell.Transform,
                new Vector3(0f, 0f, -0.05f),
                $"{cell.X},{cell.Y}",
                new Color(0.02f, 0.02f, 0.03f, 0.72f),
                0.135f,
                86);

            changedObjects += 5;
        }

        return changedObjects;
    }

    private static int CleanupDebugOverlay(List<MapCellInfo> cells)
    {
        int changedObjects = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            MapCellInfo cell = cells[i];
            changedObjects += DestroyDirectChildrenNamed(cell.Transform, HighlightName);
            changedObjects += DestroyDirectChildrenNamed(cell.Transform, GridTopName);
            changedObjects += DestroyDirectChildrenNamed(cell.Transform, GridBottomName);
            changedObjects += DestroyDirectChildrenNamed(cell.Transform, GridLeftName);
            changedObjects += DestroyDirectChildrenNamed(cell.Transform, GridRightName);
            changedObjects += DestroyDirectChildrenNamed(cell.Transform, CoordinateLabelName);
            changedObjects += DestroyLegacyCoordinateLabels(cell);
        }

        for (int i = 0; i < SceneWideDebugObjectNames.Length; i++)
        {
            changedObjects += DestroySceneObjectsNamed(SceneWideDebugObjectNames[i]);
        }

        return changedObjects;
    }

    private static bool HasDebugOverlay(List<MapCellInfo> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (HasCellDebugOverlay(cells[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCellDebugOverlay(MapCellInfo cell)
    {
        return FindDirectChild(cell.Transform, HighlightName) != null
            || FindDirectChild(cell.Transform, GridTopName) != null
            || FindDirectChild(cell.Transform, GridBottomName) != null
            || FindDirectChild(cell.Transform, GridLeftName) != null
            || FindDirectChild(cell.Transform, GridRightName) != null
            || FindDirectChild(cell.Transform, CoordinateLabelName) != null
            || FindDirectChild(cell.Transform, $"Label_{cell.X}_{cell.Y}") != null;
    }

    private static List<MapCellInfo> FindMapCells()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<MapCellInfo> cells = new List<MapCellInfo>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject sceneObject = allObjects[i];

            if (sceneObject == null)
            {
                continue;
            }

            if (TryParseMapCellName(sceneObject.name, out int x, out int y))
            {
                cells.Add(new MapCellInfo(sceneObject.transform, x, y));
            }
        }

        cells.Sort((a, b) =>
        {
            int yComparison = a.Y.CompareTo(b.Y);
            return yComparison != 0 ? yComparison : a.X.CompareTo(b.X);
        });
        return cells;
    }

    private static bool TryParseMapCellName(string objectName, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("MapCell_"))
        {
            return false;
        }

        string[] parts = objectName.Split('_');
        return parts.Length == 3
            && int.TryParse(parts[1], out x)
            && int.TryParse(parts[2], out y);
    }

    private static bool IsWalkableCell(Transform cell)
    {
        OrigamiFoldWalkableArea[] areas =
            cell.GetComponentsInChildren<OrigamiFoldWalkableArea>(true);

        for (int i = 0; i < areas.Length; i++)
        {
            if (areas[i] != null && areas[i].isWalkable)
            {
                return true;
            }
        }

        return FindDirectChild(cell, "WalkableArea") != null;
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
        quad.transform.localRotation = Quaternion.identity;
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

    private static GameObject CreateText(
        string name,
        Transform parent,
        Vector3 localPosition,
        string value,
        Color color,
        float characterSize,
        int sortingOrder)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = value;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        text.characterSize = characterSize;
        text.fontSize = 64;

        Renderer renderer = textObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
        }

        return textObject;
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

    private static int DestroyDirectChildrenNamed(Transform parent, string childName)
    {
        int destroyed = 0;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child.name != childName)
            {
                continue;
            }

            Object.DestroyImmediate(child.gameObject);
            destroyed++;
        }

        return destroyed;
    }

    private static int DestroyLegacyCoordinateLabels(MapCellInfo cell)
    {
        string labelName = $"Label_{cell.X}_{cell.Y}";
        int destroyed = 0;

        for (int i = cell.Transform.childCount - 1; i >= 0; i--)
        {
            Transform child = cell.Transform.GetChild(i);

            if (child.name != labelName)
            {
                continue;
            }

            TextMesh label = child.GetComponent<TextMesh>();

            if (label == null || label.text != $"{cell.X},{cell.Y}")
            {
                continue;
            }

            Object.DestroyImmediate(child.gameObject);
            destroyed++;
        }

        return destroyed;
    }

    private static int DestroySceneObjectsNamed(string objectName)
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<GameObject> objectsToDestroy = new List<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i] != null && allObjects[i].name == objectName)
            {
                objectsToDestroy.Add(allObjects[i]);
            }
        }

        for (int i = 0; i < objectsToDestroy.Count; i++)
        {
            Object.DestroyImmediate(objectsToDestroy[i]);
        }

        return objectsToDestroy.Count;
    }
}
