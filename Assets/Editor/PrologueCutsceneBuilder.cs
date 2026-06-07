using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PrologueCutsceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PrologueCutscene.unity";
    private const string DialogueDataPath = "Assets/ScriptableObjects/Dialogues/Prologue_Fire_Remembers.asset";
    private const string DialogueSystemPrefabPath = "Assets/Prefabs/Dialog/DialogueSystem.prefab";
    private const string PrologueMusicPath = "Assets/Audio/ultra-sunn-young-foxes.mp3";
    private const string DefaultNextSceneName = "Village_Level_01_Greybox";

    [MenuItem("Tools/PANINI/Prologue/Rebuild Prologue Cutscene")]
    public static void RebuildPrologueCutscene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Cannot rebuild PrologueCutscene while Unity is in Play Mode.");
            return;
        }

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/Dialogues");

        DialogueData dialogueData = EnsurePrologueDialogue();
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "PrologueCutscene";
        SceneManager.SetActiveScene(scene);

        CreateMainCamera();
        CreateBackgroundCanvas();
        CreateEventSystem();
        CreateAspectRatioManager();
        CreateDialogueSystem();
        CreateController(dialogueData);
        CreatePrologueMusic();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);

        if (previousActiveScene.IsValid())
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }

        EnsureSceneInBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Prologue cutscene rebuilt: {ScenePath}");
    }

    private static DialogueData EnsurePrologueDialogue()
    {
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(DialogueDataPath);

        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, DialogueDataPath);
        }

        dialogue.dialogueId = "prologue_the_stolen_fire";
        dialogue.lines = new List<DialogueLine>
        {
            CreateLine("Title", "Prologue. The Stolen Fire"),
            CreateLine("Narrator", "Long ago, in the wide steppe where the grass moved like a green sea, there stood a nomadic settlement."),
            CreateLine("Narrator", "White yurts sheltered families from the wind, and at the heart of the settlement burned a sacred communal fire."),
            CreateLine("Narrator", "To the nomads, fire was not merely flame. It gave warmth, welcomed guests, guarded the night, and gathered people together."),
            CreateLine("Narrator", "Beside the fire, quarrels were settled. Before it, the names of ancestors were remembered. Around it, children heard the old stories."),
            CreateLine("Narrator", "The elders taught that sacred fire must never be disturbed by empty words, anger, or disrespect."),
            CreateLine("Narrator", "But years passed, and people grew used to its warmth. What had once been a heavenly gift became something ordinary."),
            CreateLine("Narrator", "They stopped thanking the fire. They stopped listening to the silence around it."),
            CreateLine("Narrator", "And little by little, the flame that had protected them began to witness cruelty."),
            CreateLine("Narrator", "Fire was no longer used only for home, bread, and warmth. It was used to frighten others."),
            CreateLine("Narrator", "It burned the dry steppe. It threatened neighboring tribes. It became a sign of anger instead of care."),
            CreateLine("Narrator", "Then, one night, a wind rose over the steppe unlike any wind the oldest nomads had ever heard."),
            CreateLine("Narrator", "The sacred flame leapt from the hearth toward the sky, bright and wild, like a red bird spreading its wings."),
            CreateLine("Narrator", "Above the yurts appeared Umay, the heavenly guardian of the sacred fire."),
            CreateLine("Narrator", "In the form of a radiant bird, she flew over the settlement and lifted every spark, every ember, every trace of warmth into the darkness."),
            CreateLine("Narrator", "When the wind finally fell silent, only cold ash remained in the hearth."),
            CreateLine("Narrator", "The steppe was swallowed by night."),
            CreateLine("Narrator", "Without the sacred fire, the people were left with fear, cold, and regret."),
            CreateLine("Narrator", "But one girl saw where Umay had flown."),
            CreateLine("Narrator", "Her name was Aisulu."),
            CreateLine("Narrator", "Beyond the distant trees, where the old paths disappeared into shadow, the last light of the sacred fire was still fading."),
            CreateLine("Narrator", "And so began the tale of the girl who would follow the flame and try to bring warmth back to her people.")
        };

        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static DialogueLine CreateLine(string speakerName, string text)
    {
        return new DialogueLine
        {
            speakerProfile = null,
            speakerName = speakerName,
            text = text,
            portrait = null
        };
    }

    private static void CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateBackgroundCanvas()
    {
        GameObject canvasObject = new GameObject("Prologue Canvas");
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -20;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject background = CreateUiObject("Black Background", canvasObject.transform);
        Stretch(background.GetComponent<RectTransform>());
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.raycastTarget = false;

        GameObject placeholder = CreateUiObject("IllustrationPlaceholder", canvasObject.transform);
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0.12f, 0.36f);
        placeholderRect.anchorMax = new Vector2(0.88f, 0.9f);
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        Image placeholderImage = placeholder.AddComponent<Image>();
        placeholderImage.color = new Color(1f, 1f, 1f, 0f);
        placeholderImage.raycastTarget = false;
        placeholder.SetActive(false);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void CreateAspectRatioManager()
    {
        GameObject aspectObject = new GameObject("AspectRatioManager");
        aspectObject.AddComponent<LetterboxManager>();
    }

    private static void CreateDialogueSystem()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueSystemPrefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"DialogueSystem prefab was not found: {DialogueSystemPrefabPath}");
            return;
        }

        GameObject dialogueSystem = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (dialogueSystem != null)
        {
            dialogueSystem.name = "DialogueSystem";
        }
    }

    private static void CreateController(DialogueData dialogueData)
    {
        GameObject controllerObject = new GameObject("PrologueCutsceneController");
        PrologueCutsceneController controller = controllerObject.AddComponent<PrologueCutsceneController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("dialogueData").objectReferenceValue = dialogueData;
        serializedController.FindProperty("nextSceneName").stringValue = DefaultNextSceneName;
        serializedController.FindProperty("startDelaySeconds").floatValue = 0.25f;
        serializedController.FindProperty("playOnStart").boolValue = true;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePrologueMusic()
    {
        GameObject musicObject = new GameObject("Prologue Music");
        AudioSource audioSource = musicObject.AddComponent<AudioSource>();
        audioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PrologueMusicPath);
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.volume = 1f;

        CategorizedAudioSource categorizedAudioSource = musicObject.AddComponent<CategorizedAudioSource>();
        SerializedObject serializedAudio = new SerializedObject(categorizedAudioSource);
        serializedAudio.FindProperty("audioSource").objectReferenceValue = audioSource;
        serializedAudio.FindProperty("category").enumValueIndex = (int)AudioCategory.Music;
        serializedAudio.FindProperty("sourceVolume").floatValue = 0.75f;
        serializedAudio.ApplyModifiedPropertiesWithoutUndo();

        if (audioSource.clip == null)
        {
            Debug.LogWarning($"Prologue music clip was not found: {PrologueMusicPath}", musicObject);
        }
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.Any(scene => scene.path == scenePath))
        {
            return;
        }

        int insertIndex = scenes.FindIndex(scene => scene.path == "Assets/Scenes/Loading.unity");
        EditorBuildSettingsScene prologueScene = new EditorBuildSettingsScene(scenePath, true);

        if (insertIndex >= 0)
        {
            scenes.Insert(insertIndex + 1, prologueScene);
        }
        else
        {
            scenes.Add(prologueScene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');

        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int separatorIndex = path.LastIndexOf('/');

        if (separatorIndex < 0)
        {
            return;
        }

        string parent = path.Substring(0, separatorIndex);
        string folderName = path.Substring(separatorIndex + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
