using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrigamiFoldTileGridArtApplier
{
    private const int TileGridWidth = 12;
    private const int TileGridHeight = 9;
    private const int ExpectedTileCount = TileGridWidth * TileGridHeight;
    private const float TargetCellVisualSize = 1f;
    private const float TileGridCameraMargin = 0.3f;
    private const float TargetAspect = 16f / 9f;
    private const string VillageSceneFileName = "Village_Level_01_Greybox.unity";
    private const string VillageTileFolder = "Assets/Art/Levels/Village_Level_01/Tiles";
    private const string VillageTileFallbackFolder =
        "Assets/Art/Levels/Village_Level_01_Greybox/Tiles";
    private const string BookLevel02SceneFileName = "Book_Level_02_Greybox.unity";
    private const string BookLevel02TileFolder = "Assets/Art/Levels/Book_Level_02/Tiles";
    private const string BookLevel02TileFallbackFolder =
        "Assets/Art/Levels/Book_Level_02_Greybox/Tiles";
    private const string BookLevel03SceneFileName = "Book_Level_03_Greybox.unity";
    private const string BookLevel03TileFolder = "Assets/Art/Levels/Book_Level_03/Tiles";
    private const string BookLevel03TileFallbackFolder =
        "Assets/Art/Levels/Book_Level_03_Greybox/Tiles";

    private static readonly Regex CellNameRegex =
        new Regex(@"^MapCell_(\d+)_(\d+)$", RegexOptions.Compiled);
    private static readonly Regex TileNameRegex =
        new Regex(@"^tile_r(\d{2})_c(\d{2})\.png$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [MenuItem("Tools/PANINI/Art/Apply Tile Grid Art To Active Level")]
    public static void ApplyTileGridArtToActiveLevel()
    {
        ApplyTileGridArtToActiveLevelInternal();
    }

    [MenuItem("Tools/PANINI/Origami Fold/Apply Tile Grid Art To Active Level")]
    public static void ApplyTileGridArtToActiveLevelFromOrigamiFoldMenu()
    {
        ApplyTileGridArtToActiveLevelInternal();
    }

    [MenuItem("Tools/PANINI/Art/Fit Active Level Camera To Full Tile Map")]
    public static void FitActiveLevelCameraToFullTileMap()
    {
        FitActiveLevelCameraToFullTileMapInternal();
    }

    [MenuItem("Tools/PANINI/Origami Fold/Fit Active Level Camera To Full Tile Map")]
    public static void FitActiveLevelCameraToFullTileMapFromOrigamiFoldMenu()
    {
        FitActiveLevelCameraToFullTileMapInternal();
    }

    private static void ApplyTileGridArtToActiveLevelInternal()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("Tile Grid Art Applier: active scene is not saved or is invalid.");
            return;
        }

        if (!TryResolveTileFolder(scene, out string tileFolder))
        {
            Debug.LogWarning(
                $"Tile Grid Art Applier: no tile folder is configured/found for active scene {scene.path}.");
            return;
        }

        Dictionary<Vector2Int, GameObject> cells = FindMapCells(scene);
        Dictionary<Vector2Int, string> tilePaths = FindTilePaths(tileFolder);
        List<string> missingCells = new List<string>();
        List<string> missingTiles = new List<string>();
        List<string> unmatchedTiles = new List<string>();
        int assignedCount = 0;
        int createdArtVisualCount = 0;

        WarnIfMapCellCountDoesNotMatchTiles(cells.Count);

        foreach (KeyValuePair<Vector2Int, string> tile in tilePaths)
        {
            if (!cells.TryGetValue(tile.Key, out GameObject cell))
            {
                unmatchedTiles.Add($"{Path.GetFileName(tile.Value)} -> MapCell_{tile.Key.x}_{tile.Key.y}");
                continue;
            }

            Sprite sprite = PrepareAndLoadSprite(tile.Value);

            if (sprite == null)
            {
                Debug.LogWarning($"Tile Grid Art Applier: could not load sprite for {tile.Value}.");
                continue;
            }

            if (ApplySpriteToCell(cell, sprite))
            {
                createdArtVisualCount++;
            }

            assignedCount++;
        }

        for (int y = 0; y < TileGridHeight; y++)
        {
            for (int x = 0; x < TileGridWidth; x++)
            {
                Vector2Int key = new Vector2Int(x, y);

                if (!cells.ContainsKey(key))
                {
                    missingCells.Add($"MapCell_{x}_{y}");
                }

                if (!tilePaths.ContainsKey(key))
                {
                    int row = TileGridHeight - y;
                    int column = x + 1;
                    missingTiles.Add($"tile_r{row:00}_c{column:00}.png");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(BuildReport(
            scene,
            tileFolder,
            cells.Count,
            tilePaths.Count,
            createdArtVisualCount,
            assignedCount,
            missingCells,
            missingTiles,
            unmatchedTiles));
    }

    private static void FitActiveLevelCameraToFullTileMapInternal()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("Tile Grid Art Applier: active scene is not saved or is invalid.");
            return;
        }

        Dictionary<Vector2Int, GameObject> cells = FindMapCells(scene);

        if (cells.Count == 0)
        {
            Debug.LogWarning("Tile Grid Art Applier: no MapCell_x_y objects found in active scene.");
            return;
        }

        if (FitCameraToTileGrid(scene, cells, out string report))
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(report);
            return;
        }

        Debug.LogWarning(report);
    }

    private static bool TryResolveTileFolder(Scene scene, out string tileFolder)
    {
        string sceneFileName = Path.GetFileName(scene.path);

        if (sceneFileName == VillageSceneFileName)
        {
            return TryUseFirstExistingFolder(
                out tileFolder,
                VillageTileFolder,
                VillageTileFallbackFolder);
        }

        if (sceneFileName == BookLevel02SceneFileName)
        {
            return TryUseFirstExistingFolder(
                out tileFolder,
                BookLevel02TileFolder,
                BookLevel02TileFallbackFolder);
        }

        if (sceneFileName == BookLevel03SceneFileName)
        {
            return TryUseFirstExistingFolder(
                out tileFolder,
                BookLevel03TileFolder,
                BookLevel03TileFallbackFolder);
        }

        tileFolder = null;
        return false;
    }

    private static bool TryUseFirstExistingFolder(out string folder, params string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            if (AssetDatabase.IsValidFolder(candidates[i]))
            {
                folder = candidates[i];
                return true;
            }
        }

        folder = null;
        return false;
    }

    private static Dictionary<Vector2Int, GameObject> FindMapCells(Scene scene)
    {
        Dictionary<Vector2Int, GameObject> cells = new Dictionary<Vector2Int, GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);

            for (int j = 0; j < transforms.Length; j++)
            {
                Transform transform = transforms[j];
                Match match = CellNameRegex.Match(transform.name);

                if (!match.Success)
                {
                    continue;
                }

                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                Vector2Int key = new Vector2Int(x, y);

                if (!cells.ContainsKey(key))
                {
                    cells.Add(key, transform.gameObject);
                }
            }
        }

        return cells;
    }

    private static Dictionary<Vector2Int, string> FindTilePaths(string tileFolder)
    {
        Dictionary<Vector2Int, string> tilePaths = new Dictionary<Vector2Int, string>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { tileFolder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = Path.GetFileName(path);
            Match match = TileNameRegex.Match(fileName);

            if (!match.Success)
            {
                continue;
            }

            int row = int.Parse(match.Groups[1].Value);
            int column = int.Parse(match.Groups[2].Value);
            int x = column - 1;
            int y = TileGridHeight - row;
            Vector2Int key = new Vector2Int(x, y);

            if (!tilePaths.ContainsKey(key))
            {
                tilePaths.Add(key, path);
            }
        }

        return tilePaths;
    }

    private static Sprite PrepareAndLoadSprite(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (texture == null)
        {
            AssetDatabase.ImportAsset(path);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        int pixelsPerUnit = texture != null ? Mathf.Max(1, texture.width) : 100;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            bool changed = false;
            changed |= SetImporterValue(importer.textureType, TextureImporterType.Sprite, value => importer.textureType = value);
            changed |= SetImporterValue(importer.spriteImportMode, SpriteImportMode.Single, value => importer.spriteImportMode = value);
            changed |= SetImporterValue(importer.spritePixelsPerUnit, (float)pixelsPerUnit, value => importer.spritePixelsPerUnit = value);
            changed |= SetImporterValue(importer.filterMode, FilterMode.Bilinear, value => importer.filterMode = value);
            changed |= SetImporterValue(importer.textureCompression, TextureImporterCompression.Uncompressed, value => importer.textureCompression = value);
            changed |= SetImporterValue(importer.alphaIsTransparency, true, value => importer.alphaIsTransparency = value);
            changed |= SetSpriteMeshType(importer, SpriteMeshType.FullRect);

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static bool SetSpriteMeshType(TextureImporter importer, SpriteMeshType targetMeshType)
    {
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        if (settings.spriteMeshType == targetMeshType)
        {
            return false;
        }

        settings.spriteMeshType = targetMeshType;
        importer.SetTextureSettings(settings);
        return true;
    }

    private static bool SetImporterValue<T>(T currentValue, T targetValue, System.Action<T> apply)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, targetValue))
        {
            return false;
        }

        apply(targetValue);
        return true;
    }

    private static bool ApplySpriteToCell(GameObject cell, Sprite sprite)
    {
        Transform artTransform = cell.transform.Find("ArtVisual");
        GameObject artObject;
        bool created = false;

        if (artTransform == null)
        {
            artObject = new GameObject("ArtVisual");
            artObject.transform.SetParent(cell.transform, false);
            created = true;
        }
        else
        {
            artObject = artTransform.gameObject;
        }

        artObject.transform.localPosition = Vector3.zero;
        artObject.transform.localRotation = Quaternion.identity;
        artObject.transform.localScale = CalculateNormalizedSpriteScale(sprite);

        SpriteRenderer renderer = artObject.GetComponent<SpriteRenderer>();

        if (renderer == null)
        {
            renderer = artObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -10;

        DisableGreyboxVisual(cell.transform, "Visual");
        DisableGreyboxVisual(cell.transform, "GreyboxVisual");
        return created;
    }

    private static Vector3 CalculateNormalizedSpriteScale(Sprite sprite)
    {
        if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
        {
            return Vector3.one;
        }

        return new Vector3(
            TargetCellVisualSize / sprite.bounds.size.x,
            TargetCellVisualSize / sprite.bounds.size.y,
            1f);
    }

    private static bool FitCameraToTileGrid(
        Scene scene,
        Dictionary<Vector2Int, GameObject> cells,
        out string report)
    {
        Camera camera = FindSceneCamera(scene);

        if (camera == null)
        {
            report = "Tile Grid Art Applier: no Camera found to frame the tile grid.";
            return false;
        }

        if (!TryGetGridBounds(cells, out Vector2 center, out Vector2 size))
        {
            report = "Tile Grid Art Applier: could not resolve MapCell bounds.";
            return false;
        }

        float aspect = camera.aspect > 0f ? camera.aspect : TargetAspect;
        float sizeByHeight = size.y * 0.5f;
        float sizeByWidth = size.x / (2f * aspect);

        camera.orthographic = true;
        camera.transform.position = new Vector3(center.x, center.y, -10f);
        camera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) + TileGridCameraMargin;

        report =
            "Tile Grid Art Applier camera fit\n"
            + $"Scene: {scene.path}\n"
            + $"Map size: {size.x:0.###} x {size.y:0.###}\n"
            + $"Camera position: {camera.transform.position}\n"
            + $"Orthographic size: {camera.orthographicSize:0.###}";
        return true;
    }

    private static Camera FindSceneCamera(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Camera[] cameras = roots[i].GetComponentsInChildren<Camera>(true);

            for (int j = 0; j < cameras.Length; j++)
            {
                if (cameras[j].name == "Main Camera")
                {
                    return cameras[j];
                }
            }
        }

        for (int i = 0; i < roots.Length; i++)
        {
            Camera camera = roots[i].GetComponentInChildren<Camera>(true);

            if (camera != null)
            {
                return camera;
            }
        }

        return null;
    }

    private static bool TryGetGridBounds(
        Dictionary<Vector2Int, GameObject> cells,
        out Vector2 center,
        out Vector2 size)
    {
        bool hasAny = false;
        float minX = 0f;
        float maxX = 0f;
        float minY = 0f;
        float maxY = 0f;

        for (int y = 0; y < TileGridHeight; y++)
        {
            for (int x = 0; x < TileGridWidth; x++)
            {
                if (!cells.TryGetValue(new Vector2Int(x, y), out GameObject cell) || cell == null)
                {
                    continue;
                }

                Vector3 position = cell.transform.position;

                if (!hasAny)
                {
                    minX = position.x - 0.5f;
                    maxX = position.x + 0.5f;
                    minY = position.y - 0.5f;
                    maxY = position.y + 0.5f;
                    hasAny = true;
                    continue;
                }

                minX = Mathf.Min(minX, position.x - 0.5f);
                maxX = Mathf.Max(maxX, position.x + 0.5f);
                minY = Mathf.Min(minY, position.y - 0.5f);
                maxY = Mathf.Max(maxY, position.y + 0.5f);
            }
        }

        if (!hasAny)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            return false;
        }

        center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        size = new Vector2(maxX - minX, maxY - minY);
        return true;
    }

    private static void DisableGreyboxVisual(Transform cellTransform, string childName)
    {
        Transform child = cellTransform.Find(childName);

        if (child != null && child.name != "ArtVisual")
        {
            child.gameObject.SetActive(false);
        }
    }

    private static string BuildReport(
        Scene scene,
        string tileFolder,
        int mapCellCount,
        int tileFileCount,
        int createdArtVisualCount,
        int assignedCount,
        List<string> missingCells,
        List<string> missingTiles,
        List<string> unmatchedTiles)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("Tile Grid Art Applier report");
        report.AppendLine($"Scene: {scene.path}");
        report.AppendLine($"Tile folder: {tileFolder}");
        report.AppendLine($"MapCells found: {mapCellCount}");
        report.AppendLine($"MapCells expected: {ExpectedTileCount}");
        report.AppendLine($"Tiles found: {tileFileCount}");
        report.AppendLine($"Tiles expected: {ExpectedTileCount}");
        report.AppendLine($"ArtVisual created: {createdArtVisualCount}");
        report.AppendLine($"Sprites assigned: {assignedCount}");
        AppendList(report, "Missing cells", missingCells);
        AppendList(report, "Missing tiles", missingTiles);
        AppendList(report, "Tile files without matching MapCell", unmatchedTiles);
        return report.ToString();
    }

    private static void WarnIfMapCellCountDoesNotMatchTiles(int mapCellCount)
    {
        if (mapCellCount == ExpectedTileCount)
        {
            return;
        }

        Debug.LogWarning(
            $"Tile map cell count does not match tile grid. Found {mapCellCount} MapCells, expected {ExpectedTileCount}. "
            + "Rebuild the level before applying 12x9 tile art.");
    }

    private static void AppendList(StringBuilder report, string title, List<string> values)
    {
        if (values == null || values.Count == 0)
        {
            report.AppendLine($"{title}: none");
            return;
        }

        report.AppendLine($"{title}: {values.Count}");

        for (int i = 0; i < values.Count; i++)
        {
            report.AppendLine($"- {values[i]}");
        }
    }
}
