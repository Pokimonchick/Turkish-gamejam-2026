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

        RebuildOrigamiObjects();
        EditorSceneManager.SaveScene(workbenchScene);
        AssetDatabase.Refresh();

        Debug.Log($"Rebuilt origami fold workbench: {WorkbenchScenePath}");
    }

    private static void RebuildOrigamiObjects()
    {
        DeleteIfExists("ORIGAMI_FOLD_SYSTEM");
        DeleteIfExists("ORIGAMI_FOLD_POINTS");
        DeleteIfExists("ORIGAMI_FOLD_LINKS");
        DeleteIfExists("ORIGAMI_DEBUG");

        GameObject systemRoot = new GameObject("ORIGAMI_FOLD_SYSTEM");
        GameObject pointsRoot = new GameObject("ORIGAMI_FOLD_POINTS");
        GameObject linksRoot = new GameObject("ORIGAMI_FOLD_LINKS");
        GameObject debugRoot = new GameObject("ORIGAMI_DEBUG");

        Camera camera = FindMainCameraOrCreate();
        GameObject executeIndicator = CreateExecuteIndicator(debugRoot.transform);
        CreateInstructionText(debugRoot.transform);

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

        OrigamiFoldLink top = CreateFoldLink(
            "OrigamiLink_Top",
            topLeft,
            topRight,
            executeIndicator,
            linksRoot.transform);

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
        controller.links = new[] { top, bottom, left, right };
    }

    private static void DeleteIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static Camera FindMainCameraOrCreate()
    {
        Camera camera = Camera.main;

        if (camera != null)
        {
            return camera;
        }

        GameObject existing = GameObject.Find("Main Camera");

        if (existing != null)
        {
            camera = existing.GetComponent<Camera>();

            if (camera != null)
            {
                return camera;
            }
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();

        return camera;
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
        indicator.transform.position = new Vector3(0f, 0f, 0f);
        indicator.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

        Collider collider = indicator.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = indicator.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateMaterial(Color.green);
        indicator.SetActive(false);

        return indicator;
    }

    private static void CreateInstructionText(Transform parent)
    {
        GameObject textObject = new GameObject("InstructionText");
        textObject.transform.SetParent(parent);
        textObject.transform.position = new Vector3(-3.6f, 2.7f, 0f);

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = "Drag one origami point to a neighboring point. Diagonals should not work.";
        text.characterSize = 0.18f;
        text.fontSize = 32;
        text.anchor = TextAnchor.UpperLeft;
        text.color = Color.white;
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
