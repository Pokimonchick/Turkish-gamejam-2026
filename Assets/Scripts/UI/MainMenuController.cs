using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public sealed class MainMenuSceneReference
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif
    [HideInInspector]
    [SerializeField] private string scenePath;
    [HideInInspector]
    [SerializeField] private string sceneName;

    public string RuntimeSceneKey
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                return sceneName;
            }

            return scenePath;
        }
    }

#if UNITY_EDITOR
    public void SyncFromSceneAsset()
    {
        if (sceneAsset == null)
        {
            return;
        }

        scenePath = AssetDatabase.GetAssetPath(sceneAsset);
        sceneName = sceneAsset.name;
    }
#endif
}

[Serializable]
public sealed class MainMenuSceneButton
{
    [SerializeField] private Button button;
    [SerializeField] private MainMenuSceneReference targetScene = new MainMenuSceneReference();

    public void Register(Action<string> loadScene)
    {
        if (button == null || loadScene == null)
        {
            return;
        }

        button.onClick.AddListener(() => loadScene(targetScene.RuntimeSceneKey));
    }

#if UNITY_EDITOR
    public void SyncSceneAsset()
    {
        targetScene?.SyncFromSceneAsset();
    }
#endif
}

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private MainMenuSceneButton[] sceneButtons = Array.Empty<MainMenuSceneButton>();
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeSettingsButton;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValueText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValueText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeValueText;

    private Text masterVolumeLegacyValueText;
    private Text musicVolumeLegacyValueText;
    private Text sfxVolumeLegacyValueText;

    private void Awake()
    {
        EnsureEventSystemInputModule();
        ResolveVolumeLabels();
        RegisterButtons();
        LoadAudioSettings();
        ShowSettings(false);
    }

    private void RegisterButtons()
    {
        foreach (var sceneButton in sceneButtons)
        {
            sceneButton?.Register(LoadScene);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => ShowSettings(true));
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(QuitGame);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(() => ShowSettings(false));
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }
    }

    private void LoadAudioSettings()
    {
        var audioManager = GameAudioManager.Instance;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(audioManager.MasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(audioManager.MusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(audioManager.SfxVolume);
        }

        UpdateVolumeLabel(masterVolumeValueText, masterVolumeLegacyValueText, audioManager.MasterVolume);
        UpdateVolumeLabel(musicVolumeValueText, musicVolumeLegacyValueText, audioManager.MusicVolume);
        UpdateVolumeLabel(sfxVolumeValueText, sfxVolumeLegacyValueText, audioManager.SfxVolume);
    }

    private void LoadScene(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            Debug.LogWarning("Main menu scene button has no target scene assigned.", this);
            return;
        }

        SceneTransition.Load(sceneKey);
    }

    private void ShowSettings(bool isOpen)
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(!isOpen);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(isOpen);
        }
    }

    private void SetMasterVolume(float volume)
    {
        GameAudioManager.Instance.SetMasterVolume(volume);
        UpdateVolumeLabel(masterVolumeValueText, masterVolumeLegacyValueText, GameAudioManager.Instance.MasterVolume);
    }

    private void SetMusicVolume(float volume)
    {
        GameAudioManager.Instance.SetMusicVolume(volume);
        UpdateVolumeLabel(musicVolumeValueText, musicVolumeLegacyValueText, GameAudioManager.Instance.MusicVolume);
    }

    private void SetSfxVolume(float volume)
    {
        GameAudioManager.Instance.SetSfxVolume(volume);
        UpdateVolumeLabel(sfxVolumeValueText, sfxVolumeLegacyValueText, GameAudioManager.Instance.SfxVolume);
    }

    private void ResolveVolumeLabels()
    {
        masterVolumeValueText = ResolveTmpLabel(masterVolumeValueText, "Master Volume Value");
        musicVolumeValueText = ResolveTmpLabel(musicVolumeValueText, "Music Volume Value");
        sfxVolumeValueText = ResolveTmpLabel(sfxVolumeValueText, "SFX Volume Value");

        masterVolumeLegacyValueText = ResolveLegacyLabel(masterVolumeLegacyValueText, "Master Volume Value");
        musicVolumeLegacyValueText = ResolveLegacyLabel(musicVolumeLegacyValueText, "Music Volume Value");
        sfxVolumeLegacyValueText = ResolveLegacyLabel(sfxVolumeLegacyValueText, "SFX Volume Value");
    }

    private TMP_Text ResolveTmpLabel(TMP_Text current, string objectName)
    {
        if (current != null)
        {
            return current;
        }

        var target = FindMenuObject(objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Text ResolveLegacyLabel(Text current, string objectName)
    {
        if (current != null)
        {
            return current;
        }

        var target = FindMenuObject(objectName);
        return target != null ? target.GetComponent<Text>() : null;
    }

    private GameObject FindMenuObject(string objectName)
    {
        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var item in transforms)
        {
            if (item.gameObject.name == objectName)
            {
                return item.gameObject;
            }
        }

        return null;
    }

    private void UpdateVolumeLabel(TMP_Text tmpLabel, Text legacyLabel, float volume)
    {
        var text = $"{Mathf.RoundToInt(volume * 100f)}%";

        if (tmpLabel != null)
        {
            tmpLabel.text = text;
        }

        if (legacyLabel != null)
        {
            legacyLabel.text = text;
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureEventSystemInputModule()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var sceneButton in sceneButtons)
        {
            sceneButton?.SyncSceneAsset();
        }

        ConfigureVolumeSlider(masterVolumeSlider);
        ConfigureVolumeSlider(musicVolumeSlider);
        ConfigureVolumeSlider(sfxVolumeSlider);
    }

    private static void ConfigureVolumeSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
    }
#endif
}
