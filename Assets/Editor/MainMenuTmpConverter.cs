using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class MainMenuTmpConverter
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SourceFontPath = "Assets/artist_nouveau.ttf";
    private const string FontAssetPath = "Assets/Fonts/artist_nouveau SDF.asset";

    [MenuItem("Tools/Main Menu/Convert Text To TextMeshPro")]
    public static void ConvertMainMenuIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (sceneAsset == null)
        {
            return;
        }

        var scene = GetLoadedScene(ScenePath);
        var openedForConversion = !scene.IsValid();
        if (openedForConversion)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            if (!scene.IsValid())
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            var legacyTexts = roots
                .SelectMany(root => root.GetComponentsInChildren<Text>(true))
                .ToArray();

            if (legacyTexts.Length == 0)
            {
                return;
            }

            var fontAsset = GetOrCreateFontAsset();
            if (fontAsset == null)
            {
                Debug.LogWarning("Main menu TMP conversion skipped: artist_nouveau.ttf was not found.");
                return;
            }

            var buttons = roots
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .ToArray();

            foreach (var legacyText in legacyTexts)
            {
                ConvertText(legacyText, fontAsset, buttons);
            }

            RebindMainMenuController(roots);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (openedForConversion && scene.IsValid())
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static Scene GetLoadedScene(string scenePath)
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.path == scenePath)
            {
                return scene;
            }
        }

        return default;
    }

    private static TMP_FontAsset GetOrCreateFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null)
        {
            return existing;
        }

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            return null;
        }

        var directory = Path.GetDirectoryName(FontAssetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            128,
            12,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic);

        fontAsset.name = "artist_nouveau SDF";
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        return fontAsset;
    }

    private static void ConvertText(Text legacyText, TMP_FontAsset fontAsset, Button[] buttons)
    {
        var gameObject = legacyText.gameObject;
        var targetButtons = buttons
            .Where(button => button != null && button.targetGraphic == legacyText)
            .ToArray();

        var text = legacyText.text;
        var color = legacyText.color;
        var fontSize = legacyText.fontSize;
        var enableAutoSizing = legacyText.resizeTextForBestFit;
        var fontSizeMin = legacyText.resizeTextMinSize;
        var fontSizeMax = legacyText.resizeTextMaxSize;
        var alignment = ToTmpAlignment(legacyText.alignment);
        var fontStyle = ToTmpFontStyle(legacyText.fontStyle);
        var raycastTarget = legacyText.raycastTarget;
        var richText = legacyText.supportRichText;
        var wrappingMode = legacyText.horizontalOverflow == HorizontalWrapMode.Wrap
            ? TextWrappingModes.Normal
            : TextWrappingModes.NoWrap;
        var overflowMode = legacyText.verticalOverflow == VerticalWrapMode.Overflow
            ? TextOverflowModes.Overflow
            : TextOverflowModes.Truncate;

        var textMesh = gameObject.GetComponent<TextMeshProUGUI>();
        Object.DestroyImmediate(legacyText, true);

        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshProUGUI>();
        }

        textMesh.font = fontAsset;
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = fontSize;
        textMesh.enableAutoSizing = enableAutoSizing;
        textMesh.fontSizeMin = fontSizeMin;
        textMesh.fontSizeMax = fontSizeMax;
        textMesh.alignment = alignment;
        textMesh.fontStyle = fontStyle;
        textMesh.raycastTarget = raycastTarget;
        textMesh.richText = richText;
        textMesh.textWrappingMode = wrappingMode;
        textMesh.overflowMode = overflowMode;
        textMesh.margin = Vector4.zero;

        foreach (var button in targetButtons)
        {
            button.targetGraphic = textMesh;
            EditorUtility.SetDirty(button);
        }

        EditorUtility.SetDirty(textMesh);
    }

    private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
    {
        return alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center,
        };
    }

    private static FontStyles ToTmpFontStyle(FontStyle style)
    {
        return style switch
        {
            FontStyle.Bold => FontStyles.Bold,
            FontStyle.Italic => FontStyles.Italic,
            FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
            _ => FontStyles.Normal,
        };
    }

    private static void RebindMainMenuController(GameObject[] roots)
    {
        var controller = roots
            .SelectMany(root => root.GetComponentsInChildren<MainMenuController>(true))
            .FirstOrDefault();

        if (controller == null)
        {
            return;
        }

        var serializedController = new SerializedObject(controller);
        SetTextReference(serializedController, roots, "masterVolumeValueText", "Master Volume Value");
        SetTextReference(serializedController, roots, "musicVolumeValueText", "Music Volume Value");
        SetTextReference(serializedController, roots, "sfxVolumeValueText", "SFX Volume Value");
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void SetTextReference(
        SerializedObject serializedObject,
        GameObject[] roots,
        string propertyName,
        string objectName)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        var text = roots
            .SelectMany(root => root.GetComponentsInChildren<TextMeshProUGUI>(true))
            .FirstOrDefault(item => item.gameObject.name == objectName);

        property.objectReferenceValue = text;
    }
}
