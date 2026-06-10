using UnityEngine;

public enum AudioCategory
{
    Music,
    SFX
}

[DisallowMultipleComponent]
[AddComponentMenu("Game Audio/Game Audio Manager")]
public sealed class GameAudioManager : MonoBehaviour
{
    public const string MasterVolumePrefsKey = "MainMenu.MasterVolume";
    public const string MusicVolumePrefsKey = "GameAudio.MusicVolume";
    public const string SfxVolumePrefsKey = "GameAudio.SfxVolume";

    private const float DefaultMasterVolume = 1f;
    private const float DefaultCategoryVolume = 1f;

    private static GameAudioManager instance;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = DefaultMasterVolume;
    [SerializeField, Range(0f, 1f)] private float musicVolume = DefaultCategoryVolume;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = DefaultCategoryVolume;
    [Tooltip("Keeps this audio manager alive when another scene is loaded.")]
    [SerializeField] private bool persistBetweenScenes = true;
    [SerializeField] private AudioSource sfxOneShotSource;
    [SerializeField] private AudioSource persistentMusicSource;

    private readonly System.Collections.Generic.HashSet<CategorizedAudioSource> sources =
        new System.Collections.Generic.HashSet<CategorizedAudioSource>();

    public static bool HasInstance => instance != null;

    public static GameAudioManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<GameAudioManager>();
            if (instance != null)
            {
                instance.Initialize();
                return instance;
            }

            var audioManagerObject = new GameObject("Game Audio Manager");
            instance = audioManagerObject.AddComponent<GameAudioManager>();
            instance.Initialize();
            return instance;
        }
    }

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();
    }

    private void Initialize()
    {
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePrefsKey, DefaultMasterVolume));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumePrefsKey, DefaultCategoryVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumePrefsKey, DefaultCategoryVolume));

        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureOneShotSource();
        ApplyVolumes();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureOneShotSource();
        sfxOneShotSource.PlayOneShot(clip);
    }

    public void PlaySfx(AudioClip clip, float volumeScale)
    {
        if (clip == null)
        {
            return;
        }

        EnsureOneShotSource();
        sfxOneShotSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayMusic(AudioClip clip, bool loop)
    {
        if (clip == null)
        {
            StopMusic();
            return;
        }

        EnsurePersistentMusicSource();

        if (persistentMusicSource.clip != clip)
        {
            persistentMusicSource.Stop();
            persistentMusicSource.clip = clip;
        }

        persistentMusicSource.loop = loop;

        if (persistentMusicSource.clip.loadState == AudioDataLoadState.Unloaded)
        {
            persistentMusicSource.clip.LoadAudioData();
        }

        if (!persistentMusicSource.isPlaying)
        {
            persistentMusicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (persistentMusicSource == null)
        {
            return;
        }

        persistentMusicSource.Stop();
        persistentMusicSource.clip = null;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumePrefsKey, masterVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        SetCategoryVolume(AudioCategory.Music, volume);
    }

    public void SetSfxVolume(float volume)
    {
        SetCategoryVolume(AudioCategory.SFX, volume);
    }

    public float GetCategoryVolume(AudioCategory category)
    {
        return category == AudioCategory.Music ? musicVolume : sfxVolume;
    }

    public void Register(CategorizedAudioSource source)
    {
        if (source == null)
        {
            return;
        }

        sources.Add(source);
        source.ApplyCategoryVolume(GetCategoryVolume(source.Category));
    }

    public void Unregister(CategorizedAudioSource source)
    {
        sources.Remove(source);
    }

    private void SetCategoryVolume(AudioCategory category, float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (category == AudioCategory.Music)
        {
            musicVolume = volume;
            PlayerPrefs.SetFloat(MusicVolumePrefsKey, musicVolume);
        }
        else
        {
            sfxVolume = volume;
            PlayerPrefs.SetFloat(SfxVolumePrefsKey, sfxVolume);
        }

        PlayerPrefs.Save();
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = masterVolume;

        foreach (var source in sources)
        {
            if (source != null)
            {
                source.ApplyCategoryVolume(GetCategoryVolume(source.Category));
            }
        }
    }

    private void EnsureOneShotSource()
    {
        if (sfxOneShotSource != null)
        {
            return;
        }

        var sfxObject = new GameObject("SFX One Shot Source");
        sfxObject.transform.SetParent(transform);

        sfxOneShotSource = sfxObject.AddComponent<AudioSource>();
        sfxOneShotSource.playOnAwake = false;
        sfxOneShotSource.loop = false;

        sfxObject.AddComponent<CategorizedAudioSource>();
    }

    private void EnsurePersistentMusicSource()
    {
        if (persistentMusicSource != null)
        {
            return;
        }

        var musicObject = new GameObject("Persistent Music Source");
        musicObject.transform.SetParent(transform);

        persistentMusicSource = musicObject.AddComponent<AudioSource>();
        persistentMusicSource.playOnAwake = false;
        persistentMusicSource.loop = true;

        CategorizedAudioSource categorized = musicObject.AddComponent<CategorizedAudioSource>();
        categorized.Configure(AudioCategory.Music, 1f);
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
    }
}
