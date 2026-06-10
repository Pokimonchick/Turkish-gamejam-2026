using UnityEditor;
using UnityEngine;

public static class OrigamiFoldLevelAudioBuilder
{
    public const string GameplayMusicPath =
        "Assets/Audio/sonic289-game-music_-soundtrack_exploration-_-peaceful-area-372769.mp3";
    public const string ForestAmbiencePath = "Assets/Audio/les_den_243.mp3";
    public const string WindAmbiencePath = "Assets/Audio/soundreality-wind-blowing-457954.mp3";
    public const string PageFoldSoundPath = "Assets/Audio/short-sharp-page-flipping-sound.mp3";

    public static void CreateGameplayAudio(
        Transform parent,
        string ambienceClipPath,
        float ambienceVolume = 1f)
    {
        GameObject audioRoot = CreateChild("LEVEL_AUDIO", parent);

        AudioClip musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GameplayMusicPath);
        if (musicClip != null)
        {
            CreatePersistentMusicStarter(audioRoot.transform, musicClip);
        }
        else
        {
            Debug.LogWarning($"Gameplay music was not found: {GameplayMusicPath}");
        }

        if (!string.IsNullOrEmpty(ambienceClipPath))
        {
            AudioClip ambienceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ambienceClipPath);
            if (ambienceClip != null)
            {
                CreateLoopingMusicSource(
                    "Level Ambience",
                    audioRoot.transform,
                    ambienceClip,
                    ambienceVolume);
            }
            else
            {
                Debug.LogWarning($"Level ambience was not found: {ambienceClipPath}");
            }
        }
    }

    public static void ConfigurePageFoldSound(
        OrigamiFoldStripSqueezeAction action,
        float volume = 1f)
    {
        if (action == null)
        {
            return;
        }

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PageFoldSoundPath);
        if (clip == null)
        {
            Debug.LogWarning($"Page fold sound was not found: {PageFoldSoundPath}");
            return;
        }

        action.foldSound = clip;
        action.foldSoundVolume = Mathf.Clamp01(volume);
    }

    private static void CreatePersistentMusicStarter(Transform parent, AudioClip clip)
    {
        GameObject musicObject = CreateChild("Gameplay Music", parent);
        AudioSource source = musicObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = true;

        SceneMusicPlayer player = musicObject.AddComponent<SceneMusicPlayer>();
        player.Configure(source, clip, true, true, true);
    }

    private static void CreateLoopingMusicSource(
        string name,
        Transform parent,
        AudioClip clip,
        float volume)
    {
        GameObject audioObject = CreateChild(name, parent);
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = true;
        source.loop = true;

        CategorizedAudioSource categorized = audioObject.AddComponent<CategorizedAudioSource>();
        categorized.Configure(AudioCategory.Music, volume);
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }
}
